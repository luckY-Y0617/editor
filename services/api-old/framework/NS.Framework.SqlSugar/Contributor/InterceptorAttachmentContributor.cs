using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NS.Framework.SqlSugar.Abstractions;
using NS.Framework.SqlSugar.Interceptors;
using SqlSugar;

namespace NS.Framework.SqlSugar.Contributor;

public sealed class InterceptorAttachmentContributor : ISqlSugarClientContributor
{
    public int ExecutionOrder => int.MinValue;

    // 防止同一个 SqlSugarClient 重复 Attach（AppendRuntime 可能多次触发）
    private static readonly ConcurrentDictionary<int, byte> Attached = new();

    public void Contribute(SqlSugarDbContextConfigurationContext context, SqlSugarClientContext options)
    {
        options.AddInterceptor<AdoLoggingInterceptor>();
        options.AddInterceptor<AuditInterceptor>();
        
        options.AppendRuntime(client =>
        {
            var sp = context.ServiceProvider;

            // 构建拦截器实例列表：按 options.InterceptorTypes 从容器取
            var instances = new List<ISqlSugarInterceptor>(options.InterceptorTypes.Count);

            foreach (var type in options.InterceptorTypes)
            {
                // 你这里的 type 应该都是实现了 ISqlSugarInterceptor 的具体类型
                if (sp.GetService(type) is ISqlSugarInterceptor interceptor)
                    instances.Add(interceptor);
            }

            if (instances.Count == 0) return;

            var sugarClient = (SqlSugarClient)client;

            // 基于引用的 identity hash，避免重复 attach
            var key = RuntimeHelpers.GetHashCode(sugarClient);
            if (!Attached.TryAdd(key, 0)) return;

            AttachInterceptors(sugarClient, instances);
        });
    }

    private static void AttachInterceptors(SqlSugarClient client, List<ISqlSugarInterceptor> interceptors)
    {
        var list = interceptors.OrderBy(x => x.ExecutionOrder).ToList();

        client.Aop.OnLogExecuting = (sql, pars) =>
        {
            foreach (var itc in list) itc.OnLogExecuting(sql, pars);
        };

        client.Aop.OnLogExecuted = (sql, pars) =>
        {
            foreach (var itc in list) itc.OnLogExecuted(sql, pars);
        };

        client.Aop.DataExecuting = (obj, model) =>
        {
            foreach (var itc in list) itc.DataExecuting(obj, model);
        };

        client.Aop.DataExecuted = (obj, model) =>
        {
            foreach (var itc in list) itc.DataExecuted(obj, model);
        };
    }
}

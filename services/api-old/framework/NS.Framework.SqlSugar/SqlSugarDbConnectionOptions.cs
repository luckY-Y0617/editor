using System;
using System.Collections.Generic;
using SqlSugar;

namespace NS.Framework.SqlSugar;

public class SqlSugarDbConnectionOptions
{
    public string DefaultConnectionName { get; set; } = "Default";

    public Dictionary<string, string> ConnectionStrings { get; set; } = new();

    public Dictionary<string, DbType> DbTypes { get; set; } = new();

    public Dictionary<string, List<SlaveConnectionConfig>> SlaveConnections { get; set; } = new();

    public (string ConnectionString, DbType DbType) Get(string? name = null)
    {
        name ??= DefaultConnectionName;

        if (!ConnectionStrings.TryGetValue(name, out var conn))
            throw new InvalidOperationException($"Host 配置中缺少连接字符串 '{name}'。");

        if (!DbTypes.TryGetValue(name, out var dbType))
            throw new InvalidOperationException($"Host 配置中缺少连接类型 (DbType) '{name}'。");

        return (conn, dbType);
    }

    public void Set(string name, string connectionString, DbType dbType)
    {
        ConnectionStrings[name] = connectionString;
        DbTypes[name] = dbType;
    }

    public DbType GetDbTypeOrThrow(string name)
    {
        if (!DbTypes.TryGetValue(name, out var dbType))
            throw new InvalidOperationException($"未为连接 '{name}' 配置 DbType。");
        return dbType;
    }

    /// <summary>
    /// 获取指定连接名的从库配置列表。如果未配置，返回空列表。
    /// </summary>
    public List<SlaveConnectionConfig> GetSlaveConnections(string connectionName)
    {
        if (SlaveConnections.TryGetValue(connectionName, out var slaves))
        {
            return slaves;
        }
        return new List<SlaveConnectionConfig>();
    }
}
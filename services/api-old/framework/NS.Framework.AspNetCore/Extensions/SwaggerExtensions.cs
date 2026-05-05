using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using NS.Framework.AspNetCore.Filters;
using NS.Framework.AspNetCore.MultiTenancy;
using Swashbuckle.AspNetCore.SwaggerGen;
using Volo.Abp.AspNetCore.Mvc;

namespace NS.Framework.AspNetCore.Extensions;

public static class SwaggerExtensions
{
    private const string JwtSchemeName = "JwtBearer";
    private const string DefaultSwaggerVersion = "v1";

    public static IServiceCollection AddCustomSwaggerGen<TProgram>(
        this IServiceCollection services,
        Action<SwaggerGenOptions>? configure = null)
    {
        var mvcOptions = services.GetPreConfigureActions<AbpAspNetCoreMvcOptions>().Configure();
        var conventionalSettings = mvcOptions.ConventionalControllers.ConventionalControllerSettings;

        // 分组文档：按 RemoteServiceName 去重 + 排序
        var controllerSettings = conventionalSettings
            .Where(x => !string.IsNullOrWhiteSpace(x.RemoteServiceName))
            .DistinctBy(x => x.RemoteServiceName)
            .OrderBy(x => x.RemoteServiceName)
            .ToList();

        // 过滤文档：预先构建 Assembly -> RemoteServiceName 映射，避免每次线性查找
        var assemblyToRemoteServiceName = new Dictionary<System.Reflection.Assembly, string>();
        foreach (var setting in conventionalSettings)
        {
            if (string.IsNullOrWhiteSpace(setting.RemoteServiceName)) continue;
            assemblyToRemoteServiceName.TryAdd(setting.Assembly, setting.RemoteServiceName);
        }

        services.AddAbpSwaggerGen(options =>
        {
            // 外部自定义配置
            configure?.Invoke(options);

            // 添加分组文档
            var swaggerDocs = options.SwaggerGeneratorOptions.SwaggerDocs;
            foreach (var setting in controllerSettings)
            {
                if (!swaggerDocs.ContainsKey(setting.RemoteServiceName))
                {
                    options.SwaggerDoc(setting.RemoteServiceName, new OpenApiInfo
                    {
                        Title = setting.RemoteServiceName,
                        Version = DefaultSwaggerVersion
                    });
                }
            }

            // 根据控制器所在程序集，按分组过滤文档
            options.DocInclusionPredicate((docName, apiDesc) =>
            {
                if (apiDesc.ActionDescriptor is not ControllerActionDescriptor descriptor) return false;
                var assembly = descriptor.ControllerTypeInfo.Assembly;
                return assemblyToRemoteServiceName.TryGetValue(assembly, out var remoteServiceName)
                       && remoteServiceName == docName;
            });

            // 使用完整类型名避免 Schema 冲突
            options.CustomSchemaIds(type => type.FullName ?? type.Name);

            // 加载 XML 注释
            AddXmlComments(typeof(TProgram).Assembly.Location, options);

            // JWT 鉴权支持
            AddJwtSecurityDefinition(options);

            // 枚举增强显示
            options.SchemaFilter<EnumSchemaFilter>();
            options.OperationFilter<XTenantIdHeaderOperationFilter>();
        });

        return services;
    }

    private static void AddXmlComments(string assemblyPath, SwaggerGenOptions options)
    {
        var basePath = Path.GetDirectoryName(assemblyPath);
        if (basePath == null) return;

        foreach (var file in Directory.GetFiles(basePath, "*.xml"))
        {
            options.IncludeXmlComments(file, includeControllerXmlComments: true);
        }
    }

    private static void AddJwtSecurityDefinition(SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(JwtSchemeName, new OpenApiSecurityScheme
        {
            Description = "输入 JWT（无需 Bearer 前缀）",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtSchemeName
                }
            }] = Array.Empty<string>()
        });
    }
}

public static class SwaggerBuilderExtensions
{
    /// <summary>
    /// 配置ClayMo Swagger UI
    /// </summary>
    public static IApplicationBuilder UseClayMoSwagger(this IApplicationBuilder app, params SwaggerModel[] swaggerModels)
    {
        var mvcOptions = app.ApplicationServices.GetRequiredService<IOptions<AbpAspNetCoreMvcOptions>>().Value;
        var remoteServiceNames = mvcOptions.ConventionalControllers.ConventionalControllerSettings
            .Where(x => !string.IsNullOrWhiteSpace(x.RemoteServiceName))
            .Select(x => x.RemoteServiceName)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            foreach (var remoteServiceName in remoteServiceNames)
            {
                c.SwaggerEndpoint($"/swagger/{remoteServiceName}/swagger.json", remoteServiceName);
            }

            if (remoteServiceNames.Count == 0 && swaggerModels.Length == 0)
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClayMo.Framework");
            }
            else
            {
                foreach (var model in swaggerModels)
                {
                    c.SwaggerEndpoint(model.Url, model.Name);
                }
            }
        });

        return app;
    }
}

/// <summary>
/// Swagger模型配置
/// </summary>
public class SwaggerModel
{
    /// <summary>
    /// 初始化Swagger模型（使用默认URL）
    /// </summary>
    /// <param name="name">模型名称</param>
    public SwaggerModel(string name)
    {
        Name = name;
        Url = "/swagger/v1/swagger.json";
    }

    /// <summary>
    /// 初始化Swagger模型（自定义URL）
    /// </summary>
    /// <param name="url">Swagger JSON URL</param>
    /// <param name="name">模型名称</param>
    public SwaggerModel(string url, string name)
    {
        Url = url;
        Name = name;
    }

    /// <summary>
    /// Swagger JSON URL
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Name { get; set; }
}

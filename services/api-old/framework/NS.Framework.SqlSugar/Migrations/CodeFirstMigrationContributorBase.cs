using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions.Migrations;
using SqlSugar;

namespace NS.Framework.SqlSugar.Migrations;

public abstract class CodeFirstMigrationContributorBase : IMigrationContributor
{
    public abstract string Id { get; }
    public abstract string Description { get; }

    public virtual bool IsRepeatable => true;
    public virtual string? Checksum => null;

    protected abstract IEnumerable<Type> GetEntityTypes();

    public Task ExecuteAsync(ISqlSugarClient db, CancellationToken cancellationToken = default)
    {
        var types = GetEntityTypes().ToArray();
        if (types.Length > 0)
            db.CodeFirst.InitTables(types);

        return Task.CompletedTask;
    }

    protected static string ComputeChecksum(IEnumerable<Type> types)
    {
        var sb = new StringBuilder();

        foreach (var t in types.OrderBy(x => x.FullName, StringComparer.Ordinal))
        {
            sb.AppendLine(t.FullName);

            var tableAttr = t.GetCustomAttribute<SugarTable>();
            sb.AppendLine($"[SugarTable:{tableAttr?.TableName}]");

            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(p => p.Name, StringComparer.Ordinal);

            foreach (var p in props)
            {
                var col = p.GetCustomAttribute<SugarColumn>();

                sb.Append("  ");
                sb.Append(p.Name);
                sb.Append(":");
                sb.Append(p.PropertyType.FullName);

                if (col != null)
                {
                    sb.Append($" [Len={col.Length}, Null={col.IsNullable}, PK={col.IsPrimaryKey}, Id={col.IsIdentity}, Ignore={col.IsIgnore}]");
                    if (!string.IsNullOrWhiteSpace(col.ColumnName))
                        sb.Append($" [ColName={col.ColumnName}]");
                }

                sb.AppendLine();
            }
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }
}
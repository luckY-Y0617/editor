namespace NS.Framework.SqlSugar.Abstractions.Migrations;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class MigrationAttribute : Attribute
{
    public string Scope { get; }
    public int Order { get; }

    public MigrationAttribute(string scope = MigrationScopes.Both, int order = 0)
    {
        Scope = scope;
        Order = order;
    }
}
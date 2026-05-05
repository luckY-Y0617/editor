using NS.Framework.SqlSugar.Abstractions.Migrations;
using NS.Framework.SqlSugar.Migrations;
using NS.Module.Knowledge.Domain.Comments;
using NS.Module.Knowledge.Domain.Documents;
using NS.Module.Knowledge.Domain.KnowledgeBases;
using NS.Module.Knowledge.Domain.Members;
using NS.Module.Knowledge.Domain.References;
using NS.Module.Knowledge.Domain.Tags;
using NS.Module.Knowledge.Domain.Versions;

namespace NS.Module.Knowledge.SqlSugar;

[Migration(MigrationScopes.Tenant, order: 200)]
public sealed class KnowledgeMigrationContributor : CodeFirstMigrationContributorBase
{
    public override string Id => "knowledge/tenant/codefirst";
    public override string Description => "Knowledge module tenant tables (CodeFirst InitTables)";

    protected override IEnumerable<Type> GetEntityTypes()
    {
        return new[]
        {
            // Comments
            typeof(Comment),

            // Documents
            typeof(Document),
            typeof(DocumentContent),

            // KnowledgeBases
            typeof(KnowledgeBase),

            // Members
            typeof(KnowledgeBaseMember),

            // References
            typeof(DocumentReference),

            // Tags
            typeof(Tag),
            typeof(DocumentTag),

            // Versions
            typeof(DocumentVersion)
        };
    }
}
using System;
using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Knowledge.Domain.Documents
{
    [SugarTable("kb_document_contents")]
    public class DocumentContent : Entity<Guid>, IMultiTenant, IHasConcurrencyStamp
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
        public override Guid Id { get; protected set; }

        [SugarColumn(IsNullable = true)]
        public Guid? TenantId { get; set; }

        [SugarColumn(IsNullable = false, ColumnDataType = "longtext")]
        public string ContentJson { get; protected set; } = default!;

        [SugarColumn(IsNullable = true, ColumnDataType = "longtext")]
        public string? ContentHtml { get; protected set; }

        [SugarColumn(IsNullable = true, ColumnDataType = "longtext")]
        public string? PlainText { get; protected set; }
        
        [SugarColumn(IsNullable = false)]
        public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

        public DocumentContent()
        {
        }

        public DocumentContent(Guid documentId, string contentJson,
            string? contentHtml, string? plainText): base(documentId)
        {
            ContentJson = contentJson;
            ContentHtml = contentHtml;
            PlainText = plainText;
        }

        public void Update(string contentJson, string? contentHtml, string? plainText)
        {
            ContentJson = contentJson;
            ContentHtml = contentHtml;
            PlainText = plainText;
        }

    }
}

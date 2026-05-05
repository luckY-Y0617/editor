using System;
using NS.Module.Knowledge.Domain.Documents;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using NS.Module.Knowledge.Domain.Shared.Enums;

namespace NS.Module.Knowledge.Domain.Versions
{
    [SugarTable("kb_document_versions")]
    public class DocumentVersion : CreationAuditedEntity<Guid>, IMultiTenant
    {
        [SugarColumn(IsNullable = true)]
        public Guid? TenantId { get; set; }

        [SugarColumn(IsNullable = false)]
        public Guid DocumentId { get; protected set; }
        
        [SugarColumn(IsNullable = false)]
        public int VersionNumber { get; protected set; }

        [SugarColumn(IsNullable = false, ColumnDataType = "longtext")]
        public string SnapshotJson { get; protected set; } = null!;

        [SugarColumn(IsNullable = true, ColumnDataType = "longtext")]
        public string? SnapshotHtml { get; protected set; }

        /// <summary>
        /// 纯文本快照（可选，用于搜索 / Diff / 摘要）。
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = "longtext")]
        public string? SnapshotPlainText { get; protected set; }


        [SugarColumn(Length = 256, IsNullable = true)]
        public string? ChangeSummary { get; protected set; }

        [SugarColumn(IsNullable = false)]
        public DocumentVersionSource Source { get; protected set; }

        [Navigate(NavigateType.OneToOne, nameof(DocumentId))]
        public Document? Document { get; set; }

        public DocumentVersion()
        {
        }

        public DocumentVersion(
            Guid documentId,
            int versionNumber,
            string snapshotJson,
            string? snapshotHtml,
            string? snapshotPlainText,
            DocumentVersionSource source,
            string? changeSummary)
        {
            DocumentId = documentId;
            VersionNumber = versionNumber;
            SnapshotJson = snapshotJson;
            SnapshotHtml = snapshotHtml;
            SnapshotPlainText = snapshotPlainText;
            Source = source;
            ChangeSummary = changeSummary;
        }
    }
}

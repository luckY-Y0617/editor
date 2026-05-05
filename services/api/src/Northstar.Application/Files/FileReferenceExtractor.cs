using System.Text.Json;
using System.Text.RegularExpressions;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public sealed partial class FileReferenceExtractor : IFileReferenceExtractor
{
    public IReadOnlyList<DocumentFileReference> Extract(JsonElement content)
    {
        var references = new HashSet<Guid>();
        Visit(content, references);

        return references
            .Select(fileId => new DocumentFileReference(fileId, DocumentAttachmentRelationType.InlineImage))
            .ToArray();
    }

    private static void Visit(JsonElement element, ISet<Guid> references)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                VisitObject(element, references);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Visit(item, references);
                }

                break;
        }
    }

    private static void VisitObject(JsonElement element, ISet<Guid> references)
    {
        if (element.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object)
        {
            ExtractFromAttrs(attrs, references);
        }

        foreach (var property in element.EnumerateObject())
        {
            Visit(property.Value, references);
        }
    }

    private static void ExtractFromAttrs(JsonElement attrs, ISet<Guid> references)
    {
        if (attrs.TryGetProperty("fileId", out var fileIdElement) &&
            fileIdElement.ValueKind == JsonValueKind.String &&
            Guid.TryParse(fileIdElement.GetString(), out var fileId))
        {
            references.Add(fileId);
        }

        ExtractFromUriAttribute(attrs, "src", references);
        ExtractFromUriAttribute(attrs, "href", references);
    }

    private static void ExtractFromUriAttribute(JsonElement attrs, string attributeName, ISet<Guid> references)
    {
        if (!attrs.TryGetProperty(attributeName, out var attribute) || attribute.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var value = attribute.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (Match match in FileContentUrlRegex().Matches(value))
        {
            if (Guid.TryParse(match.Groups["fileId"].Value, out var fileId))
            {
                references.Add(fileId);
            }
        }
    }

    [GeneratedRegex(@"(?:^|/)api/v1/files/(?<fileId>[0-9a-fA-F-]{36})/content(?:\?|$|#)|(?:^|/)files/(?<fileId>[0-9a-fA-F-]{36})/content(?:\?|$|#)", RegexOptions.Compiled)]
    private static partial Regex FileContentUrlRegex();
}

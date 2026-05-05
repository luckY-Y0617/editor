using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Northstar.Application.Knowledge;

public sealed class TiptapDocumentLinkExtractor : IDocumentLinkExtractor
{
    private const int AnchorTextLimit = 180;
    private static readonly Regex DocumentPathRegex = new(
        @"(?:^|/)documents/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:$|[/?#])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<DocumentLinkCandidate> Extract(JsonElement content)
    {
        var links = new List<DocumentLinkCandidate>();
        Visit(content, links);

        return links
            .GroupBy(link => link.TargetDocumentId)
            .Select(group => group.First())
            .ToArray();
    }

    private static void Visit(JsonElement element, List<DocumentLinkCandidate> links)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                VisitObject(element, links);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Visit(item, links);
                }

                break;
        }
    }

    private static void VisitObject(JsonElement element, List<DocumentLinkCandidate> links)
    {
        var anchorText = ExtractText(element);

        if (element.TryGetProperty("attrs", out var attrs))
        {
            AddCandidateFromAttrs(attrs, anchorText, links);
        }

        if (element.TryGetProperty("marks", out var marks) && marks.ValueKind == JsonValueKind.Array)
        {
            foreach (var mark in marks.EnumerateArray())
            {
                if (mark.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (mark.TryGetProperty("attrs", out var markAttrs))
                {
                    AddCandidateFromAttrs(markAttrs, anchorText, links);
                }
            }
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.NameEquals("attrs") || property.NameEquals("marks"))
            {
                continue;
            }

            Visit(property.Value, links);
        }
    }

    private static void AddCandidateFromAttrs(JsonElement attrs, string? anchorText, List<DocumentLinkCandidate> links)
    {
        if (attrs.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var propertyName in new[] { "documentId", "targetDocumentId", "href" })
        {
            if (!attrs.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var rawValue = value.GetString();
            if (TryParseDocumentId(rawValue, out var documentId))
            {
                links.Add(new DocumentLinkCandidate(documentId, anchorText));
            }
        }
    }

    private static bool TryParseDocumentId(string? value, out Guid documentId)
    {
        documentId = Guid.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (Guid.TryParse(trimmed, out documentId))
        {
            return true;
        }

        var match = DocumentPathRegex.Match(trimmed);
        return match.Success && Guid.TryParse(match.Groups["id"].Value, out documentId);
    }

    private static string? ExtractText(JsonElement element)
    {
        var builder = new StringBuilder();
        AppendText(element, builder);

        var text = builder.ToString().Trim();
        if (text.Length == 0)
        {
            return null;
        }

        return text.Length <= AnchorTextLimit ? text : text[..AnchorTextLimit];
    }

    private static void AppendText(JsonElement element, StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("text") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        builder.Append(property.Value.GetString());
                        builder.Append(' ');
                    }
                    else
                    {
                        AppendText(property.Value, builder);
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    AppendText(item, builder);
                }

                break;
        }
    }
}

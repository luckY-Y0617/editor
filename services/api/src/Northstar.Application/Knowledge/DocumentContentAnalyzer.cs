using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Northstar.Application.Knowledge;

public static class DocumentContentAnalyzer
{
    public static DocumentContentMetadata Analyze(JsonElement content)
    {
        var textBuilder = new StringBuilder();
        AppendText(content, textBuilder);

        var textContent = textBuilder.ToString().Trim();
        var wordCount = CountWords(textContent);
        var canonicalJson = JsonSerializer.Serialize(content);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson))).ToLowerInvariant();

        return new DocumentContentMetadata(canonicalJson, textContent, "[]", wordCount, hash);
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

    private static int CountWords(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

public sealed record DocumentContentMetadata(
    string ContentJson,
    string TextContent,
    string OutlineJson,
    int WordCount,
    string ContentHash);

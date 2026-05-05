using System.Text.Json;

namespace Northstar.Application.Files;

public interface IFileReferenceExtractor
{
    IReadOnlyList<DocumentFileReference> Extract(JsonElement content);
}

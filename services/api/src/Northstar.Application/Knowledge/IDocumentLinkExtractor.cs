using System.Text.Json;

namespace Northstar.Application.Knowledge;

public interface IDocumentLinkExtractor
{
    IReadOnlyList<DocumentLinkCandidate> Extract(JsonElement content);
}

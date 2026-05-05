using System.Text.Json;
using Northstar.Application.Knowledge;

namespace Northstar.Application.Tests;

public sealed class TiptapDocumentLinkExtractorTests
{
    [Fact]
    public void Extract_ReadsDocumentIdFromLinkMarkHref()
    {
        var documentId = Guid.NewGuid();
        using var json = JsonDocument.Parse($$"""
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                {
                  "type": "text",
                  "text": "linked doc",
                  "marks": [
                    {
                      "type": "link",
                      "attrs": {
                        "href": "/documents/{{documentId}}"
                      }
                    }
                  ]
                }
              ]
            }
          ]
        }
        """);

        var links = new TiptapDocumentLinkExtractor().Extract(json.RootElement);

        var link = Assert.Single(links);
        Assert.Equal(documentId, link.TargetDocumentId);
        Assert.Equal("linked doc", link.AnchorText);
    }

    [Fact]
    public void Extract_IgnoresInvalidUuid()
    {
        using var json = JsonDocument.Parse("""
        {
          "type": "doc",
          "content": [
            {
              "type": "paragraph",
              "content": [
                {
                  "type": "text",
                  "text": "broken link",
                  "marks": [
                    {
                      "type": "link",
                      "attrs": {
                        "href": "/documents/not-a-uuid"
                      }
                    }
                  ]
                }
              ]
            }
          ]
        }
        """);

        var links = new TiptapDocumentLinkExtractor().Extract(json.RootElement);

        Assert.Empty(links);
    }
}

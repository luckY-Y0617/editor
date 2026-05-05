using Northstar.Domain.Knowledge.Documents;

namespace Northstar.Application.Knowledge;

public sealed record DocumentEditState(Document Document, DocumentDraft Draft);


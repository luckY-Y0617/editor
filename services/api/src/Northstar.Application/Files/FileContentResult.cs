using Northstar.Contracts.Files;

namespace Northstar.Application.Files;

public sealed record FileContentResult(
    FileDto File,
    Stream Content);

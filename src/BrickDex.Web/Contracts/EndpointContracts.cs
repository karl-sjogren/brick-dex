using BrickDex.Core.Models;

namespace BrickDex.Web.Contracts;

public record UpdateStatusRequest(SetStatus Status);

public record AddSetRequest(string SetNumber);

public record AddSetResponse(Guid Id, string SetNumber, string Name);

public record ReindexResponse(bool Success, string Message, long DurationMs);

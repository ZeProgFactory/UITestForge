using System.Text.Json.Serialization;

namespace UITestForge;

/// <summary>Minimal element info returned by GET /api/v1/ui/elements.</summary>
public record ElementInfo(
   [property: JsonPropertyName("id")] string Id);

/// <summary>Request body for POST /api/v1/ui/actions/tap.</summary>
public record TapRequest(
   [property: JsonPropertyName("elementId")] string ElementId);

/// <summary>Response returned by POST /api/v1/ui/actions/tap (and other action endpoints).</summary>
public record ActionResponse(
   [property: JsonPropertyName("success")] bool Success,
   [property: JsonPropertyName("message")] string? Message = null,
   [property: JsonPropertyName("error")]   ProblemDetails? Error = null);

/// <summary>RFC 7807 problem details used in error responses.</summary>
public record ProblemDetails(
   [property: JsonPropertyName("title")]     string? Title  = null,
   [property: JsonPropertyName("status")]    int?    Status = null,
   [property: JsonPropertyName("errorCode")] string? ErrorCode = null);

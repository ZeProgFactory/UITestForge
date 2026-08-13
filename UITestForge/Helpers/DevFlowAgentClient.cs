using System.Diagnostics;
using System.Net.Http.Json;

namespace UITestForge.Helpers;

/// <summary>
/// Communicates directly with a DevFlow agent's REST API
/// (<c>http://{host}:{agent.Port}/api/v1/…</c>).
/// </summary>
internal static class DevFlowAgentClient
{
   private static readonly HttpClient _http = new(
      new SocketsHttpHandler { PooledConnectionLifetime = TimeSpan.FromMinutes(2) })
   { Timeout = TimeSpan.FromSeconds(20) };

   private static string BaseUrl(DevFlowAgent agent)
      => $"http://{DevFlowBrokerClient.BrokerHost}:{agent.Port}";

   /// <summary>
   /// Queries the agent for elements matching the given <paramref name="automationId"/>
   /// using the <c>accessibility-id</c> locator strategy
   /// (<c>GET /api/v1/ui/elements?strategy=accessibility-id&amp;value=…</c>).
   /// Returns the first matching element id, or <see langword="null"/> if not found.
   /// </summary>
   public static async Task<string?> FindElementByAutomationIdAsync(
      DevFlowAgent agent, string automationId, CancellationToken ct = default)
   {
      var url = $"{BaseUrl(agent)}/api/v1/ui/elements"
              + $"?automationId={Uri.EscapeDataString(automationId)}&limit=1";

      var elements = await _http.GetFromJsonAsync(
         url,
         DevFlowJsonContext.Default.ListElementInfo,
         ct);

      return elements?.Count > 0 ? elements[0].Id : null;
   }

   /// <summary>
   /// Taps the element with the given <paramref name="elementId"/>
   /// (<c>POST /api/v1/ui/actions/tap</c>).
   /// </summary>
   public static async Task<ActionResponse> TapElementAsync(
      DevFlowAgent agent, string elementId, CancellationToken ct = default)
   {
      var url = $"{BaseUrl(agent)}/api/v1/ui/actions/tap";

      try
      {
         var response = await _http.PostAsJsonAsync(
            url,
            new TapRequest(elementId),
            DevFlowJsonContext.Default.TapRequest,
            ct);

         response.EnsureSuccessStatusCode();

         return await response.Content.ReadFromJsonAsync(
            DevFlowJsonContext.Default.ActionResponse,
            ct) ?? new ActionResponse(false);
      }
      catch (Exception ex)
      {
         Debug.WriteLine($"Error tapping element {elementId} on agent {agent.Id}: {ex}"); 
         Debug.WriteLine(ex);
         return new ActionResponse(false);
      }
   }
}

using System.Net.Http.Json;

namespace UITestForge.Helpers;

internal static class DevFlowBrokerClient
{
   public const int BrokerPort = 19223;

   public static string BrokerHost =>
#if ANDROID
      "10.0.2.2";
#else
      "localhost";
#endif

   public static string BrokerAgentsUrl => $"http://{BrokerHost}:{BrokerPort}/api/agents";

   private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(4) };

   /// <summary>
   /// Fetches the current list of connected DevFlow agents from the broker.
   /// </summary>
   public static Task<List<DevFlowAgent>?> FetchAgentsAsync(CancellationToken ct)
      => _http.GetFromJsonAsync<List<DevFlowAgent>>(
            BrokerAgentsUrl,
            DevFlowJsonContext.Default.ListDevFlowAgent,
            ct);
}

using System.Diagnostics;

namespace UITestForge.Helpers;

internal static class DevFlowCliHelper
{
#if !ANDROID
   /// <summary>
   /// Ensures the DevFlow broker is running. If it is not reachable, launches
   /// <c>maui devflow broker start</c> and waits up to <paramref name="startupTimeoutMs"/> ms
   /// for it to become available.
   /// </summary>
   public static async Task EnsureBrokerStartedAsync(int startupTimeoutMs = 5_000)
   {
      using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
      try
      {
         var resp = await http.GetAsync(
            $"http://localhost:{DevFlowBrokerClient.BrokerPort}/api/agents");
         if (resp.IsSuccessStatusCode)
            return; // broker already running
      }
      catch { }

      // Broker not reachable – start it.
      var psi = new ProcessStartInfo("maui", "devflow broker start")
      {
         UseShellExecute = false,
         RedirectStandardOutput = true,
         RedirectStandardError = true,
         CreateNoWindow = true,
      };
      Process.Start(psi); // fire-and-forget; broker runs as a background daemon

      // Wait until the broker responds or the timeout elapses.
      var deadline = DateTime.UtcNow.AddMilliseconds(startupTimeoutMs);
      while (DateTime.UtcNow < deadline)
      {
         await Task.Delay(500);
         try
         {
            var resp = await http.GetAsync(
               $"http://localhost:{DevFlowBrokerClient.BrokerPort}/api/agents");
            if (resp.IsSuccessStatusCode)
               return;
         }
         catch { }
      }
   }

   /// <summary>
   /// Runs a <c>maui devflow</c> sub-command targeting the given agent's port
   /// and returns (exitCode, stdout, stderr).
   /// Only available on platforms where the <c>maui</c> CLI is installed (Windows).
   /// </summary>
   public static async Task<(int ExitCode, string Stdout, string Stderr)> RunDevFlowAsync(
      string arguments, DevFlowAgent agent)
   {
      var psi = new ProcessStartInfo("maui", $"devflow --agent-port {agent.Port} {arguments}")
      {
         UseShellExecute = false,
         RedirectStandardOutput = true,
         RedirectStandardError = true,
         CreateNoWindow = true,
      };

      using var process = Process.Start(psi)
         ?? throw new InvalidOperationException("Failed to start maui devflow process.");

      var stdoutTask = process.StandardOutput.ReadToEndAsync();
      var stderrTask = process.StandardError.ReadToEndAsync();
      await process.WaitForExitAsync();

      return (process.ExitCode, await stdoutTask, await stderrTask);
   }
#endif

   /// <summary>
   /// Runs an <c>adb</c> command and returns (stdout, stderr).
   /// Prefers the ANDROID_HOME / ANDROID_SDK_ROOT environment variable,
   /// falling back to the common Windows install path and then PATH.
   /// </summary>
   public static async Task<(string Stdout, string Stderr)> RunAdbAsync(string arguments)
   {
      // Prefer ANDROID_HOME env var, fall back to common Windows install path.
      var sdkRoot = Environment.GetEnvironmentVariable("ANDROID_HOME")
         ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT")
         ?? @"C:\Program Files (x86)\Android\android-sdk";
      var adbPath = Path.Combine(sdkRoot, "platform-tools", "adb.exe");
      if (!File.Exists(adbPath))
         adbPath = "adb";  // fall back to PATH

      var psi = new ProcessStartInfo(adbPath, arguments)
      {
         UseShellExecute = false,
         RedirectStandardOutput = true,
         RedirectStandardError = true,
         CreateNoWindow = true,
      };
      using var proc = Process.Start(psi)
         ?? throw new InvalidOperationException("Failed to start adb.");
      var stdout = await proc.StandardOutput.ReadToEndAsync();
      var stderr = await proc.StandardError.ReadToEndAsync();
      await proc.WaitForExitAsync();
      return (stdout, stderr);
   }
}

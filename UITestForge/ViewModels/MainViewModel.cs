using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using UITestForge.Helpers;
using UITestForge.Models;

namespace UITestForge.ViewModels;

public partial class MainViewModel : ObservableObject
{
   private const int PollIntervalMs = 5_000;

   private CancellationTokenSource? _cts;
   private bool _isRefreshing;

   // Observable Collections
   public ObservableCollection<DevFlowAgent> Agents { get; } = [];
   public ObservableCollection<TreeNodeItem> TreeItems { get; } = [];

   // Observable Properties
   [ObservableProperty]
   private DevFlowAgent? _selectedAgent;

   [ObservableProperty]
   private TreeNodeItem? _selectedTreeNode;

   [ObservableProperty]
   private string _statusText = "Press refresh if picker is empty";

   [ObservableProperty]
   private string _actionStatusText = "Select an agent above";

   [ObservableProperty]
   private string _monitorButtonText = "Start";

   [ObservableProperty]
   private bool _isBusy;

   [ObservableProperty]
   private bool _isMonitoring;

   [ObservableProperty]
   private bool _tapCounterEnabled;

   [ObservableProperty]
   private bool _screenshotImageVisible;

   [ObservableProperty]
   private bool _screenshotRefreshButtonVisible;

   [ObservableProperty]
   private bool _treeColumnVisible;

   [ObservableProperty]
   private bool _treeViewRefreshButtonVisible;

   [ObservableProperty]
   private bool _nodeDetailFrameVisible;

   [ObservableProperty]
   private bool _selectedAgentFrameVisible;

   [ObservableProperty]
   private string? _selectedAgentAppName;

   [ObservableProperty]
   private string? _selectedAgentPlatform;

   [ObservableProperty]
   private string? _selectedAgentTfm;

   [ObservableProperty]
   private string? _selectedAgentConnectedAt;

   [ObservableProperty]
   private string _scriptStatusText = "Ready";

   [ObservableProperty]
   private string _scriptOutputText = "(output will appear here)";

   [ObservableProperty]
   private string? _screenshotImageSource;

   [ObservableProperty]
   private string? _lastScreenshotPath;

   // Events for View interactions
   public event EventHandler? AgentsCollectionChanged;
   public event EventHandler<string>? ScreenshotCaptured;
   public event EventHandler? TreeLoaded;
   public event Action<bool>? AdbForwardButtonEnabledChanged;

   string AppTitle = "UITestForge";
   public string DataFolder { get; set; } = "";
   public Params Config { get; set; } = new Params();

   [ObservableProperty]
   private string _pageName = "";

   // Constructor
   public MainViewModel()
   {
      if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
      {
         DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), AppTitle);
      }

      if (DeviceInfo.Current.Platform == DevicePlatform.MacCatalyst)
      {
         DataFolder = Path.Combine(FileSystem.AppDataDirectory, AppTitle);
      }

      if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
      {
         DataFolder = FileSystem.AppDataDirectory + @"/";
      }

      if (DeviceInfo.Current.Platform == DevicePlatform.Android)
      {
         DataFolder = FileSystem.AppDataDirectory + @"/";
      }

      if (!Directory.Exists(DataFolder))
      {
         Directory.CreateDirectory(DataFolder);
      }

      // - - -  - - - 

      System.Diagnostics.Debug.WriteLine($"Data:   {DataFolder}");
   }

   // - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  - -  -

   public bool Load()
   {
      // - - - config - - -

      {
         string FileName = Path.Combine(DataFolder, AppTitle + @".Params.json");

         if (File.Exists(FileName))
         {
            string json = File.ReadAllText(FileName);

            var p = System.Text.Json.JsonSerializer.Deserialize<Params>(json);
            if (p != null)
            {
               Config = p;
            }

            if (!string.IsNullOrEmpty(p.DataFolder)
               && Directory.Exists(p.DataFolder)
               && DataFolder != p.DataFolder)
            {
               DataFolder = p.DataFolder;
               Load();
            }
         }
         else
         {
            Save();
         }
      }

      // - - -  - - -

      return true;
   }

   public bool Save()
   {
      var options = new JsonSerializerOptions
      {
         Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.All),
         WriteIndented = true
      };

      try
      {
         string FileName = Path.Combine(DataFolder, AppTitle + @".Params.json");
         var json = System.Text.Json.JsonSerializer.Serialize(Config, options);
         File.WriteAllText(FileName, json);
      }
      catch { }

      return true;
   }

   // ── Monitor Commands ────────────────────────────────────────────────────────

   [RelayCommand]
   private void ToggleMonitoring()
   {
      if (_cts is null)
         StartMonitoring();
      else
         StopMonitoring();
   }

   [RelayCommand]
   private async Task RefreshAgentsAsync()
   {
      try
      {
         IsBusy = true;
         StatusText = "Refreshing agents...";

#if !ANDROID
         await DevFlowCliHelper.EnsureBrokerStartedAsync();
#endif

         var agents = await DevFlowBrokerClient.FetchAgentsAsync(CancellationToken.None);

         await MainThread.InvokeOnMainThreadAsync(() =>
         {
            var previousSelectedId = SelectedAgent?.Id;
            var incoming = agents ?? [];
            var selectionLost = false;
            var collectionChanged = false;

            _isRefreshing = true;
            try
            {
               // Remove agents that are no longer present
               for (int i = Agents.Count - 1; i >= 0; i--)
               {
                  if (!incoming.Any(a => a.Id == Agents[i].Id))
                  {
                     Agents.RemoveAt(i);
                     collectionChanged = true;
                  }
               }

               // Update existing agents and append new ones
               for (int i = 0; i < incoming.Count; i++)
               {
                  var fresh = incoming[i];
                  var existing = Agents.FirstOrDefault(a => a.Id == fresh.Id);
                  if (existing is not null)
                  {
                     existing.Project = fresh.Project;
                     existing.Tfm = fresh.Tfm;
                     existing.Platform = fresh.Platform;
                     existing.AppName = fresh.AppName;
                     existing.Port = fresh.Port;
                     existing.Version = fresh.Version;
                     existing.SessionId = fresh.SessionId;
                     existing.ConnectedAt = fresh.ConnectedAt;
                  }
                  else
                  {
                     Agents.Insert(i, fresh);
                     collectionChanged = true;
                     if (fresh.Id == previousSelectedId)
                        selectionLost = true;
                  }
               }
            }
            finally
            {
               _isRefreshing = false;
            }

            if (collectionChanged)
            {
               AgentsCollectionChanged?.Invoke(this, EventArgs.Empty);
            }

            StatusText = Agents.Count > 0
                    ? $"Manual refresh: {DateTime.Now:HH:mm:ss}  —  {Agents.Count} agent(s)"
                    : $"Manual refresh: {DateTime.Now:HH:mm:ss}  —  No agents connected";

            if (previousSelectedId is not null &&
                    (collectionChanged || SelectedAgent is null))
               selectionLost = true;

            if (selectionLost)
            {
               var restored = Agents.FirstOrDefault(a => a.Id == previousSelectedId);
               if (restored is not null)
                  SelectedAgent = restored;
            }
         });
      }
      catch (Exception ex)
      {
         await MainThread.InvokeOnMainThreadAsync(() =>
             StatusText = $"Refresh error: {ex.Message}");
      }
      finally
      {
         IsBusy = false;
      }
   }

   private void StartMonitoring()
   {
      _cts = new CancellationTokenSource();
      MonitorButtonText = "Stop";
      StatusText = "Connecting to broker…";
      IsMonitoring = true;
      _ = MonitorDevFlowAppAsync(_cts.Token);
   }

   private void StopMonitoring()
   {
      _cts?.Cancel();
      _cts = null;
      MonitorButtonText = "Start";
      StatusText = "Monitoring stopped.";
      IsMonitoring = false;
   }

   private async Task MonitorDevFlowAppAsync(CancellationToken ct)
   {
#if !ANDROID
      await DevFlowCliHelper.EnsureBrokerStartedAsync();
#endif
      while (!ct.IsCancellationRequested)
      {
         try
         {
            var agents = await DevFlowBrokerClient.FetchAgentsAsync(ct);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
               var previousSelectedId = SelectedAgent?.Id;
               var incoming = agents ?? [];
               var selectionLost = false;
               var collectionChanged = false;

               _isRefreshing = true;
               try
               {
                  // Remove agents that are no longer present
                  for (int i = Agents.Count - 1; i >= 0; i--)
                  {
                     if (!incoming.Any(a => a.Id == Agents[i].Id))
                     {
                        Agents.RemoveAt(i);
                        collectionChanged = true;
                     }
                  }

                  // Update existing agents and append new ones
                  for (int i = 0; i < incoming.Count; i++)
                  {
                     var fresh = incoming[i];
                     var existing = Agents.FirstOrDefault(a => a.Id == fresh.Id);
                     if (existing is not null)
                     {
                        existing.Project = fresh.Project;
                        existing.Tfm = fresh.Tfm;
                        existing.Platform = fresh.Platform;
                        existing.AppName = fresh.AppName;
                        existing.Port = fresh.Port;
                        existing.Version = fresh.Version;
                        existing.SessionId = fresh.SessionId;
                        existing.ConnectedAt = fresh.ConnectedAt;
                     }
                     else
                     {
                        Agents.Insert(i, fresh);
                        collectionChanged = true;
                        if (fresh.Id == previousSelectedId)
                           selectionLost = true;
                     }
                  }
               }
               finally
               {
                  _isRefreshing = false;
               }

               if (collectionChanged)
               {
                  AgentsCollectionChanged?.Invoke(this, EventArgs.Empty);
               }

               StatusText = Agents.Count > 0
                       ? $"Last refresh: {DateTime.Now:HH:mm:ss}  —  {Agents.Count} agent(s)"
                       : $"Last refresh: {DateTime.Now:HH:mm:ss}  —  No agents connected";

               if (previousSelectedId is not null &&
                       (collectionChanged || SelectedAgent is null))
                  selectionLost = true;

               if (selectionLost)
               {
                  var restored = Agents.FirstOrDefault(a => a.Id == previousSelectedId);
                  if (restored is not null)
                     SelectedAgent = restored;
               }
            });
         }
         catch (OperationCanceledException)
         {
            break;
         }
         catch (Exception ex)
         {
            await MainThread.InvokeOnMainThreadAsync(() =>
                StatusText = $"Broker error: {ex.Message}");
         }

         try
         {
            await Task.Delay(PollIntervalMs, ct);
         }
         catch (OperationCanceledException)
         {
            break;
         }
      }
   }

   // ── Agent Selection ─────────────────────────────────────────────────────────

   partial void OnSelectedAgentChanged(DevFlowAgent? value)
   {
      var hasAgent = value is not null;
      SelectedAgentFrameVisible = hasAgent;

      if (hasAgent)
      {
         SelectedAgentAppName = value!.AppName;
         SelectedAgentPlatform = value.Platform;
         SelectedAgentTfm = value.Tfm;
         SelectedAgentConnectedAt = $"Connected: {value.ConnectedAt}";
      }

#if ANDROID
        TapCounterEnabled = false;
        ActionStatusText = hasAgent
            ? $"Agent: {value!.AppName} ({value.Platform}) — Screenshot/Tree/Tap require Windows"
            : "Select an agent below";
#else
      TapCounterEnabled = hasAgent;
      ActionStatusText = hasAgent
          ? $"Agent: {value!.AppName} ({value.Platform})"
          : "Select an agent below";

      if (hasAgent)
         _ = ForwardAndroidAgentPortAsync(value!);
#endif

      HideResults();

      if (hasAgent)
      {
         _ = TakeScreenshotAndRefreshTreeAsync();
      }
   }

   partial void OnSelectedTreeNodeChanged(TreeNodeItem? value)
   {
      NodeDetailFrameVisible = value is not null;
   }

   private async Task TakeScreenshotAndRefreshTreeAsync()
   {
#if !ANDROID
      if (SelectedAgent is null) return;

      await TakeScreenshotAsync();
      await Task.Delay(500);
      await RefreshTreeAsync();
#endif
   }

   // ── ADB Port Forward ────────────────────────────────────────────────────────

   private async Task ForwardAndroidAgentPortAsync(DevFlowAgent agent)
   {
      var error = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(agent);
      if (error is not null)
         await MainThread.InvokeOnMainThreadAsync(() =>
             ActionStatusText = $"ADB forward: {error}");
   }

   [RelayCommand]
   private async Task AdbForwardAsync()
   {
      AdbForwardButtonEnabledChanged?.Invoke(false);
      ActionStatusText = "Setting up ADB port forward…";
      try
      {
         var (devices, err) = await DevFlowCliHelper.RunAdbAsync("devices");
         if (err.Length > 0 && devices.Length == 0)
         {
            ActionStatusText = $"adb error: {err.Trim()}";
            return;
         }

         var serials = devices
             .Split('\n', StringSplitOptions.RemoveEmptyEntries)
             .Skip(1)
             .Select(l => l.Split('\t'))
             .Where(p => p.Length >= 2 && p[1].Trim() == "device")
             .Select(p => p[0].Trim())
             .Where(s => s.StartsWith("emulator-"))
             .ToList();

         if (serials.Count == 0)
         {
            ActionStatusText = "No Android emulators found. Start an emulator first.";
            return;
         }

         var results = new List<string>();
         foreach (var serial in serials)
         {
            var (stdout, stderr) = await DevFlowCliHelper.RunAdbAsync($"-s {serial} reverse tcp:{DevFlowBrokerClient.BrokerPort} tcp:{DevFlowBrokerClient.BrokerPort}");
            var line = stdout.Trim().Length > 0 ? stdout.Trim() : stderr.Trim();
            results.Add($"{serial}: {(line.Length > 0 ? line : "ok")}");
         }
         ActionStatusText = "ADB forward: " + string.Join(" | ", results);
      }
      catch (Exception ex)
      {
         ActionStatusText = $"ADB error: {ex.Message}";
      }
      finally
      {
         AdbForwardButtonEnabledChanged?.Invoke(true);
      }
   }

   // ── Screenshot ──────────────────────────────────────────────────────────────

   [RelayCommand]
   private async Task TakeScreenshotAsync()
   {
#if ANDROID
        ActionStatusText = "Screenshot requires running UITestForge on Windows.";
        return;
#else
      if (SelectedAgent is null) return;

      SetBusy(true, "Taking screenshot…");

      try
      {
         var forwardError = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(SelectedAgent);
         if (forwardError is not null)
         {
            ActionStatusText = $"Screenshot failed: {forwardError}";
            return;
         }

         var tmpPath = System.IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(), ".png");
         LastScreenshotPath = tmpPath;

         var (exitCode, _, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
             $"ui screenshot --output \"{tmpPath}\" --overwrite --verbose",
             SelectedAgent);

         if (exitCode == 0 && File.Exists(tmpPath))
         {
            ScreenshotImageSource = tmpPath;
            ScreenshotImageVisible = true;
            ScreenshotRefreshButtonVisible = true;
            ActionStatusText = $"Screenshot captured — {new FileInfo(tmpPath).Length / 1024} KB";
            ScreenshotCaptured?.Invoke(this, tmpPath);
         }
         else
         {
            ScreenshotImageVisible = false;
            string stderrMessage = stderr.Trim();
            try
            {
               var errorObj = JsonDocument.Parse(stderrMessage);
               if (errorObj.RootElement.TryGetProperty("error", out var errorProp))
                  stderrMessage = errorProp.GetString() ?? stderrMessage;
            }
            catch { }
            ActionStatusText = $"Screenshot failed: {stderrMessage}";
         }
      }
      catch (Exception ex)
      {
         ActionStatusText = $"Error: {ex.Message}";
      }
      finally
      {
         SetBusy(false);
      }
#endif
   }

   // ── Visual Tree ─────────────────────────────────────────────────────────────

   [RelayCommand]
   private async Task RefreshTreeAsync()
   {
#if ANDROID
        ActionStatusText = "Visual tree requires running UITestForge on Windows.";
        return;
#else
      if (SelectedAgent is null) return;

      SetBusy(true, "Fetching visual tree…");

      try
      {
         var forwardError = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(SelectedAgent);
         if (forwardError is not null)
         {
            ActionStatusText = $"Tree failed: {forwardError}";
            return;
         }

         var (exitCode, stdout, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
             "ui tree",
             SelectedAgent);

         if (exitCode == 0 && stdout.Length > 0)
         {
            var roots = JsonSerializer.Deserialize(
                stdout, DevFlowJsonContext.Default.ListTreeNode);

            TreeItems.Clear();
            SelectedTreeNode = null;
            if (roots is { Count: > 0 })
            {
               foreach (var root in roots)
                  FlattenTree(root, depth: 0, expandDepth: 2);

               ActionStatusText = $"Tree loaded — {TreeItems.Count} nodes";
            }
            else
            {
               ActionStatusText = "Tree loaded — 0 nodes";
            }

            if (TreeItems.Count>1 && TreeItems[0].DisplayType== "Window" )
            {
               PageName = TreeItems[1].DisplayType;
            }
            else
            {
               PageName = string.Empty;
            }

            TreeColumnVisible = true;
            NodeDetailFrameVisible = true;
            TreeViewRefreshButtonVisible = true;
            TreeLoaded?.Invoke(this, EventArgs.Empty);
         }
         else
         {
            TreeColumnVisible = false;
            ActionStatusText = $"Tree failed: {stderr.Trim()}";
         }
      }
      catch (Exception ex)
      {
         ActionStatusText = $"Error: {ex.Message}";
      }
      finally
      {
         SetBusy(false);
      }
#endif
   }

   /// <summary>
   /// Refreshes the visual tree and returns the current page name.
   /// This is used by script commands like checkpage and checknpage.
   /// </summary>
   internal async Task<string?> RefreshAndGetPageNameAsync()
   {
#if ANDROID
      return null;
#else
      if (SelectedAgent is null) return null;

      try
      {
         var forwardError = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(SelectedAgent);
         if (forwardError is not null)
            return PageName;

         var (exitCode, stdout, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
             "ui tree",
             SelectedAgent);

         if (exitCode == 0 && stdout.Length > 0)
         {
            var roots = JsonSerializer.Deserialize(
                stdout, DevFlowJsonContext.Default.ListTreeNode);

            TreeItems.Clear();
            SelectedTreeNode = null;
            if (roots is { Count: > 0 })
            {
               foreach (var root in roots)
                  FlattenTree(root, depth: 0, expandDepth: 2);
            }

            if (TreeItems.Count > 1 && TreeItems[0].DisplayType == "Window")
            {
               PageName = TreeItems[1].DisplayType;
            }
            else
            {
               PageName = string.Empty;
            }

            return PageName;
         }

         return PageName;
      }
      catch (Exception)
      {
         return PageName;
      }
#endif
   }

   // ── Tap Counter Button ──────────────────────────────────────────────────

   [RelayCommand]
   private async Task TapCounterButtonAsync()
   {
#if ANDROID
        ActionStatusText = "Tap requires running UITestForge on Windows.";
        return;
#else
      if (SelectedAgent is null) return;

      SetBusy(true, "Tapping CounterBtn…");
      try
      {
         var (exitCode, _, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
             "ui tap --automationId \"CounterBtn\"",
             SelectedAgent);

         ActionStatusText = exitCode == 0
             ? "CounterBtn tapped ✓"
             : $"Tap failed: {stderr.Trim()}";
      }
      catch (Exception ex)
      {
         ActionStatusText = $"Error: {ex.Message}";
      }
      finally
      {
         SetBusy(false);
      }
#endif
   }

   // ── Tree Operations ─────────────────────────────────────────────────────────

   public void ExpandNode(TreeNodeItem item)
       => VisualTreeHelper.ExpandNode(item, TreeItems);

   public void CollapseNode(TreeNodeItem item)
       => VisualTreeHelper.CollapseNode(item, TreeItems);

   private void FlattenTree(TreeNode node, int depth, int expandDepth)
       => VisualTreeHelper.FlattenTree(node, depth, expandDepth, TreeItems);

   public void ExpandAllNodes()
   {
      int previousCount;
      do
      {
         previousCount = TreeItems.Count;
         var nodesToExpand = TreeItems.Where(item => item.HasChildren && !item.IsExpanded).ToList();
         foreach (var item in nodesToExpand)
            ExpandNode(item);
      } while (TreeItems.Count > previousCount);
   }

   // ── Helpers ─────────────────────────────────────────────────────────────────

   public void SetBusy(bool busy, string? message = null)
   {
      IsBusy = busy;
      TapCounterEnabled = !busy && SelectedAgent is not null;
      if (message is not null)
         ActionStatusText = message;
   }

   private void HideResults()
   {
      ScreenshotImageVisible = false;
      TreeColumnVisible = false;
      TreeItems.Clear();
      SelectedTreeNode = null;
   }

   // ── Script Operations (delegated to View for file/UI interactions) ─────────

   public void UpdateScriptStatus(string status)
   {
      ScriptStatusText = status;
   }

   public void UpdateScriptOutput(string output)
   {
      ScriptOutputText = output;
   }

   public void ClearScript()
   {
      ScriptOutputText = "(output will appear here)";
      ScriptStatusText = "Ready";
   }
}

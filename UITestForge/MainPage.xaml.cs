using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using UITestForge.Helpers;

namespace UITestForge
{
   public partial class MainPage : ContentPage, INotifyPropertyChanged
   {
      private const int PollIntervalMs = 5_000;

      private CancellationTokenSource? _cts;
      private DevFlowAgent? _selectedAgent;
      private string? _lastScreenshotPath;
      private bool _isRefreshing;

      public ObservableCollection<DevFlowAgent> Agents { get; } = [];
      public ObservableCollection<TreeNodeItem> TreeItems { get; } = [];

      private TreeNodeItem? _selectedTreeNode;
      public TreeNodeItem? SelectedTreeNode
      {
         get => _selectedTreeNode;
         set
         {
            _selectedTreeNode = value;
            OnPropertyChanged();
            NodeDetailFrame.IsVisible = value is not null;
         }
      }

      public event PropertyChangedEventHandler? PropertyChanged;
      private void OnPropertyChanged([CallerMemberName] string? name = null)
         => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

      public MainPage()
      {
         InitializeComponent();
         BindingContext = this;
      }

      // ── Monitor start/stop ───────────────────────────────────────────────

      private void OnMonitorClicked(object? sender, EventArgs e)
      {
         if (_cts is null)
            StartMonitoring();
         else
            StopMonitoring();
      }

      private void StartMonitoring()
      {
         _cts = new CancellationTokenSource();
         MonitorBtn.Text = "Stop";
         StatusLabel.Text = "Connecting to broker…";
         _ = MonitorDevFlowApp(_cts.Token);
      }

      private void StopMonitoring()
      {
         _cts?.Cancel();
         _cts = null;
         MonitorBtn.Text = "Start";
         StatusLabel.Text = "Monitoring stopped.";
      }

      /// <summary>
      /// Polls the DevFlow broker every 5 seconds and refreshes the agent list.
      /// </summary>
      private async Task MonitorDevFlowApp(CancellationToken ct)
      {
         while (!ct.IsCancellationRequested)
         {
            try
            {
               var agents = await DevFlowBrokerClient.FetchAgentsAsync(ct);

               MainThread.BeginInvokeOnMainThread(() =>
               {
                  var previousSelectedId = _selectedAgent?.Id;
                  var incoming = agents ?? [];
                  var selectionLost = false;

                  _isRefreshing = true;
                  try
                  {
                     // Remove agents that are no longer present
                     for (int i = Agents.Count - 1; i >= 0; i--)
                     {
                        if (!incoming.Any(a => a.Id == Agents[i].Id))
                           Agents.RemoveAt(i);
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
                           if (fresh.Id == previousSelectedId)
                              selectionLost = true;
                        }
                     }
                  }
                  finally
                  {
                     _isRefreshing = false;
                  }

                  StatusLabel.Text = Agents.Count > 0
                     ? $"Last refresh: {DateTime.Now:HH:mm:ss}  —  {Agents.Count} agent(s)"
                     : $"Last refresh: {DateTime.Now:HH:mm:ss}  —  No agents connected";

                  // Check if the CollectionView silently dropped selection during mutations
                  if (previousSelectedId is not null && AgentsPicker.SelectedItem is null)
                     selectionLost = true;

                  // Only restore selection when the item instance was actually replaced
                  if (selectionLost)
                  {
                     var restored = Agents.FirstOrDefault(a => a.Id == previousSelectedId);
                     if (restored is not null)
                        AgentsPicker.SelectedItem = restored;
                  }
               });
            }
            catch (OperationCanceledException)
            {
               break;
            }
            catch (Exception ex)
            {
               MainThread.BeginInvokeOnMainThread(() =>
                  StatusLabel.Text = $"Broker error: {ex.Message}");
            }

            try { await Task.Delay(PollIntervalMs, ct); }
            catch (OperationCanceledException) { break; }
         }

         _cts = null;
         MainThread.BeginInvokeOnMainThread(() =>
         {
            MonitorBtn.Text = "Start";
            StatusLabel.Text = "Monitoring stopped.";
         });
      }

      // ── Agent selection ──────────────────────────────────────────────────

      private void OnAgentSelectionChanged(object? sender, EventArgs e)
      {
         // Ignore spurious deselection events fired while the collection is being mutated.
         if (_isRefreshing) return;

         _selectedAgent = AgentsPicker.SelectedItem as DevFlowAgent;
         bool hasAgent = _selectedAgent is not null;

         SelectedAgentFrame.IsVisible = hasAgent;
         if (hasAgent)
         {
            SelectedAgentAppName.Text = _selectedAgent!.AppName;
            SelectedAgentPlatform.Text = _selectedAgent.Platform;
            SelectedAgentTfm.Text = _selectedAgent.Tfm;
            SelectedAgentConnectedAt.Text = $"Connected: {_selectedAgent.ConnectedAt}";
         }

#if ANDROID
         // The maui devflow CLI is not available on Android, so Screenshot/Tree cannot be run.
         ScreenshotBtn.IsEnabled = false;
         TreeBtn.IsEnabled = false;
         TapCounterBtn.IsEnabled = false;
         ActionStatusLabel.Text = hasAgent
            ? $"Agent: {_selectedAgent!.AppName} ({_selectedAgent.Platform}) — Screenshot/Tree/Tap require Windows"
            : "Select an agent below";
#else
         ScreenshotBtn.IsEnabled = hasAgent;
         TreeBtn.IsEnabled = hasAgent;
         TapCounterBtn.IsEnabled = hasAgent;
         ActionStatusLabel.Text = hasAgent
            ? $"Agent: {_selectedAgent!.AppName} ({_selectedAgent.Platform})"
            : "Select an agent below";
#endif

         HideResults();
      }

      // ── ADB port forward ──────────────────────────────────────────────────

      /// <summary>
      /// Runs <c>adb -s {serial} reverse tcp:19223 tcp:19223</c> for every connected
      /// Android emulator so the DevFlow agent inside the emulator can reach the broker
      /// on the host at localhost:19223.
      /// </summary>
      private async void OnAdbForwardClicked(object? sender, EventArgs e)
      {
         AdbForwardBtn.IsEnabled = false;
         ActionStatusLabel.Text = "Setting up ADB port forward…";
         try
         {
            var (devices, err) = await DevFlowCliHelper.RunAdbAsync("devices");
            if (err.Length > 0 && devices.Length == 0)
            {
               ActionStatusLabel.Text = $"adb error: {err.Trim()}";
               return;
            }

            // Parse "emulator-XXXX\tdevice" lines
            var serials = devices
               .Split('\n', StringSplitOptions.RemoveEmptyEntries)
               .Skip(1)  // skip "List of devices attached" header
               .Select(l => l.Split('\t'))
               .Where(p => p.Length >= 2 && p[1].Trim() == "device")
               .Select(p => p[0].Trim())
               .Where(s => s.StartsWith("emulator-"))
               .ToList();

            if (serials.Count == 0)
            {
               ActionStatusLabel.Text = "No Android emulators found. Start an emulator first.";
               return;
            }

            var results = new List<string>();
            foreach (var serial in serials)
            {
               var (stdout, stderr) = await DevFlowCliHelper.RunAdbAsync($"-s {serial} reverse tcp:{DevFlowBrokerClient.BrokerPort} tcp:{DevFlowBrokerClient.BrokerPort}");
               var line = stdout.Trim().Length > 0 ? stdout.Trim() : stderr.Trim();
               results.Add($"{serial}: {(line.Length > 0 ? line : "ok")}");
            }
            ActionStatusLabel.Text = "ADB forward: " + string.Join(" | ", results);
         }
         catch (Exception ex)
         {
            ActionStatusLabel.Text = $"ADB error: {ex.Message}";
         }
         finally
         {
            AdbForwardBtn.IsEnabled = true;
         }
      }



      // ── Screenshot ───────────────────────────────────────────────────────

      private async void OnScreenshotClicked(object? sender, EventArgs e)
      {
#if ANDROID
         ActionStatusLabel.Text = "Screenshot requires running UITestForge on Windows.";
         return;
#else
         if (_selectedAgent is null) return;

         SetBusy(true, "Taking screenshot…");
         TreeColumn.IsVisible = false;

         try
         {
            var tmpPath = Path.ChangeExtension(Path.GetTempFileName(), ".png");
            _lastScreenshotPath = tmpPath;

            var (exitCode, _, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
               $"ui screenshot --output \"{tmpPath}\" --overwrite",
               _selectedAgent);

            if (exitCode == 0 && File.Exists(tmpPath))
            {
               ScreenshotImage.Source = ImageSource.FromFile(tmpPath);
               ScreenshotImage.IsVisible = true;
               ActionStatusLabel.Text = $"Screenshot captured — {new FileInfo(tmpPath).Length / 1024} KB";
            }
            else
            {
               ScreenshotImage.IsVisible = false;
               ActionStatusLabel.Text = $"Screenshot failed: {stderr.Trim()}";
            }
         }
         catch (Exception ex)
         {
            ActionStatusLabel.Text = $"Error: {ex.Message}";
         }
         finally
         {
            SetBusy(false);
         }
#endif
      }

      // ── Visual tree ──────────────────────────────────────────────────────

      private async void OnTreeClicked(object? sender, EventArgs e)
      {
#if ANDROID
         ActionStatusLabel.Text = "Visual tree requires running UITestForge on Windows.";
         return;
#else
         if (_selectedAgent is null) return;

         SetBusy(true, "Fetching visual tree…");
         ScreenshotImage.IsVisible = false;

         try
         {
            var (exitCode, stdout, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
               "ui tree --depth 0",
               _selectedAgent);

            if (exitCode == 0 && stdout.Length > 0)
            {
               var roots = JsonSerializer.Deserialize(
                  stdout, DevFlowJsonContext.Default.ListTreeNode);

               TreeItems.Clear();
               SelectedTreeNode = null;
               if (roots is { Count: > 0 })
               {
                  foreach (var root in roots)
                     FlattenTree(root, 0, expandDepth: 1);
               }
               TreeColumn.IsVisible = true;
               ActionStatusLabel.Text = $"Tree loaded — {TreeItems.Count} nodes";
            }
            else
            {
               TreeColumn.IsVisible = false;
               ActionStatusLabel.Text = $"Tree failed: {stderr.Trim()}";
            }
         }
         catch (Exception ex)
         {
            ActionStatusLabel.Text = $"Error: {ex.Message}";
         }
         finally
         {
            SetBusy(false);
         }
#endif
      }

      // ── Helpers ──────────────────────────────────────────────────────────



      // ── Tap CounterBtn ────────────────────────────────────────────────────

      private async void OnTapCounterBtnClicked(object? sender, EventArgs e)
      {
#if ANDROID
         ActionStatusLabel.Text = "Tap requires running UITestForge on Windows.";
         return;
#else
         if (_selectedAgent is null) return;

         SetBusy(true, "Tapping CounterBtn…");
         try
         {
            var (exitCode, _, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
               "ui tap --automationId \"CounterBtn\"",
               _selectedAgent);

            ActionStatusLabel.Text = exitCode == 0
               ? "CounterBtn tapped ✓"
               : $"Tap failed: {stderr.Trim()}";
         }
         catch (Exception ex)
         {
            ActionStatusLabel.Text = $"Error: {ex.Message}";
         }
         finally
         {
            SetBusy(false);
         }
#endif
      }

      private async void OnTapCounterBtnClickedHTTP(object? sender, EventArgs e)
      {
         var agent = _selectedAgent;
         if (agent is null) return;

         SetBusy(true, "Tapping CounterBtn…");
         try
         {
            ActionResponse? result = null;
            HttpRequestException? lastHttpEx = null;

            try
            {
               //StopMonitoring();

               var elementId = "CounterBtn";

               //var elementId = await DevFlowAgentClient.FindElementByAutomationIdAsync(
               //   agent, "CounterBtn");

               //if (elementId is null)
               //{
               //   ActionStatusLabel.Text = "CounterBtn not found in visual tree.";
               //   return;
               //}

               result = await DevFlowAgentClient.TapElementAsync(agent, elementId);
            }
            catch (HttpRequestException httpEx)
            {
               // Agent connection dropped prematurely; wait briefly and retry.
               lastHttpEx = httpEx;
            }

            if (result is null)
               throw new InvalidOperationException(
                  $"All tap attempts failed. Last error: {lastHttpEx?.Message}", lastHttpEx);

            ActionStatusLabel.Text = result.Success
               ? "CounterBtn tapped ✓"
               : $"Tap failed: {result.Error?.ErrorCode ?? result.Error?.Title ?? "unknown error"}";
         }
         catch (Exception ex)
         {
            ActionStatusLabel.Text = $"Error: {ex.Message}";
         }
         finally
         {
            SetBusy(false);
         }
      }

      // ── Tree expand / collapse / selection ──────────────────────────────────

      private void OnTreeNodeSelected(object? sender, SelectionChangedEventArgs e)
         => SelectedTreeNode = e.CurrentSelection.FirstOrDefault() as TreeNodeItem;

      private void OnExpandToggled(object? sender, TappedEventArgs e)
      {
         if (e.Parameter is not TreeNodeItem item || !item.HasChildren) return;
         if (item.IsExpanded) CollapseNode(item);
         else ExpandNode(item);
      }

      /// <summary>Inserts the immediate children of <paramref name="item"/> into the flat list.</summary>
      private void ExpandNode(TreeNodeItem item)
         => VisualTreeHelper.ExpandNode(item, TreeItems);

      private void CollapseNode(TreeNodeItem item)
         => VisualTreeHelper.CollapseNode(item, TreeItems);

      private void FlattenTree(TreeNode node, int depth, int expandDepth)
         => VisualTreeHelper.FlattenTree(node, depth, expandDepth, TreeItems);

      private void SetBusy(bool busy, string? message = null)
      {
         Busy.IsRunning = busy;
         Busy.IsVisible = busy;
         ScreenshotBtn.IsEnabled = !busy && _selectedAgent is not null;
         TreeBtn.IsEnabled = !busy && _selectedAgent is not null;
         TapCounterBtn.IsEnabled = !busy && _selectedAgent is not null;
         if (message is not null)
            ActionStatusLabel.Text = message;
      }

      private void HideResults()
      {
         ScreenshotImage.IsVisible = false;
         TreeColumn.IsVisible = false;
         TreeItems.Clear();
         SelectedTreeNode = null;
      }
   }
}



using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using UITestForge.Helpers;
using UITestForge.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;

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
#if !ANDROID
         await DevFlowCliHelper.EnsureBrokerStartedAsync();
#endif
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

                  // MAUI's Picker doesn't always sync its dropdown list when the bound
                  // ObservableCollection is mutated; resetting ItemsSource forces a rebuild.
                  if (collectionChanged)
                  {
                     AgentsPicker.ItemsSource = null;
                     AgentsPicker.ItemsSource = Agents;
                  }

                  StatusLabel.Text = Agents.Count > 0
                     ? $"Last refresh: {DateTime.Now:HH:mm:ss}  —  {Agents.Count} agent(s)"
                     : $"Last refresh: {DateTime.Now:HH:mm:ss}  —  No agents connected";

                  // After an ItemsSource reset the Picker always clears its selection;
                  // also catch cases where the collection mutations silently dropped it.
                  if (previousSelectedId is not null &&
                      (collectionChanged || AgentsPicker.SelectedItem is null))
                     selectionLost = true;

                  // Restore selection when the item instance was replaced or lost
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
         TapCounterBtn.IsEnabled = false;
         ActionStatusLabel.Text = hasAgent
            ? $"Agent: {_selectedAgent!.AppName} ({_selectedAgent.Platform}) — Screenshot/Tree/Tap require Windows"
            : "Select an agent below";
#else
         TapCounterBtn.IsEnabled = hasAgent;
         ActionStatusLabel.Text = hasAgent
            ? $"Agent: {_selectedAgent!.AppName} ({_selectedAgent.Platform})"
            : "Select an agent below";

         // For Android agents, forward the agent port from the emulator to the host
         // so that `maui devflow --agent-port` and DevFlowAgentClient can reach it.
         if (hasAgent)
            _ = ForwardAndroidAgentPortAsync(_selectedAgent!);
#endif

         HideResults();

         // Take screenshot and refresh tree when agent is selected
         if (hasAgent)
         {
            _ = TakeScreenshotAndRefreshTreeAsync();
         }
      }

      private async Task TakeScreenshotAndRefreshTreeAsync()
      {
#if !ANDROID
         if (_selectedAgent is null) return;

         // Take screenshot
         await TakeScreenshotAsync();

         // Wait a bit for screenshot to complete, then refresh tree
         await Task.Delay(500);

         // Refresh tree
         OnTreeClicked(null, EventArgs.Empty);
#endif
      }

      // ── ADB port forward ──────────────────────────────────────────────────

      /// <summary>
      /// Automatically forwards the selected Android agent's port from the emulator
      /// to the Windows host so CLI and HTTP calls to localhost:{port} reach the agent.
      /// </summary>
      private async Task ForwardAndroidAgentPortAsync(DevFlowAgent agent)
      {
         var error = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(agent);
         if (error is not null)
            MainThread.BeginInvokeOnMainThread(()
               => ActionStatusLabel.Text = $"ADB forward: {error}");
      }

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

      private async Task TakeScreenshotAsync()
      {
#if ANDROID
         ActionStatusLabel.Text = "Screenshot requires running UITestForge on Windows.";
         return;
#else
         if (_selectedAgent is null) return;

         SetBusy(true, "Taking screenshot…");

         try
         {
            var forwardError = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(_selectedAgent);
            if (forwardError is not null)
            {
               ActionStatusLabel.Text = $"Screenshot failed: {forwardError}";
               return;
            }

            var tmpPath = System.IO.Path.ChangeExtension(System.IO.Path.GetTempFileName(), ".png");
            _lastScreenshotPath = tmpPath;

            var (exitCode, _, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
               $"ui screenshot --output \"{tmpPath}\" --overwrite --verbose",
               _selectedAgent);

            if (exitCode == 0 && File.Exists(tmpPath))
            {
               ScreenshotImage.Source = ImageSource.FromFile(tmpPath);
               ScreenshotImage.IsVisible = true;
               ScreenshotRefreshBtn.IsVisible = true;
               ActionStatusLabel.Text = $"Screenshot captured — {new FileInfo(tmpPath).Length / 1024} KB";
            }
            else
            {
               ScreenshotImage.IsVisible = false;
               string stderrMessage = stderr.Trim();
               try
               {
                  var errorObj = JsonDocument.Parse(stderrMessage);
                  if (errorObj.RootElement.TryGetProperty("error", out var errorProp))
                     stderrMessage = errorProp.GetString() ?? stderrMessage;
               }
               catch { /* fallback to raw stderr */ }
               ActionStatusLabel.Text = $"Screenshot failed: {stderrMessage}";
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

      private async void OnScreenshotRefreshClicked(object? sender, EventArgs e)
      {
         // Simply call the existing screenshot functionality
         await TakeScreenshotAsync();
      }

      private async void OnCopyScreenshotClicked(object? sender, EventArgs e)
      {
         try
         {
            if (string.IsNullOrEmpty(_lastScreenshotPath) || !File.Exists(_lastScreenshotPath))
            {
               await DisplayAlert("Copy Failed", "No screenshot available to copy.", "OK");
               return;
            }

            // Read the image file as bytes
            var imageBytes = await File.ReadAllBytesAsync(_lastScreenshotPath);

            // Encode as base64 data URI for clipboard (fallback approach)
            var base64Image = Convert.ToBase64String(imageBytes);
            var dataUri = $"data:image/png;base64,{base64Image}";

            // Copy the file path to clipboard as text (most compatible approach)
            await Clipboard.Default.SetTextAsync(_lastScreenshotPath);

            ActionStatusLabel.Text = $"Screenshot path copied to clipboard: {System.IO.Path.GetFileName(_lastScreenshotPath)}";
         }
         catch (Exception ex)
         {
            await DisplayAlert("Copy Failed", $"Failed to copy screenshot: {ex.Message}", "OK");
         }
      }

      // ── Visual tree ────────────────────────────────────────────────────
      private void OnTreeViewRefreshClicked(object sender, EventArgs e)
      {
         OnTreeClicked(null, null);
      }

      private async void OnTreeClicked(object? sender, EventArgs e)
      {
#if ANDROID
         ActionStatusLabel.Text = "Visual tree requires running UITestForge on Windows.";
         return;
#else
         if (_selectedAgent is null) return;

         SetBusy(true, "Fetching visual tree…");

         try
         {
            var forwardError = await DevFlowCliHelper.EnsureAgentPortForwardedAsync(_selectedAgent);
            if (forwardError is not null)
            {
               ActionStatusLabel.Text = $"Tree failed: {forwardError}";
               return;
            }

            var (exitCode, stdout, stderr) = await DevFlowCliHelper.RunDevFlowAsync(
               "ui tree",
               _selectedAgent);

            if (exitCode == 0 && stdout.Length > 0)
            {
               var roots = JsonSerializer.Deserialize(
                  stdout, DevFlowJsonContext.Default.ListTreeNode);

               TreeItems.Clear();
               SelectedTreeNode = null;
               if (roots is { Count: > 0 })
               {
                  // Diagnostic: Count all node types in the full tree
                  var allTypes = new Dictionary<string, int>();
                  var allNodesFlat = new List<(TreeNode node, int depth)>();

                  void CollectNodes(TreeNode node, int depth)
                  {
                     if (!allTypes.ContainsKey(node.Type))
                        allTypes[node.Type] = 0;
                     allTypes[node.Type]++;
                     allNodesFlat.Add((node, depth));

                     if (node.Children != null)
                        foreach (var child in node.Children)
                           CollectNodes(child, depth + 1);
                  }

                  foreach (var root in roots)
                     CollectNodes(root, 0);

                  var customPickerCount = allTypes.GetValueOrDefault("CustomPicker", 0);
                  var customEntryCount = allTypes.GetValueOrDefault("CustomEntry", 0);

                  // Find the depth of CustomPicker and CustomEntry nodes
                  var customPickerDepths = allNodesFlat
                     .Where(n => n.node.Type == "CustomPicker")
                     .Select(n => n.depth)
                     .ToList();
                  var customEntryDepths = allNodesFlat
                     .Where(n => n.node.Type == "CustomEntry")
                     .Select(n => n.depth)
                     .ToList();

                  // Now flatten the tree for display (only expands to depth 1)
                  foreach (var root in roots)
                     FlattenTree(root, 0, expandDepth: 1);

                  var diagnosticInfo = "";

                  // Auto-expand all nodes if CustomPicker or CustomEntry are found but not visible
                  if ((customPickerCount > 0 || customEntryCount > 0) &&
                      !TreeItems.Any(item => item.Node.Type == "CustomPicker" || item.Node.Type == "CustomEntry"))
                  {
                     var beforeCount = TreeItems.Count;
                     // They exist but are hidden in collapsed nodes - expand everything
                     ExpandAllNodes();
                     diagnosticInfo += $" [Auto-expanded: {beforeCount} → {TreeItems.Count} nodes]";
                  }

                  if (string.IsNullOrEmpty(diagnosticInfo))
                     diagnosticInfo = " (no CustomPicker/CustomEntry found)";

                  ActionStatusLabel.Text = $"Tree loaded — {TreeItems.Count} visible.{diagnosticInfo}";
               }
               else
               {
                  ActionStatusLabel.Text = "Tree loaded — 0 nodes";
               }

               TreeColumn.IsVisible = true;
               NodeDetailFrame.IsVisible = true;
               TreeViewRefreshBtn.IsVisible = true;
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

      private async void OnTreeNodeDoubleTapped(object? sender, TappedEventArgs e)
      {
         if (e.Parameter is not TreeNodeItem item) return;

         var automationId = item.Node.AutomationId;
         if (string.IsNullOrWhiteSpace(automationId))
         {
            ActionStatusLabel.Text = "No AutomationId to copy";
            return;
         }

         try
         {
            await Clipboard.Default.SetTextAsync(automationId);
            ActionStatusLabel.Text = $"Copied AutomationId: {automationId}";
         }
         catch (Exception ex)
         {
            ActionStatusLabel.Text = $"Copy failed: {ex.Message}";
         }
      }

      /// <summary>Expands all collapsed nodes recursively to make all controls visible.</summary>
      private void ExpandAllNodes()
      {
         // Keep expanding until no more nodes can be expanded
         int previousCount;
         do
         {
            previousCount = TreeItems.Count;
            var nodesToExpand = TreeItems.Where(item => item.HasChildren && !item.IsExpanded).ToList();
            foreach (var item in nodesToExpand)
               ExpandNode(item);
         } while (TreeItems.Count > previousCount); // Continue if new nodes were added
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

      // ── Script Editor ─────────────────────────────────────────────────────────

      private async void OnScriptLoadClicked(object? sender, EventArgs e)
      {
         try
         {
            var result = await FilePicker.PickAsync(new PickOptions
            {
               PickerTitle = "Select a DevFlow script file",
               FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
               {
                  { DevicePlatform.WinUI, new[] { ".txt", ".devflow", ".script" } },
                  { DevicePlatform.macOS, new[] { "txt", "devflow", "script" } },
                  { DevicePlatform.iOS, new[] { "public.text" } },
                  { DevicePlatform.Android, new[] { "text/plain" } }
               })
            });

            if (result != null)
            {
               using var stream = await result.OpenReadAsync();
               using var reader = new StreamReader(stream);
               var content = await reader.ReadToEndAsync();

               ScriptEditor.Text = content;
               ScriptStatusLabel.Text = $"Loaded: {result.FileName}";
            }
         }
         catch (Exception ex)
         {
            ScriptStatusLabel.Text = $"Load failed: {ex.Message}";
         }
      }

      private async void OnScriptSaveClicked(object? sender, EventArgs e)
      {
         try
         {
            var scriptContent = ScriptEditor.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(scriptContent))
            {
               ScriptStatusLabel.Text = "Nothing to save.";
               return;
            }

            var defaultFileName = $"script_{DateTime.Now:yyyyMMdd_HHmmss}.devflow";
            var filePath = System.IO.Path.Combine(FileSystem.Current.CacheDirectory, defaultFileName);

            await File.WriteAllTextAsync(filePath, scriptContent);

            ScriptStatusLabel.Text = $"Saved to: {System.IO.Path.GetFileName(filePath)}";

            // Optionally show the full path in an alert
            await DisplayAlertAsync("Script Saved", 
               $"Script saved successfully to:\n{filePath}\n\nYou can find it in the app's cache directory.", 
               "OK");
         }
         catch (Exception ex)
         {
            ScriptStatusLabel.Text = $"Save failed: {ex.Message}";
            await DisplayAlertAsync("Save Error", $"Failed to save script: {ex.Message}", "OK");
         }
      }

      private void OnScriptClearClicked(object? sender, EventArgs e)
      {
         ScriptEditor.Text = string.Empty;
         ScriptOutputLabel.Text = "(output will appear here)";
         ScriptStatusLabel.Text = "Ready";
      }

      private void OnShowSyntaxHelperClicked(object? sender, EventArgs e)
      {
         var popup = new SyntaxHelpPopup();
         this.ShowPopup(popup);
      }

      private async void OnScriptRunClicked(object? sender, EventArgs e)
      {
#if ANDROID
         ScriptStatusLabel.Text = "Script execution requires running UITestForge on Windows.";
         return;
#else
         if (_selectedAgent is null)
         {
            ScriptStatusLabel.Text = "Select an agent first.";
            return;
         }

         ScriptRunBtn.IsEnabled = false;
         ScriptClearBtn.IsEnabled = false;
         SetBusy(true, "Running script…");

         try
         {
            var (stepCount, error) = await ScriptEditorHelper.RunScriptAsync(
               ScriptEditor.Text ?? string.Empty,
               _selectedAgent,
               onStepStatus: (n, cmd) => ScriptStatusLabel.Text = $"Step {n}: {cmd}…",
               onOutputUpdate: AppendOutput,
               onScreenshotCaptured: path =>
               {
                  ScreenshotImage.Source = ImageSource.FromFile(path);
                  ScreenshotImage.IsVisible = true;
                  ScreenshotRefreshBtn.IsVisible = true;
                  _lastScreenshotPath = path;
               });

            ScriptStatusLabel.Text = error is not null
               ? $"Script error: {error.Message}"
               : stepCount == 0 ? "No commands found." : $"Done — {stepCount} step(s) executed.";
         }
         finally
         {
            //PptxReportHelper.AddPage(
            //    pptxPath: "report.pptx",
            //    beforeImagePath: "before.png",
            //    afterImagePath: "after.png",
            //    executionLogs: "Step 1 passed\nStep 2 passed",
            //    scriptText: "Full test script here…",
            //    title: "Test Case 42 – Login Flow");

            SetBusy(false);
            ScriptRunBtn.IsEnabled = true;
            ScriptClearBtn.IsEnabled = true;
         }
#endif
      }

      private void AppendOutput(string text)
      {
         ScriptOutputLabel.Text = text;
         //_ = ScriptOutputScroll.ScrollToAsync(0, ScriptOutputLabel.Height, false);
      }

   }
}



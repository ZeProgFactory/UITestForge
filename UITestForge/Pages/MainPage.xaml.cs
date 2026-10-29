using System.Diagnostics;
using System.Text;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using ScriptPad.Sample;
using UITestForge.Helpers;
using UITestForge.ViewModels;
using UITestForge.Views;
using ZPF.Maui.Script;
using ZPF.Maui.Script.ScriptEditing;

namespace UITestForge;

public partial class MainPage : ContentPage
{
   private readonly MainViewModel _viewModel;
   private bool _initialLoadDone;
   private bool _editorModeInitialized;
   private bool _lastEditorNarrow;

   public MainPage()
   {
      InitializeComponent();

      _viewModel = new MainViewModel();
      BindingContext = _viewModel;

      // Wire up ViewModel events
      _viewModel.AgentsCollectionChanged += OnAgentsCollectionChanged;
      _viewModel.PropertyChanged += (_, args) =>
      {
         if (args.PropertyName is nameof(MainViewModel.SelectedTreeNode) or nameof(MainViewModel.ScreenshotImageVisible))
            MainThread.BeginInvokeOnMainThread(UpdateSelectionHighlight);
      };
      _viewModel.ScreenshotCaptured += OnScreenshotCaptured;
      _viewModel.AdbForwardButtonEnabledChanged += (enabled) => AdbForwardBtn.IsEnabled = enabled;

      // Wire up UI events
      this.Loaded += async (s, e) =>
      {
         if (_initialLoadDone)
            return;
         _initialLoadDone = true;

         _viewModel.Load();

         if (System.IO.File.Exists(_viewModel.Config.LastScript))
         {
            await ScriptEditor.LoadFileAsync(_viewModel.Config.LastScript);

            ScriptEditorView.RootPath = _viewModel.Config.ScriptFolder;
            ScriptEditorView.Highlighter = new UITestForgeScriptHighlighter();
            ScriptEditorView.Text = ScriptEditor.Text ?? string.Empty;
            ScriptEditorView.FileName = _viewModel.Config.LastScript;
            ScriptEditorView.CaretPosition = ScriptEditor.GetCaret();
            ScriptEditorView.IsModified = ScriptEditor.IsModified;
         }

         // optional user entry in the editor context menu (right click / long press).
         // The command parameter is the current selection (empty string when nothing is selected).
         ScriptEditor.CustomMenuItemText = "Run script";
         ScriptEditor.CustomMenuCommand = new Command<string>(
            async selection =>
            {
               var script = string.IsNullOrWhiteSpace(selection) ? ScriptEditor.CurrentLine : selection;
               Debug.WriteLine($"RunScript({ScriptEditor.FileName}): \"{script}\"");

               //run the script in the editor, or just the selected text if any.
               await RunScriptAsync(script);
            });

         ScriptEditor.CustomMenuItemText = "Run script";
         ScriptEditor.CustomMenuCommand = new Command<string>(
            async selection =>
            {
               var script = string.IsNullOrWhiteSpace(selection) ? ScriptEditor.CurrentLine : selection;
               Debug.WriteLine($"RunScript({ScriptEditor.FileName}): \"{script}\"");

               //run the script in the editor, or just the selected text if any.
               await RunScriptAsync(script);
            });


         ScriptEditorView.Editor.CustomMenuItemText = "Run script";
         ScriptEditorView.Editor.CustomMenuCommand = new Command<string>(
            async selection =>
            {
               var script = string.IsNullOrWhiteSpace(selection) ? ScriptEditorView.Editor.CurrentLine : selection;
               Debug.WriteLine($"RunScript({ScriptEditorView.Editor.FileName}): \"{script}\"");

               //run the script in the editor, or just the selected text if any.
               await RunScriptAsync(script);
            });

         // Example: Set specific file extensions 
         ScriptEditorView.Explorer.FileExtensions = ".txt,.md,.cs,.df";

         // Example: Show all files
         //ScriptEditorView.Explorer.FileExtensions = "*";

         // Enable subfolder display
         ScriptEditorView.Explorer.ShowSubfolders = true;

         ScriptEditorView.Explorer.CustomActionText = "Run script";
         ScriptEditorView.Explorer.FileCustomAction += async (sender, e) =>
         {
            if (!System.IO.File.Exists(e.FilePath))
            {
               _viewModel.UpdateScriptStatus($"File not found: {e.FilePath}");
               return;
            }

            var scriptContent = await System.IO.File.ReadAllTextAsync(e.FilePath);
            Debug.WriteLine($"RunScript({e.FilePath}): \"{scriptContent}\"");
            //run the script in the editor, or just the selected text if any.
            await RunScriptAsync(scriptContent);
         };

         ScriptEditorView.Toolbar.FileLoaded += (sender, e) =>
         {
            _viewModel.Config.LastScript = ScriptEditor.FileName;
         };
      };

      ScriptEditor.Theme = ScriptPadTheme.Light();          // or Dark()
      ScriptEditor.Highlighter = new UITestForgeScriptHighlighter();
      //EditorCtl.Highlighter = new MarkdownHighlighter();     // or PlainTextHighlighter / your own
   }


   // ── Collapsible panels ──────────────────────────────────────────────────────

   private void OnScreenshotExpandedChanged(object? sender, bool expanded)
      => ScreenshotColumn.Width = expanded ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;

   private void OnTreeviewExpandedChanged(object? sender, bool expanded)
      => UpdateTreeviewColumnWidth();

   private void OnPanelGridSizeChanged(object? sender, EventArgs e)
      => UpdateTreeviewColumnWidth();

   /// <summary>
   /// Keeps the tree column star-sized while it still fits, and switches it to
   /// Auto once its share of the panel would exceed <see cref="VisualElement.MaximumWidthRequest"/>
   /// of <c>TreeviewBorder</c> (extra space then goes to the other columns).
   /// </summary>
   private void UpdateTreeviewColumnWidth()
   {
      if (!TreeviewExpander.IsExpanded)
      {
         TreeviewColumn.Width = GridLength.Auto;
         return;
      }

      double max = TreeviewBorder.MaximumWidthRequest;
      double available = PanelGrid.Width;

      if (max <= 0 || available <= 0)
      {
         TreeviewColumn.Width = new GridLength(1, GridUnitType.Star);
         return;
      }

      // Count star columns and subtract the space taken by the auto (collapsed) ones.
      int starColumns = 0;
      double usedByAuto = 0;
      foreach (ColumnDefinition column in PanelGrid.ColumnDefinitions)
      {
         if (column.Width.IsStar)
            starColumns++;
         else
            usedByAuto += column.Width.IsAbsolute ? column.Width.Value : 25; // collapsed expander width
      }

      double spacing = PanelGrid.ColumnSpacing * Math.Max(0, PanelGrid.ColumnDefinitions.Count - 1);
      double perStar = starColumns > 0
         ? (available - usedByAuto - spacing) / starColumns
         : available;

      TreeviewColumn.Width = perStar >= max
         ? new GridLength(max, GridUnitType.Absolute)
         : new GridLength(1, GridUnitType.Star);
   }

   private void OnAgentsCollectionChanged(object? sender, EventArgs e)
   {
      // MAUI's Picker doesn't always sync its dropdown list when the bound
      // ObservableCollection is mutated; resetting ItemsSource forces a rebuild.
      AgentsPicker.ItemsSource = null;
      AgentsPicker.ItemsSource = _viewModel.Agents;

      // Restore selection after ItemsSource reset
      if (_viewModel.SelectedAgent is not null)
      {
         AgentsPicker.SelectedItem = _viewModel.SelectedAgent;
      }
   }

   private void OnScreenshotCaptured(object? sender, string imagePath)
   {
      ScreenshotImage.Source = ImageSource.FromFile(imagePath);
   }

   // ── Screenshot → visual tree hit testing ────────────────────────────────

   /// <summary>
   /// Maps the tapped point on the screenshot to the matching node of the
   /// visual tree and selects it (expanding its ancestors when needed).
   /// </summary>
   private void OnScreenshotTapped(object? sender, TappedEventArgs e)
   {
      if (_viewModel.TreeItems.Count == 0)
      {
         _viewModel.ActionStatusText = "No visual tree loaded";
         return;
      }

      if (e.GetPosition(ScreenshotHost) is not { } tap)
         return;

      // Inverse of the selection highlight mapping: host = offset + node * scale.
      if (GetScreenshotMapping() is not var (scale, offsetX, offsetY))
         return;

      double nodeX = (tap.X - offsetX) / scale;
      double nodeY = (tap.Y - offsetY) / scale;

      Debug.WriteLine($"Screenshot hit test: tap=({tap.X:F1}, {tap.Y:F1}) " +
         $"scale={scale:F3} offset=({offsetX:F1}, {offsetY:F1}) node=({nodeX:F1}, {nodeY:F1})");

      var path = FindNodePath(_viewModel.TreeItems
         .Where(i => i.Depth == 0)
         .Select(i => i.Node), nodeX, nodeY);

      if (path is null || path.Count == 0)
      {
         _viewModel.ActionStatusText = $"No control at ({nodeX:F0}, {nodeY:F0})";
         return;
      }

      Debug.WriteLine("Screenshot hit test path: " +
         string.Join(" > ", path.Select(n => $"{n.Type}[{n.Bounds?.X:F0},{n.Bounds?.Y:F0} {n.Bounds?.Width:F0}x{n.Bounds?.Height:F0}]")));

      // Diagnostics: why did none of the children of the match win the hit test?
      foreach (var child in path[^1].Children ?? [])
         Debug.WriteLine($"   child {child.Type} " +
            $"bounds=({child.Bounds?.X:F0},{child.Bounds?.Y:F0} {child.Bounds?.Width:F0}x{child.Bounds?.Height:F0}) " +
            $"displayed={child.State?.Displayed} isVisible={child.IsVisible}");

      SelectNodeInTree(path);
   }

   /// <summary>Reads the pixel size of the last captured screenshot (PNG).</summary>
   private (int Width, int Height) GetScreenshotPixelSize()
   {
      var path = _viewModel.LastScreenshotPath;
      if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
         return (0, 0);

      try
      {
         Span<byte> header = stackalloc byte[24];
         using var stream = System.IO.File.OpenRead(path);
         if (stream.Read(header) < header.Length)
            return (0, 0);

         // PNG signature + IHDR: width/height are big-endian at offsets 16 and 20.
         if (header[0] != 0x89 || header[1] != 'P' || header[2] != 'N' || header[3] != 'G')
            return (0, 0);

         int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
         int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
         return (width, height);
      }
      catch (Exception ex)
      {
         Debug.WriteLine($"Reading screenshot size failed: {ex.Message}");
         return (0, 0);
      }
   }

   /// <summary>
   /// Vertical calibration between screenshot pixels and node coordinates.
   /// Measurements showed both spaces line up 1:1 apart from a few pixels, so
   /// no system-bar inset is removed. Increase this value when a control is
   /// only hit by clicking below it, decrease it for the opposite case.
   /// </summary>
   private double ScreenshotVerticalOffset = -20;

   /// <summary>
   /// Returns the path from a root node down to the smallest (and, on a tie, the
   /// deepest) visible node whose bounds contain the given point, or <c>null</c>
   /// when none matches.
   /// </summary>
   private static List<TreeNode>? FindNodePath(IEnumerable<TreeNode> roots, double x, double y)
   {
      List<TreeNode>? best = null;
      double bestArea = double.MaxValue;

      // Uses the same rectangles as the selection highlight (WindowBounds ?? Bounds).
      void Walk(TreeNode node, List<TreeNode> current)
      {
         current.Add(node);

         if ((node.WindowBounds ?? node.Bounds) is { } b
             && (node.State?.Displayed ?? node.IsVisible)
             && b.Width > 0 && b.Height > 0
             && x >= b.X && x <= b.X + b.Width
             && y >= b.Y && y <= b.Y + b.Height)
         {
            var area = b.Width * b.Height;

            // The innermost control is the one with the smallest hit area;
            // when two nodes share the same area (e.g. a layout wrapping a
            // single child) the deeper one is the more specific match.
            if (best is null
                || area < bestArea
                || (area == bestArea && current.Count > best.Count))
            {
               best = [.. current];
               bestArea = area;
            }
         }

         if (node.Children is not null)
            foreach (var child in node.Children)
               Walk(child, current);

         current.RemoveAt(current.Count - 1);
      }

      foreach (var root in roots)
         Walk(root, []);

      return best;
   }

   /// <summary>Expands the ancestors of the target node and selects it in the tree.</summary>
   private void SelectNodeInTree(List<TreeNode> path)
   {
      TreeNodeItem? target = null;

      foreach (var node in path)
      {
         var item = _viewModel.TreeItems.FirstOrDefault(i => ReferenceEquals(i.Node, node));

         if (item is null)
         {
            // The parent reported itself as expanded but its rows are missing;
            // force a collapse/expand cycle so the children get materialized.
            if (target is { HasChildren: true })
            {
               _viewModel.CollapseNode(target);
               _viewModel.ExpandNode(target);
               item = _viewModel.TreeItems.FirstOrDefault(i => ReferenceEquals(i.Node, node));
            }

            if (item is null) break;
         }

         target = item;
         if (item.HasChildren && !item.IsExpanded)
            _viewModel.ExpandNode(item);
      }

      if (target is null) return;

      TreeView.SelectedItem = target;
      _viewModel.SelectedTreeNode = target;
      _viewModel.ActionStatusText = $"Selected {target.DisplayType} {target.DisplayLabel}".TrimEnd();

      Dispatcher.Dispatch(() =>
         TreeView.ScrollTo(target, position: ScrollToPosition.Center, animate: false));
   }

   // ── Agent Selection ─────────────────────────────────────────────────────────

   private void OnAgentSelectionChanged(object? sender, EventArgs e)
   {
      // Sync the ViewModel's SelectedAgent with the Picker
      _viewModel.SelectedAgent = AgentsPicker.SelectedItem as DevFlowAgent;
   }

   // ── Tree expand / collapse / selection ──────────────────────────────────────

   private void OnScreenshotHostSizeChanged(object? sender, EventArgs e) => UpdateSelectionHighlight();

   /// <summary>Absolute rectangle of a node, resolved with the same rules as the screenshot hit test.</summary>
   private static Rect? ResolveAbsoluteRect(IEnumerable<TreeNode> roots, TreeNode target)
   {
      Rect? result = null;

      bool Walk(TreeNode node, double parentX, double parentY, Rect? parentRect)
      {
         double originX = parentX;
         double originY = parentY;
         Rect? rect = parentRect;

         if (node.Bounds is { } b)
         {
            if (parentRect is { } pr)
            {
               bool absoluteFits = Fits(pr, b.X, b.Y, b.Width, b.Height);
               bool relativeFits = Fits(pr, parentX + b.X, parentY + b.Y, b.Width, b.Height);
               if (absoluteFits || !relativeFits)
               {
                  originX = 0;
                  originY = 0;
               }
            }
            else
            {
               originX = 0;
               originY = 0;
            }

            rect = new Rect(originX + b.X, originY + b.Y, b.Width, b.Height);
            if (ReferenceEquals(node, target))
            {
               result = rect;
               return true;
            }
         }
         else if (ReferenceEquals(node, target))
            return true;

         if (node.Children is null) return false;
         var ox = node.Bounds is null ? parentX : rect!.Value.X;
         var oy = node.Bounds is null ? parentY : rect!.Value.Y;
         foreach (var child in node.Children)
            if (Walk(child, ox, oy, rect)) return true;
         return false;
      }

      static bool Fits(Rect parent, double x, double y, double w, double h)
      {
         const double tolerance = 1.0;
         return x >= parent.Left - tolerance && y >= parent.Top - tolerance
             && x + w <= parent.Right + tolerance && y + h <= parent.Bottom + tolerance;
      }

      foreach (var root in roots)
         if (Walk(root, 0, 0, null)) break;

      return result;
   }

   /// <summary>
   /// Mapping between node coordinates (WindowBounds ?? Bounds) and the screenshot host:
   /// host = offset + node * scale.
   /// </summary>
   private (double Scale, double OffsetX, double OffsetY)? GetScreenshotMapping()
   {
      if (_viewModel.TreeItems.Count == 0
         || ScreenshotHost.Width <= 0 || ScreenshotHost.Height <= 0)
         return null;

      double extW = 0, extH = 0;
      static void GetExtent(TreeNode n, ref double w, ref double h)
      {
         if ((n.WindowBounds ?? n.Bounds) is { } nb)
         {
            w = Math.Max(w, nb.X + nb.Width);
            h = Math.Max(h, nb.Y + nb.Height);
         }
         if (n.Children is null) return;
         foreach (var c in n.Children) GetExtent(c, ref w, ref h);
      }
      GetExtent(_viewModel.TreeItems[0].Node, ref extW, ref extH);
      if (extW <= 0 || extH <= 0) return null;

      var scale = Math.Min(ScreenshotHost.Width / extW, ScreenshotHost.Height / extH);
      var offsetX = (ScreenshotHost.Width - extW * scale) / 2;
      var offsetY = (ScreenshotHost.Height - extH * scale) / 2;
      return (scale, offsetX, offsetY);
   }

   private void UpdateSelectionHighlight()
   {
      var hide = true;
      try
      {
         if (_viewModel.SelectedTreeNode?.Node is not { } node
            || (node.WindowBounds ?? node.Bounds) is not { } b
            || b.Width <= 0 || b.Height <= 0
            || !_viewModel.ScreenshotImageVisible
            || _viewModel.TreeItems.Count == 0
            || ScreenshotHost.Width <= 0 || ScreenshotHost.Height <= 0)
            return;

         if (GetScreenshotMapping() is not var (scale, offsetX, offsetY)) return;

         SelectionHighlight.WidthRequest = Math.Max(2, b.Width * scale);
         SelectionHighlight.HeightRequest = Math.Max(2, b.Height * scale);
         SelectionHighlight.Margin = new Thickness(offsetX + b.X * scale, offsetY + b.Y * scale, 0, 0);
         hide = false;
      }
      finally
      {
         SelectionHighlight.IsVisible = !hide;
      }
   }

   private void OnTreeNodeSelected(object? sender, SelectionChangedEventArgs e)
       => _viewModel.SelectedTreeNode = e.CurrentSelection.FirstOrDefault() as TreeNodeItem;

   private void OnExpandToggled(object? sender, TappedEventArgs e)
   {
      if (e.Parameter is not TreeNodeItem item || !item.HasChildren) return;

      if (item.IsExpanded) _viewModel.CollapseNode(item);
      else _viewModel.ExpandNode(item);

      // Inserting/removing rows makes the CollectionView reset its scroll offset to
      // the top. Re-anchor on the node that was toggled once layout has settled.
      Dispatcher.Dispatch(() =>
         TreeView.ScrollTo(item, position: ScrollToPosition.Start, animate: false));
   }

   private async void OnTreeNodeDoubleTapped(object? sender, TappedEventArgs e)
   {
      if (e.Parameter is not TreeNodeItem item) return;

      var automationId = item.Node.AutomationId;
      if (string.IsNullOrWhiteSpace(automationId))
      {
         _viewModel.ActionStatusText = "No AutomationId to copy";
         return;
      }

      try
      {
         await Clipboard.Default.SetTextAsync(automationId);
         _viewModel.ActionStatusText = $"Copied AutomationId: {automationId}";
      }
      catch (Exception ex)
      {
         _viewModel.ActionStatusText = $"Copy failed: {ex.Message}";
      }
   }

   // ── Script Editor ───────────────────────────────────────────────────────────

   private async void OnScriptMenuClicked(object? sender, EventArgs e)
   {
      var choice = await DisplayActionSheet("Script", "Cancel", null, "Load script…", "Save script…");

      switch (choice)
      {
         case "Load script…":
            OnScriptLoadClicked(sender, e);
            break;

         case "Save script…":
            OnScriptSaveClicked(sender, e);
            break;
      }
   }

   private async void OnScriptLoadClicked(object? sender, EventArgs e)
   {
      try
      {
         var result = await FilePicker.PickAsync(new PickOptions
         {
            PickerTitle = "Select a DevFlow script file",
            FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".txt", ".devflow", ".df" } },
                    { DevicePlatform.macOS, new[] { "txt", "devflow", "df" } },
                    { DevicePlatform.iOS, new[] { "public.text" } },
                    { DevicePlatform.Android, new[] { "text/plain" } }
                })
         });

         if (result != null)
         {
             await ScriptEditor.LoadFileAsync(result.FullPath);
            _viewModel.UpdateScriptStatus($"Loaded: {result.FileName}");

            _viewModel.Config.LastScript = result.FullPath;
            _viewModel.Save();
         }
      }
      catch (Exception ex)
      {
         _viewModel.UpdateScriptStatus($"Load failed: {ex.Message}");
      }
   }

   private async void OnScriptSaveClicked(object? sender, EventArgs e)
   {
      try
      {
         var scriptContent = ScriptEditor.Text ?? string.Empty;
         if (string.IsNullOrWhiteSpace(scriptContent))
         {
            _viewModel.UpdateScriptStatus("Nothing to save.");
            return;
         }

         var defaultFileName = $"script_{DateTime.Now:yyyyMMdd_HHmmss}.df";

         // Use CommunityToolkit.Maui FileSaver
         using var stream = new MemoryStream(Encoding.UTF8.GetBytes(scriptContent));
         var fileSaverResult = await FileSaver.Default.SaveAsync(defaultFileName, stream, CancellationToken.None);

         if (fileSaverResult.IsSuccessful)
         {
            _viewModel.UpdateScriptStatus($"Saved to: {Path.GetFileName(fileSaverResult.FilePath)}");

            _viewModel.Config.LastScript = fileSaverResult.FilePath;
            _viewModel.Save();

            await DisplayAlertAsync("Script Saved",
                $"Script saved successfully to:\n{fileSaverResult.FilePath}",
                "OK");
         }
         else
         {
            _viewModel.UpdateScriptStatus($"Save cancelled or failed: {fileSaverResult.Exception?.Message}");
         }
      }
      catch (Exception ex)
      {
         _viewModel.UpdateScriptStatus($"Save failed: {ex.Message}");
         await DisplayAlertAsync("Save Error", $"Failed to save script: {ex.Message}", "OK");
      }
   }

   private void OnScriptClearClicked(object? sender, EventArgs e)
   {
      ScriptEditor.Text = string.Empty;
      ScriptEditorView.Text = string.Empty;

      _viewModel.ClearScript();
   }

   private void OnShowSyntaxHelperClicked(object? sender, EventArgs e)
   {
      var popup = new SyntaxHelpPopup();
      this.ShowPopup(popup);
   }


   private async void OnScriptRunClicked(object? sender, EventArgs e)
   {
      // retrive scropt from current visible editor
      var script = (ScriptEditor.IsVisible ? ScriptEditor.Text : ScriptEditorView.Text) ?? string.Empty;

      await RunScriptAsync(script);
   }

   /// <summary>
   /// Runs the supplied script text against the currently selected agent,
   /// updating status, output and screenshot UI while it executes.
   /// </summary>
   /// <param name="script">The script text to execute.</param>
   private async Task RunScriptAsync(string script)
   {
#if ANDROID
        _viewModel.UpdateScriptStatus("Script execution requires running UITestForge on Windows.");
        return;
#else
      if (_viewModel.SelectedAgent is null)
      {
         _viewModel.UpdateScriptStatus("Select an agent first.");
         return;
      }

      ScriptRunBtn.IsEnabled = false;
      ScriptClearBtn.IsEnabled = false;
      _viewModel.SetBusy(true, "Running script…");

      try
      {
         var (stepCount, error) = await ScriptEditorHelper.RunScriptAsync(
             script,
             _viewModel.SelectedAgent,
             onStepStatus: (n, cmd) => _viewModel.UpdateScriptStatus($"Step {n}: {cmd}…"),
             onOutputUpdate: (text) => _viewModel.UpdateScriptOutput(text),
             onScreenshotCaptured: path =>
             {
                ScreenshotImage.Source = ImageSource.FromFile(path);
                ScreenshotImage.IsVisible = true;
                ScreenshotRefreshBtn.IsVisible = true;
                _viewModel.LastScreenshotPath = path;
             },
             onGetCurrentPage: async () => await _viewModel.RefreshAndGetPageNameAsync(),
             scriptFolder: _viewModel.Config.ScriptFolder,
             currentPageName: _viewModel.PageName);

         _viewModel.UpdateScriptStatus(error is not null
             ? $"Script error: {error.Message}"
             : stepCount == 0 ? "No commands found." : $"Done — {stepCount} step(s) executed.");
      }
      finally
      {
         _viewModel.ActionStatusText = "";

         _viewModel.SetBusy(false);
         ScriptRunBtn.IsEnabled = true;
         ScriptClearBtn.IsEnabled = true;
      }
#endif
   }

   // Width threshold (device-independent units) below which the lightweight
   // EditorView replaces the full-featured ScriptPad.

   private void OnEditorBorderSizeChanged(object? sender, EventArgs e)
   {
      // NOTE: do NOT measure borderEditor here. That border is content-sized, so
      // ScriptPad and the editor header buttons impose a ~500px floor on its width
      // and it never reports a "narrow" value. Use the page width instead.
      if (scriptEditor.Width <= 0)
         return;

      bool isNarrow = scriptEditor.Width < 710;

      Debug.WriteLine($"OnEditorBorderSizeChanged {scriptEditor.Width}");

      // Only sync content when the mode actually switches; otherwise a layout
      // change would overwrite the visible editor with stale text.
      if (_editorModeInitialized && _lastEditorNarrow == isNarrow)
         return;
      _editorModeInitialized = true;
      _lastEditorNarrow = isNarrow;

      ScriptEditor.IsVisible = isNarrow;
      ScriptEditorView.IsVisible = !isNarrow;

      if (!isNarrow)
      {
         ScriptEditorView.RootPath = _viewModel.Config.ScriptFolder;
         ScriptEditorView.Highlighter = new UITestForgeScriptHighlighter();
         ScriptEditorView.Text = ScriptEditor.Text ?? string.Empty;
         ScriptEditorView.FileName = _viewModel.Config.LastScript;
         ScriptEditorView.CaretPosition = ScriptEditor.GetCaret();
         ScriptEditorView.IsModified = ScriptEditor.IsModified;
      }
      else
      {
         ScriptEditor.Highlighter = new UITestForgeScriptHighlighter();
         ScriptEditor.Text = ScriptEditorView.Text ?? string.Empty;
         ScriptEditor.FileName = _viewModel.Config.LastScript;
         ScriptEditor.SetCaret(ScriptEditorView.CaretPosition);
         ScriptEditor.IsModified = ScriptEditorView.IsModified;
      }
   }

   private void OnScriptOutputBorderSizeChanged(object? sender, EventArgs e)
   {
      // ScrollView never stretches its content, so give the Editor an explicit
      // minimum width matching the visible area. This makes it fill the border
      // horizontally for short lines, while still allowing AutoSize/ScrollView
      // to grow and scroll for lines longer than the available width.
      if (sender is Border border && border.Width > 0)
      {
         ScriptOutputLabel.MinimumWidthRequest = Math.Max(0, border.Width - 12);
      }
   }

   private async void OnCopyScreenshotClicked(object? sender, EventArgs e)
   {
      try
      {
         if (string.IsNullOrEmpty(_viewModel.LastScreenshotPath) || !File.Exists(_viewModel.LastScreenshotPath))
         {
            await DisplayAlertAsync("Copy Failed", "No screenshot available to copy.", "OK");
            return;
         }

         await Clipboard.Default.SetTextAsync(_viewModel.LastScreenshotPath);
         _viewModel.ActionStatusText = $"Screenshot path copied to clipboard: {System.IO.Path.GetFileName(_viewModel.LastScreenshotPath)}";
      }
      catch (Exception ex)
      {
         await DisplayAlertAsync("Copy Failed", $"Failed to copy screenshot: {ex.Message}", "OK");
      }
   }

   private async Task CreateAgentPresentationAsync()
   {
      if (_viewModel.SelectedAgent == null)
      {
         _viewModel.ActionStatusText = "No agent selected";
         return;
      }

      try
      {
         _viewModel.ActionStatusText = "Creating PowerPoint presentation...";

         var fileName = $"Agent_{_viewModel.SelectedAgent.AppName}_{DateTime.Now:yyyyMMdd_HHmmss}.pptx";
         var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

         //PptxReportHelper.CreateAgentTitlePage(
         //    filePath,
         //    _viewModel.SelectedAgent.AppName,
         //    _viewModel.SelectedAgent.Platform,
         //    _viewModel.SelectedAgent.Tfm);

         _viewModel.ActionStatusText = $"PowerPoint created: {fileName}";

         await Share.Default.RequestAsync(new ShareFileRequest
         {
            Title = "Share Agent Report",
            File = new ShareFile(filePath)
         });
      }
      catch (Exception ex)
      {
         _viewModel.ActionStatusText = $"Error creating PowerPoint: {ex.Message}";
      }
   }
}

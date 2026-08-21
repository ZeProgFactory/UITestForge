using UITestForge.Helpers;
using UITestForge.Views;
using UITestForge.ViewModels;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Storage;
using System.Text;

namespace UITestForge;

public partial class MainPage : ContentPage
{
   private readonly MainViewModel _viewModel;

   public MainPage()
   {
      InitializeComponent();

      _viewModel = new MainViewModel();
      BindingContext = _viewModel;

      // Wire up ViewModel events
      _viewModel.AgentsCollectionChanged += OnAgentsCollectionChanged;
      _viewModel.ScreenshotCaptured += OnScreenshotCaptured;
      _viewModel.AdbForwardButtonEnabledChanged += (enabled) => AdbForwardBtn.IsEnabled = enabled;

      // Wire up UI events
      this.Loaded += (s, e) =>
      {
         _viewModel.Load();

         ScriptEditor.Text = System.IO.File.ReadAllText(_viewModel.Config.LastScript);
      };
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

   // ── Agent Selection ─────────────────────────────────────────────────────────

   private void OnAgentSelectionChanged(object? sender, EventArgs e)
   {
      // Sync the ViewModel's SelectedAgent with the Picker
      _viewModel.SelectedAgent = AgentsPicker.SelectedItem as DevFlowAgent;
   }

   // ── Tree expand / collapse / selection ──────────────────────────────────────

   private void OnTreeNodeSelected(object? sender, SelectionChangedEventArgs e)
       => _viewModel.SelectedTreeNode = e.CurrentSelection.FirstOrDefault() as TreeNodeItem;

   private void OnExpandToggled(object? sender, TappedEventArgs e)
   {
      if (e.Parameter is not TreeNodeItem item || !item.HasChildren) return;
      if (item.IsExpanded) _viewModel.CollapseNode(item);
      else _viewModel.ExpandNode(item);
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
            using var stream = await result.OpenReadAsync();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            ScriptEditor.Text = content;
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
         await DisplayAlert("Save Error", $"Failed to save script: {ex.Message}", "OK");
      }
   }

   private void OnScriptClearClicked(object? sender, EventArgs e)
   {
      ScriptEditor.Text = string.Empty;
      _viewModel.ClearScript();
   }

   private void OnShowSyntaxHelperClicked(object? sender, EventArgs e)
   {
      var popup = new SyntaxHelpPopup();
      this.ShowPopup(popup);
   }

   private async void OnScriptRunClicked(object? sender, EventArgs e)
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
             ScriptEditor.Text ?? string.Empty,
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

   private async void OnCopyScreenshotClicked(object? sender, EventArgs e)
   {
      try
      {
         if (string.IsNullOrEmpty(_viewModel.LastScreenshotPath) || !File.Exists(_viewModel.LastScreenshotPath))
         {
            await DisplayAlert("Copy Failed", "No screenshot available to copy.", "OK");
            return;
         }

         await Clipboard.Default.SetTextAsync(_viewModel.LastScreenshotPath);
         _viewModel.ActionStatusText = $"Screenshot path copied to clipboard: {System.IO.Path.GetFileName(_viewModel.LastScreenshotPath)}";
      }
      catch (Exception ex)
      {
         await DisplayAlert("Copy Failed", $"Failed to copy screenshot: {ex.Message}", "OK");
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

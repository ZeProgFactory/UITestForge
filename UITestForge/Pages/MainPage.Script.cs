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

public partial class MainPage
{
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

}

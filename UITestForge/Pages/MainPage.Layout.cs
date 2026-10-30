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

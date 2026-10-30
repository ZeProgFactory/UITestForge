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


}

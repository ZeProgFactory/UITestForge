using ZPF.Maui.Script;
using ZPF.Maui.Script.ScriptEditing;

namespace ScriptPad.Sample
{
   public partial class EditorPage : ContentPage
   {
      private ISyntaxHighlighter _DefaultHighlighter = new PlainTextHighlighter();

      public string RootPath { get => FileExplorer.RootPath; set => FileExplorer.RootPath = value; }

      /// <summary>Gives access to the file explorer shown on this page (context menu, events, ...).</summary>
      public FileExplorerTreeView Explorer => FileExplorer;

      /// <summary>Gives access to the editor shown on this page (context menu, caret, find, ...).</summary>
      public ZPF.Maui.Script.ScriptPad Editor => EditorCtl;

      public string FileName 
      { 
         get => EditorCtl.FileName; 
         set => EditorCtl.FileName = value; }

      public string FileExtensions { get => FileExplorer.FileExtensions; set => FileExplorer.FileExtensions = value; } 
      public ISyntaxHighlighter Highlighter 
      { 
         get => EditorCtl.Highlighter; 
         set 
         {
            EditorCtl.Highlighter = value;
            _DefaultHighlighter = value;
         } 
      }
      public string Text { get => EditorCtl.Text; set => EditorCtl.Text = value; }
      public bool IsModified { get => EditorCtl.IsModified; set => EditorCtl.IsModified = value; }

      public TextPosition CaretPosition { get => EditorCtl.GetCaret(); set => EditorCtl.SetCaret(value); }


      // - - -  - - - 

      public EditorPage()
      {
         InitializeComponent();

         EditorCtl.Theme = ScriptPadTheme.Light();             // or Dark()
         EditorCtl.Highlighter = new MarkdownHighlighter();    // or PlainTextHighlighter / your own
         EditorCtl.Text =
#if DEBUG
@"#Hello, World!
Holla die Waldfee ...
";
#else
string.Empty;
#endif

         // Set default root path to user's documents folder
         // You can change this to any desired path
         // FileExplorer.RootPath = @"D:\GitWare\Nugets\ScriptPad";
         FileExplorer.RootPath = (System.IO.Directory.Exists(RootPath) ? RootPath : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

         // Example: Set specific file extensions (comma-separated)
         FileExplorer.FileExtensions = ".txt,.md,.cs,.df";

         // Example: Show all files
         //FileExplorer.FileExtensions = "*";

         // Enable subfolder display
         FileExplorer.ShowSubfolders = true;

         // Store original text for modification tracking
         EditorCtl.IsModified = false;

         // Subscribe to caret position changes
         EditorCtl.Editor.CaretMoved += OnCaretMoved;

         // Subscribe to text changes for modification tracking
         EditorCtl.TextChanged += OnTextChanged;

         // Keep the status label in sync whenever IsModified is changed (e.g. from outside)
         EditorCtl.IsModifiedChanged += OnTextChanged;

         // Initialize status
         UpdateCaretPosition();
         UpdateModifiedStatus();
      }

      private void OnCaretMoved(object? sender, EventArgs e)
      {
         UpdateCaretPosition();
      }

      private void OnTextChanged(object? sender, EventArgs e)
      {
         UpdateModifiedStatus();
      }

      private void UpdateCaretPosition()
      {
         var pos = EditorCtl.Editor.CaretPosition;
         // Display 1-based line and column numbers (user-friendly)
         CaretPositionLabel.Text = $"Ln {pos.Line + 1}, Col {pos.Column + 1}";
      }

      private void UpdateModifiedStatus()
      {
         ModifiedStatusLabel.Text = EditorCtl.IsModified ? "Modified" : "";
      }

      // Check if user wants to discard unsaved changes
      private async Task<bool> ConfirmDiscardChangesAsync()
      {
         if (!EditorCtl.IsModified)
            return true;

         return await DisplayAlertAsync(
            "Unsaved Changes",
            "You have unsaved changes. Do you want to discard them?",
            "Discard",
            "Cancel");
      }

      // Handle file selection from the TreeView
      private async void OnFileSelected(object? sender, FileSelectedEventArgs e)
      {
         // Check for unsaved changes
         if (!await ConfirmDiscardChangesAsync())
            return;

         try
         {
            // Set the text, then take it as the new unmodified baseline
            await EditorCtl.LoadFileAsync(e.FilePath);

            UpdateModifiedStatus(); // This will now show as not modified

            // Optional: Update the highlighter based on file extension
            var extension = Path.GetExtension(e.FilePath).ToLowerInvariant();
            switch (extension)
            {
               case ".md":
               case ".markdown":
                  EditorCtl.Highlighter = new MarkdownHighlighter();
                  break;

               default:
                  EditorCtl.Highlighter = _DefaultHighlighter;
                  break;
            }
         }
         catch (Exception ex)
         {
            await DisplayAlertAsync("Error", $"Could not load file: {ex.Message}", "OK");
         }
      }

      // Optional: Handle load event with custom logic
      // If not handled (Handled = false), the default file picker dialog will be used
      private async void OnLoadRequested(object sender, LoadRequestedEventArgs e)
      {
         // Always handle this event ourselves to check for unsaved changes first
         e.Handled = true;

         // Check for unsaved changes before loading
         if (!await ConfirmDiscardChangesAsync())
         {
            return; // User cancelled, do nothing
         }

         // User confirmed, proceed with file picker
         try
         {
            var result = await FilePicker.PickAsync(new PickOptions
            {
               PickerTitle = "Select a file to load"
            });

            if (result != null)
            {
               using var stream = await result.OpenReadAsync();
               using var reader = new StreamReader(stream);
               var content = await reader.ReadToEndAsync();

               // Set the text, then take it as the new unmodified baseline
               EditorCtl.Text = content;
               EditorCtl.IsModified = false;
               UpdateModifiedStatus(); // This will now show as not modified

               // Update the highlighter based on file extension
               var extension = Path.GetExtension(result.FileName).ToLowerInvariant();
               switch (extension)
               {
                  case ".md":
                  case ".markdown":
                     EditorCtl.Highlighter = new MarkdownHighlighter();
                     break;
                  default:
                     EditorCtl.Highlighter = new PlainTextHighlighter();
                     break;
               }
            }
         }
         catch (Exception ex)
         {
            await DisplayAlertAsync("Error", $"Could not load file: {ex.Message}", "OK");
         }
      }

      // Optional: Handle save event with custom logic
      // If not handled (Handled = false), the default save dialog will be used
      private void OnSaveRequested(object sender, SaveRequestedEventArgs e)
      {
         // Example: You can implement custom save logic here
         // string textToSave = e.Text;
         // e.Handled = true; // Set to true to prevent default dialog

         // Leave Handled as false to use the default dialog
      }

   }
}

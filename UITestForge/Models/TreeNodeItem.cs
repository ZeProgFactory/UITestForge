// ── Tree flat-list view model ─────────────────────────────────────────────

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UITestForge;

public class TreeNodeItem : INotifyPropertyChanged
{
   public required TreeNode Node { get; init; }
   public required int Depth { get; init; }

   public bool HasChildren => Node.Children?.Count > 0;

   private bool _isExpanded;
   public bool IsExpanded
   {
      get => _isExpanded;
      set { _isExpanded = value; OnPropertyChanged(); OnPropertyChanged(nameof(ExpandIcon)); }
   }

   // ── Tree row display ────────────────────────────────────────────────

   public Thickness IndentPadding => new(Depth * 14.0, 2, 4, 2);

   public string ExpandIcon => HasChildren ? (_isExpanded ? "▾" : "▸") : " ";

   public string TypeIcon => GetTypeIcon(Node.Type);

   private static string GetTypeIcon(string type)
   {
      // Try exact match first
      var icon = type switch
      {
         "Button" => "🔘",
         "Label" => "🏷",
         "Image" => "🖼",
         "Entry" => "✏",
         "Editor" => "📝",
         "ScrollView" => "📜",
         "ContentPage" => "📄",
         "Window" => "🪟",
         "Grid" => "⊞",
         "VerticalStackLayout" or "HorizontalStackLayout"
            or "StackLayout" => "⊟",
         "AppShell" or "Shell" => "🐚",
         "Frame" or "Border" => "▭",
         "ActivityIndicator" => "⏳",
         "Switch" => "↕",
         "CollectionView" or "ListView" => "📋",
         "ShellItem" or "ShellSection" or "ShellContent"
            or "FlyoutButton" => "🗂",
         "Picker" => "🎚",
         _ => null
      };

      if (icon != null) return icon;

      // Try to extract base type from custom controls (e.g., CustomPicker -> Picker)
      string[] knownTypes = [
         "Button", "Label", "Image", "Entry", "Editor", "ScrollView",
         "ContentPage", "Window", "Grid", "StackLayout", "Shell",
         "Frame", "Border", "ActivityIndicator", "Switch",
         "CollectionView", "ListView", "Picker"
      ];

      foreach (var knownType in knownTypes)
      {
         if (type.Contains(knownType, StringComparison.OrdinalIgnoreCase))
         {
            return GetTypeIcon(knownType);
         }
      }

      // Unknown type
      return "▫";
   }

   public string DisplayType => Node.Type;

   public string DisplayLabel =>
      Node.AutomationId is { Length: > 0 } aid ? $"#{aid}" :
      Node.Text is { Length: > 0 } txt ? $"\"{txt}\"" :
      string.Empty;

   public bool IsHiddenNode => !(Node.State?.Displayed ?? Node.IsVisible);

   // ── Detail panel ────────────────────────────────────────────────────

   public string FullTypeLabel => Node.FullType;
   public string IdLabel => $"id: {Node.Id}";
   public bool HasAutomationId => Node.AutomationId is { Length: > 0 };
   public string AutomationIdLabel => $"automationId: {Node.AutomationId}";
   public bool HasText => Node.Text is { Length: > 0 };
   public string TextLabel => $"text: \"{Node.Text}\"";

   public string BoundsLabel =>
      Node.Bounds is { } b
         ? $"bounds: ({b.X:F1}, {b.Y:F1})  {b.Width:F1} × {b.Height:F1}"
         : string.Empty;

   public string StateLabel
   {
      get
      {
         if (Node.State is not { } s) return string.Empty;
         var parts = new List<string>(4);
         if (s.Displayed) parts.Add("visible");
         if (s.Enabled) parts.Add("enabled");
         if (s.Focused) parts.Add("focused");
         if (s.Selected) parts.Add("selected");
         return parts.Count > 0
            ? string.Join("  ·  ", parts)
            : "hidden / disabled";
      }
   }

   public event PropertyChangedEventHandler? PropertyChanged;
   private void OnPropertyChanged([CallerMemberName] string? name = null)
      => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

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

}

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

}

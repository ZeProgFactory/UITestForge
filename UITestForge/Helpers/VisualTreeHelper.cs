using System.Collections.ObjectModel;

namespace UITestForge.Helpers;

internal static class VisualTreeHelper
{
   /// <summary>
   /// Adds <paramref name="node"/> and its children to <paramref name="items"/>.
   /// Children are expanded automatically up to <paramref name="expandDepth"/> levels.
   /// </summary>
   public static void FlattenTree(
      TreeNode node, int depth, int expandDepth,
      ObservableCollection<TreeNodeItem> items)
   {
      try
      {
         var item = new TreeNodeItem { Node = node, Depth = depth };
         if (depth < expandDepth && node.Children?.Count > 0)
            item.IsExpanded = true;
         items.Add(item);
         if (item.IsExpanded && node.Children is not null)
            foreach (var child in node.Children)
               FlattenTree(child, depth + 1, expandDepth, items);
      }
      catch (Exception ex)
      {
         System.Diagnostics.Debug.WriteLine($"Error flattening tree node {node?.Type ?? "null"}: {ex.Message}");
      }
   }

   /// <summary>Inserts the immediate children of <paramref name="item"/> into the flat list.</summary>
   public static void ExpandNode(TreeNodeItem item, ObservableCollection<TreeNodeItem> items)
   {
      item.IsExpanded = true;
      var idx = items.IndexOf(item);
      if (idx < 0 || item.Node.Children is null) return;
      int insertAt = idx + 1;
      foreach (var child in item.Node.Children)
      {
         try
         {
            var childItem = new TreeNodeItem { Node = child, Depth = item.Depth + 1 };
            items.Insert(insertAt++, childItem);
         }
         catch (Exception ex)
         {
            System.Diagnostics.Debug.WriteLine($"Error expanding node {child?.Type ?? "null"}: {ex.Message}");
         }
      }
   }

   /// <summary>Removes the entire subtree below <paramref name="item"/> from the flat list.</summary>
   public static void CollapseNode(TreeNodeItem item, ObservableCollection<TreeNodeItem> items)
   {
      item.IsExpanded = false;
      var idx = items.IndexOf(item);
      if (idx < 0) return;
      while (idx + 1 < items.Count && items[idx + 1].Depth > item.Depth)
         items.RemoveAt(idx + 1);
   }
}

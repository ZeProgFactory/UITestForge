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

}

using ShapeCrawler;

namespace UITestForge.Helpers;

/// <summary>
/// Generates a single-slide PowerPoint report with three columns:
/// Before screenshot | Execution logs | After screenshot.
/// The script text is placed in the slide's speaker notes.
/// </summary>
internal static class PptxReportHelper
{
   // ── Public entry points ───────────────────────────────────────────────────

   /// <summary>
   /// Creates a .pptx file at <paramref name="outputPath"/> containing one slide
   /// with three columns (before image, execution logs, after image) and the
   /// <paramref name="scriptText"/> in the speaker-notes section.
   /// </summary>
   public static void CreateReport(
         string outputPath,
         string title,
         string? appName = null,
         string? platform = null,
         string? version = null)
   {
      // Create a new presentation with a slide
      var pres = new Presentation(p => p.Slide());

      var shapes = pres.Slide(1).Shapes;

      // Get slide dimensions
      int slideWidth = (int)pres.SlideWidth;
      int slideHeight = (int)pres.SlideHeight;

      // Standard slide dimensions (in points): 720 x 540
      //const int slideWidth = 720;
      //const int slideHeight = 540;
      const int contentWidth = 500;
      const int boxPadding = 40;

      // Add background image - try multiple potential paths
      var possiblePaths = new[]
      {
         Path.Combine(AppContext.BaseDirectory, "Resources", "Images", "powerpoint.png"),
         Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Resources", "Images", "powerpoint.png"),
         Path.Combine(Environment.CurrentDirectory, "Resources", "Images", "powerpoint.png"),
         "Resources/Images/powerpoint.png"
      };

      string? backgroundImagePath = null;
      foreach (var path in possiblePaths)
      {
         var fullPath = Path.GetFullPath(path);
         if (File.Exists(fullPath))
         {
            backgroundImagePath = fullPath;
            break;
         }
      }

      if (backgroundImagePath != null)
      {
         // Load image bytes into memory stream to keep it available until Save
         var imageBytes = File.ReadAllBytes(backgroundImagePath);
         var imageStream = new MemoryStream(imageBytes);
         shapes.AddPicture(imageStream);
         var backgroundPicture = shapes.Last();
         backgroundPicture.X = 0;
         backgroundPicture.Y = 0;
         backgroundPicture.Width = slideWidth;
         backgroundPicture.Height = slideHeight;
      }



      // Calculate number of lines needed
      int lineCount = 1; // title
      if (!string.IsNullOrWhiteSpace(appName)) lineCount++;
      if (!string.IsNullOrWhiteSpace(platform)) lineCount++;
      if (!string.IsNullOrWhiteSpace(version)) lineCount++;
      lineCount++; // timestamp

      // Calculate content dimensions
      const int lineHeight = 40;
      const int lineSpacing = 10;
      int contentHeight = (lineCount * lineHeight) + ((lineCount - 1) * lineSpacing);
      int totalBoxHeight = contentHeight + (boxPadding * 2);

      // Center the box on the slide
      int boxX = (slideWidth - contentWidth - (boxPadding * 2)) / 2;
      int boxY = (slideHeight - totalBoxHeight) / 2;

      // Add rounded rectangle border with light blue background
      shapes.AddShape(
         x: boxX,
         y: boxY,
         width: contentWidth + (boxPadding * 2),
         height: totalBoxHeight,
         geometry: Geometry.RoundedRectangle);
      var borderShape = shapes.Last();
      borderShape.Fill.SetColor("D6E8F5"); // Light blue background
      borderShape.Outline.Weight = 2;
      borderShape.Outline.SetHexColor("4472C4"); // Blue border

      // Starting position for text (inside the box)
      int textX = boxX + boxPadding;
      int textY = boxY + boxPadding;

      // Add title (centered)
      shapes.AddShape(x: textX, y: textY, width: contentWidth, height: lineHeight);
      var titleShape = shapes.Last();
      titleShape.Fill.SetNoFill();
      titleShape.Outline.SetNoOutline();
      titleShape.TextBox.SetText(title);
      titleShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
      titleShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
      titleShape.TextBox.Paragraphs.First().SetFontColor(Colors.Navy.ToHex()); // Black text
      titleShape.TextBox.Paragraphs.First().SetFontSize(32);
      textY += lineHeight + lineSpacing;

      // Add AppName (centered)
      if (!string.IsNullOrWhiteSpace(appName))
      {
         shapes.AddShape(x: textX, y: textY, width: contentWidth, height: lineHeight);
         var appNameShape = shapes.Last();
         appNameShape.Fill.SetNoFill();
         appNameShape.Outline.SetNoOutline();
         appNameShape.TextBox.SetText($"{appName}");
         appNameShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
         appNameShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
         appNameShape.TextBox.Paragraphs.First().SetFontColor(Colors.Navy.ToHex()); // Black text
         appNameShape.TextBox.Paragraphs.First().SetFontSize(48);
         appNameShape.TextBox.Paragraphs.First().Portions.First().Font.IsBold = true; // Bold text
         textY += lineHeight + lineSpacing;
      }

      // Add Platform (centered)
      if (!string.IsNullOrWhiteSpace(platform))
      {
         shapes.AddShape(x: textX, y: textY, width: contentWidth, height: lineHeight);
         var platformShape = shapes.Last();
         platformShape.Fill.SetNoFill();
         platformShape.Outline.SetNoOutline();
         platformShape.TextBox.SetText($"{platform}");
         platformShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
         platformShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
         platformShape.TextBox.Paragraphs.First().SetFontColor(Colors.Navy.ToHex()); // Black text
         platformShape.TextBox.Paragraphs.First().SetFontSize(32);
         textY += lineHeight + lineSpacing;
      }

      // Add Version (centered)
      if (!string.IsNullOrWhiteSpace(version))
      {
         shapes.AddShape(x: textX, y: textY, width: contentWidth, height: lineHeight);
         var versionShape = shapes.Last();
         versionShape.Fill.SetNoFill();
         versionShape.Outline.SetNoOutline();
         versionShape.TextBox.SetText($"V {version}");
         versionShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
         versionShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
         versionShape.TextBox.Paragraphs.First().SetFontColor(Colors.Navy.ToHex()); // Black text
         versionShape.TextBox.Paragraphs.First().SetFontSize(28);
         textY += lineHeight + lineSpacing;
      }

      // Add Timestamp (centered)
      shapes.AddShape(x: textX, y: textY, width: contentWidth, height: lineHeight);
      var timestampShape = shapes.Last();
      timestampShape.Fill.SetNoFill();
      timestampShape.Outline.SetNoOutline();
      timestampShape.TextBox.SetText(DateTime.Now.ToString("dd.MM.yyyy  HH:mm:ss"));
      timestampShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
      timestampShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
      timestampShape.TextBox.Paragraphs.First().SetFontColor(Colors.Navy.ToHex());
      timestampShape.TextBox.Paragraphs.First().SetFontSize(20);

      pres.Save(outputPath);
   }




}

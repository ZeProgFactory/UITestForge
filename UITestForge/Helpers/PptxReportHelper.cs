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

      // Standard slide dimensions (in points): 720 x 540
      const int slideWidth = 720;
      const int slideHeight = 540;
      const int contentWidth = 500;
      const int boxPadding = 40;

      // Calculate number of lines needed
      int lineCount = 1; // title
      if (!string.IsNullOrWhiteSpace(appName)) lineCount++;
      if (!string.IsNullOrWhiteSpace(platform)) lineCount++;
      if (!string.IsNullOrWhiteSpace(version)) lineCount++;

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
      titleShape.TextBox.Paragraphs.First().SetFontColor("000000"); // Black text
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
         appNameShape.TextBox.Paragraphs.First().SetFontColor("000000"); // Black text
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
         platformShape.TextBox.Paragraphs.First().SetFontColor("000000"); // Black text
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
         versionShape.TextBox.Paragraphs.First().SetFontColor("000000"); // Black text
      }

      pres.Save(outputPath);
   }




}

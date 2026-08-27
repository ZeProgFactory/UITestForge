using ShapeCrawler;
using System.Drawing;

namespace UITestForge.Helpers;

/// <summary>
/// Generates a single-slide PowerPoint report with three columns:
/// Before screenshot | Execution logs | After screenshot.
/// The script text is placed in the slide's speaker notes.
/// </summary>
internal static class PptxReportHelper
{
   public static string CurrentPPTXFile = "";

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

      AddBackgroundImage(shapes, slideWidth, slideHeight);

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
      CurrentPPTXFile = outputPath;
   }

   private static void AddBackgroundImage(IUserSlideShapeCollection shapes, decimal slideWidth, decimal slideHeight)
   {
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
   }

   /// <summary>
   /// Adds a report page to the current PPTX file with 3 columns:
   /// - Left: Before image
   /// - Center: Script execution log
   /// - Right: After image
   /// The script text is placed in the slide's speaker notes.
   /// Note: This creates a new single-slide PPTX file at the moment since ShapeCrawler 
   /// doesn't easily support adding slides to existing presentations.
   /// </summary>
   /// <param name="beforeImagePath">Path to the "before" screenshot image</param>
   /// <param name="afterImagePath">Path to the "after" screenshot image</param>
   /// <param name="executionLog">Multi-line text containing execution logs</param>
   /// <param name="scriptText">Script text to be added to speaker notes</param>
   /// <param name="slideTitle">Optional title for the slide (default: "Test Report")</param>
   public static void AddReportPage(
       string? beforeImagePath,
       string? afterImagePath,
       string executionLog,
       string scriptText,
       string slideTitle = "Test Report")
   {
      if (string.IsNullOrEmpty(CurrentPPTXFile) || !File.Exists(CurrentPPTXFile))
      {
         throw new InvalidOperationException("No current PPTX file. Create a report first using CreateReport().");
      }


      // Load the existing presentation
      var pres = new Presentation(CurrentPPTXFile);
      var initialCount = pres.Slides.Count;

      // Get the first slide layout from the first slide master
      int slideLayout = pres.MasterSlides[0].LayoutSlides[0].Number;

      // Add a new slide
      pres.Slides.Add(slideLayout);
      var newSlide = pres.Slides[initialCount]; // Get the newly added slide
      var shapes = newSlide.Shapes;

      // Get slide dimensions
      int slideWidth = (int)pres.SlideWidth;
      int slideHeight = (int)pres.SlideHeight;

      // Layout constants
      const int margin = 20;
      const int titleHeight = 60;
      const int columnSpacing = 15;

      // Calculate column widths (image columns 10% smaller, log column 20% larger)
      int availableWidth = slideWidth - (2 * margin) - (2 * columnSpacing);
      int imageColumnWidth = (int)(availableWidth * 0.2667); // 26.67% (10% smaller than 33.33%)
      int logColumnWidth = (int)(availableWidth * 0.4667);   // 46.67% (20% larger than 33.33%)

      // Content area (below title)
      int contentY = margin + titleHeight + 10;
      int contentHeight = slideHeight - contentY - margin;

      AddBackgroundImage(shapes, slideWidth, slideHeight);

      // Add slide title
      shapes.AddShape(
          x: margin,
          y: margin,
          width: slideWidth - (2 * margin),
          height: titleHeight,
          geometry: Geometry.Rectangle);
      var titleShape = shapes.Last();
      titleShape.Fill.SetColor("4472C4"); // Blue background
      titleShape.Outline.SetNoOutline();
      titleShape.TextBox.SetText(slideTitle);
      titleShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
      titleShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
      titleShape.TextBox.Paragraphs.First().SetFontColor("FFFFFF"); // White text
      titleShape.TextBox.Paragraphs.First().SetFontSize(28);
      titleShape.TextBox.Paragraphs.First().Portions.First().Font.IsBold = true;

      // Column 1: Before Image (Left) - 10% smaller
      int col1X = margin;
      AddColumnWithImage(shapes, col1X, contentY, imageColumnWidth, contentHeight, "Before", beforeImagePath);

      // Column 2: Execution Log (Center) - 20% larger
      int col2X = col1X + imageColumnWidth + columnSpacing;
      AddColumnWithText(shapes, col2X, contentY, logColumnWidth, contentHeight, "Execution Log", executionLog);

      // Column 3: After Image (Right) - 10% smaller
      int col3X = col2X + logColumnWidth + columnSpacing;
      AddColumnWithImage(shapes, col3X, contentY, imageColumnWidth, contentHeight, "After", afterImagePath);

      // Create notes if they don't exist
      newSlide.AddNotes(scriptText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None));

      // Save the presentation back to the same file
      pres.Save(CurrentPPTXFile);
   }

   /// <summary>
   /// Helper method to add a column with a header and an image.
   /// </summary>
   private static void AddColumnWithImage(
       IUserSlideShapeCollection shapes,
       int x,
       int y,
       int width,
       int height,
       string headerText,
       string? imagePath)
   {
      const int headerHeight = 40;
      const int spacing = 5;

      // Add column border/background
      shapes.AddShape(
          x: x,
          y: y,
          width: width,
          height: height,
          geometry: Geometry.Rectangle);
      var borderShape = shapes.Last();
      borderShape.Fill.SetColor("F2F2F2"); // Light gray background
      borderShape.Outline.Weight = 1;
      borderShape.Outline.SetHexColor("CCCCCC"); // Gray border

      // Add header
      shapes.AddShape(
          x: x,
          y: y,
          width: width,
          height: headerHeight,
          geometry: Geometry.Rectangle);
      var headerShape = shapes.Last();
      headerShape.Fill.SetColor("D6E8F5"); // Light blue background
      headerShape.Outline.SetNoOutline();
      headerShape.TextBox.SetText(headerText);
      headerShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
      headerShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
      headerShape.TextBox.Paragraphs.First().SetFontColor(Colors.Navy.ToHex());
      headerShape.TextBox.Paragraphs.First().SetFontSize(18);
      headerShape.TextBox.Paragraphs.First().Portions.First().Font.IsBold = true;

      // Add image if provided
      if (!string.IsNullOrWhiteSpace(imagePath) && File.Exists(imagePath))
      {
         int imageY = y + headerHeight + spacing;
         int availableHeight = height - headerHeight - (2 * spacing);
         int availableWidth = width - (2 * spacing);

         try
         {
            // Load image bytes into memory stream
            var imageBytes = File.ReadAllBytes(imagePath);
            var imageStream = new MemoryStream(imageBytes);

            // Get actual image dimensions
            int actualImageWidth, actualImageHeight;
            using(var img = System.Drawing.Image.FromStream(new MemoryStream(imageBytes)))
            {
               actualImageWidth = img.Width;
               actualImageHeight = img.Height;
            }

            // Calculate aspect ratio
            double imageAspectRatio = (double)actualImageWidth / actualImageHeight;
            double availableAspectRatio = (double)availableWidth / availableHeight;

            int scaledWidth, scaledHeight;

            // Scale to fit while maintaining aspect ratio
            if (imageAspectRatio > availableAspectRatio)
            {
               // Image is wider - fit to width
               scaledWidth = availableWidth;
               scaledHeight = (int)(availableWidth / imageAspectRatio);
            }
            else
            {
               // Image is taller - fit to height
               scaledHeight = availableHeight;
               scaledWidth = (int)(availableHeight * imageAspectRatio);
            }

            // Center the image within the available space
            int centeredX = x + spacing + (availableWidth - scaledWidth) / 2;
            int centeredY = imageY + (availableHeight - scaledHeight) / 2;

            shapes.AddPicture(imageStream);
            var picture = shapes.Last();

            // Set image position and dimensions
            picture.X = centeredX;
            picture.Y = centeredY;
            picture.Width = scaledWidth;
            picture.Height = scaledHeight;
         }
         catch
         {
            // If image loading fails, add placeholder text
            shapes.AddShape(
                x: x + spacing,
                y: imageY,
                width: availableWidth,
                height: availableHeight,
                geometry: Geometry.Rectangle);
            var placeholderShape = shapes.Last();
            placeholderShape.Fill.SetNoFill();
            placeholderShape.Outline.SetNoOutline();
            placeholderShape.TextBox.SetText("Image not available");
            placeholderShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
            placeholderShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
            placeholderShape.TextBox.Paragraphs.First().SetFontColor("999999");
            placeholderShape.TextBox.Paragraphs.First().SetFontSize(14);
         }
      }
      else
      {
         // Add "No image" placeholder
         int imageY = y + headerHeight + spacing;
         int imageHeight = height - headerHeight - (2 * spacing);
         int imageWidth = width - (2 * spacing);

         shapes.AddShape(
             x: x + spacing,
             y: imageY,
             width: imageWidth,
             height: imageHeight,
             geometry: Geometry.Rectangle);
         var placeholderShape = shapes.Last();
         placeholderShape.Fill.SetNoFill();
         placeholderShape.Outline.SetNoOutline();
         placeholderShape.TextBox.SetText("No image");
         placeholderShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
         placeholderShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
         placeholderShape.TextBox.Paragraphs.First().SetFontColor("CCCCCC");
         placeholderShape.TextBox.Paragraphs.First().SetFontSize(14);
      }
   }

   /// <summary>
   /// Helper method to add a column with a header and text content.
   /// </summary>
   private static void AddColumnWithText(
       IUserSlideShapeCollection shapes,
       int x,
       int y,
       int width,
       int height,
       string headerText,
       string contentText)
   {
      const int headerHeight = 40;
      const int spacing = 5;

      // Add column border/background
      shapes.AddShape(
          x: x,
          y: y,
          width: width,
          height: height,
          geometry: Geometry.Rectangle);
      var borderShape = shapes.Last();
      borderShape.Fill.SetColor("F2F2F2"); // Light gray background
      borderShape.Outline.Weight = 1;
      borderShape.Outline.SetHexColor("CCCCCC"); // Gray border

      // Add header
      shapes.AddShape(
          x: x,
          y: y,
          width: width,
          height: headerHeight,
          geometry: Geometry.Rectangle);
      var headerShape = shapes.Last();
      headerShape.Fill.SetColor("D6E8F5"); // Light blue background
      headerShape.Outline.SetNoOutline();
      headerShape.TextBox.SetText(headerText);
      headerShape.TextBox.VerticalAlignment = TextVerticalAlignment.Middle;
      headerShape.TextBox.Paragraphs.First().HorizontalAlignment = TextHorizontalAlignment.Center;
      headerShape.TextBox.Paragraphs.First().SetFontColor(Colors.Navy.ToHex());
      headerShape.TextBox.Paragraphs.First().SetFontSize(18);
      headerShape.TextBox.Paragraphs.First().Portions.First().Font.IsBold = true;

      // Add text content
      int contentY = y + headerHeight + spacing;
      int contentHeight = height - headerHeight - (2 * spacing);
      int contentWidth = width - (2 * spacing);

      shapes.AddShape(
          x: x + spacing,
          y: contentY,
          width: contentWidth,
          height: contentHeight,
          geometry: Geometry.Rectangle);
      var textShape = shapes.Last();
      textShape.Fill.SetColor("FFFFFF"); // White background
      textShape.Outline.SetNoOutline();
      textShape.TextBox.SetText(contentText ?? "No log available");
      textShape.TextBox.VerticalAlignment = TextVerticalAlignment.Top;

      foreach (var paragraph in textShape.TextBox.Paragraphs)
      {
         paragraph.HorizontalAlignment = TextHorizontalAlignment.Left;
         paragraph.SetFontColor("000000");
         paragraph.SetFontSize(10);

         foreach(var portion in paragraph.Portions)
         {
            // Set font family to monospace for better log readability
            portion.Font.LatinName = "Consolas";

            if(portion.Text.Trim().StartsWith("✗", StringComparison.OrdinalIgnoreCase) )
            {
               portion.Font.IsBold = true;
               portion.TextHighlightColor = new ShapeCrawler.Color("FF0000"); // Red for errors
            }
         }

      }
   }


}

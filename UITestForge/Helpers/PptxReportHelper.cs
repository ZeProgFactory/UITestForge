using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace UITestForge.Helpers;

/// <summary>
/// Generates a single-slide PowerPoint report with three columns:
/// Before screenshot | Execution logs | After screenshot.
/// The script text is placed in the slide's speaker notes.
/// </summary>
internal static class PptxReportHelper
{
   // ── Slide geometry (EMU) ──────────────────────────────────────────────────
   private const long SW = 9_144_000;   // slide width  (13.33 in)
   private const long SH = 5_143_500;   // slide height (7.5 in)
   private const long Pad = 200_000;
   private const long TitleH = 450_000;
   private const long LabelH = 280_000;
   private const long ContentY = TitleH + LabelH + Pad / 2;
   private const long ContentH = SH - ContentY - Pad;
   private const long ColW = (SW - 4 * Pad) / 3;   // ~2 781 333 EMU
   private const long Col0X = Pad;
   private const long Col1X = Col0X + ColW + Pad;
   private const long Col2X = Col1X + ColW + Pad;

   // ── Public entry points ───────────────────────────────────────────────────

   /// <summary>
   /// Creates a .pptx file at <paramref name="outputPath"/> containing one slide
   /// with three columns (before image, execution logs, after image) and the
   /// <paramref name="scriptText"/> in the speaker-notes section.
   /// </summary>
   public static void CreateReport(
      string outputPath,
      string? beforeImagePath,
      string? afterImagePath,
      string executionLogs,
      string scriptText,
      string title)
   {
      using var doc = PresentationDocument.Create(outputPath, PresentationDocumentType.Presentation);

      var presPart = doc.AddPresentationPart();

      // ── Minimal slide master & layout ─────────────────────────────────────
      var masterPart = presPart.AddNewPart<SlideMasterPart>("rIdM1");
      var layoutPart = masterPart.AddNewPart<SlideLayoutPart>("rIdL1");

      layoutPart.SlideLayout = MakeSlideLayout();
      layoutPart.SlideLayout.Save();

      masterPart.SlideMaster = MakeSlideMaster(masterPart.GetIdOfPart(layoutPart));
      masterPart.SlideMaster.Save();

      // ── Slide ─────────────────────────────────────────────────────────────
      var slidePart = presPart.AddNewPart<SlidePart>("rIdS1");

      // ── Presentation root ─────────────────────────────────────────────────
      presPart.Presentation = new Presentation(
         new SlideMasterIdList(
            new SlideMasterId { Id = 2048U, RelationshipId = "rIdM1" }),
         new SlideIdList(
            new SlideId { Id = 256U, RelationshipId = "rIdS1" }),
         new SlideSize { Cx = (int)SW, Cy = (int)SH, Type = SlideSizeValues.Screen16x9 },
         new NotesSize { Cx = 6_858_000, Cy = 9_144_000 },
         new DefaultTextStyle()
      );
      presPart.Presentation.Save();

      BuildSlide(slidePart, beforeImagePath, afterImagePath, executionLogs, scriptText, title);
   }

   /// <summary>
   /// Opens an existing .pptx file at <paramref name="pptxPath"/> and appends a new slide
   /// with three columns (before image, execution logs, after image) and the
   /// <paramref name="scriptText"/> in the speaker-notes section.
   /// </summary>
   public static void AddPage(
      string pptxPath,
      string? beforeImagePath,
      string? afterImagePath,
      string executionLogs,
      string scriptText,
      string title)
   {
      using var doc = PresentationDocument.Open(pptxPath, isEditable: true);

      var presPart = doc.PresentationPart
         ?? throw new InvalidOperationException("The presentation has no PresentationPart.");

      var presentation = presPart.Presentation;
      var slideIdList = presentation.SlideIdList
         ?? presentation.AppendChild(new SlideIdList());

      // Determine the next available slide Id and relationship Id
      uint maxSlideId = slideIdList.Elements<SlideId>()
         .Select(s => s.Id?.Value ?? 0U)
         .DefaultIfEmpty(255U)
         .Max();

      int slideCount = slideIdList.Elements<SlideId>().Count();
      string slideRelId = $"rIdS{slideCount + 1}";

      var slidePart = presPart.AddNewPart<SlidePart>(slideRelId);

      // Link the new slide to the first available slide layout
      var layoutPart = presPart.SlideMasterParts
         .SelectMany(m => m.SlideLayoutParts)
         .First();
      slidePart.AddPart(layoutPart);

      slideIdList.Append(new SlideId { Id = maxSlideId + 1, RelationshipId = slideRelId });
      presentation.Save();

      BuildSlide(slidePart, beforeImagePath, afterImagePath, executionLogs, scriptText, title);
   }

   // ── Shared slide builder ──────────────────────────────────────────────────

   private static void BuildSlide(
      SlidePart slidePart,
      string? beforeImagePath,
      string? afterImagePath,
      string executionLogs,
      string scriptText,
      string title)
   {
      // ── Build shape tree ──────────────────────────────────────────────────
      var tree = MakeGroupTree();
      uint id = 2;

      // Title bar
      tree.Append(MakeTextBox(ref id, title,
         Col0X, Pad / 2, SW - 2 * Pad, TitleH - Pad / 2,
         fontSize: 2000, bold: true));

      // Column header labels
      tree.Append(MakeTextBox(ref id, "📷 Before",
         Col0X, TitleH, ColW, LabelH, fontSize: 1200, bold: true));
      tree.Append(MakeTextBox(ref id, "📋 Execution Logs",
         Col1X, TitleH, ColW, LabelH, fontSize: 1200, bold: true));
      tree.Append(MakeTextBox(ref id, "📷 After",
         Col2X, TitleH, ColW, LabelH, fontSize: 1200, bold: true));

      // Before image (left column)
      if (beforeImagePath is not null && File.Exists(beforeImagePath))
      {
         var imgPart = slidePart.AddImagePart(ImagePartType.Png, "rIdBefore");
         using var fs = File.OpenRead(beforeImagePath);
         imgPart.FeedData(fs);
         tree.Append(MakePicture(ref id, "Before", "rIdBefore",
            Col0X, ContentY, ColW, ContentH));
      }
      else
      {
         tree.Append(MakeTextBox(ref id, "(no before screenshot)",
            Col0X, ContentY, ColW, ContentH, fontSize: 1000));
      }

      // Execution logs (centre column)
      tree.Append(MakeTextBox(ref id, executionLogs,
         Col1X, ContentY, ColW, ContentH, fontSize: 800, monospace: true));

      // After image (right column)
      if (afterImagePath is not null && File.Exists(afterImagePath))
      {
         var imgPart = slidePart.AddImagePart(ImagePartType.Png, "rIdAfter");
         using var fs = File.OpenRead(afterImagePath);
         imgPart.FeedData(fs);
         tree.Append(MakePicture(ref id, "After", "rIdAfter",
            Col2X, ContentY, ColW, ContentH));
      }
      else
      {
         tree.Append(MakeTextBox(ref id, "(no after screenshot)",
            Col2X, ContentY, ColW, ContentH, fontSize: 1000));
      }

      slidePart.Slide = new Slide(
         new CommonSlideData(tree),
         new ColorMapOverride(new A.MasterColorMapping())
      );
      slidePart.Slide.Save();

      // ── Speaker notes ─────────────────────────────────────────────────────
      var notesPart = slidePart.AddNewPart<NotesSlidePart>();
      notesPart.NotesSlide = MakeNotesSlide(scriptText);
      notesPart.NotesSlide.Save();
   }

   // ── Shape builders ────────────────────────────────────────────────────────

   private static ShapeTree MakeGroupTree()
   {
      var tree = new ShapeTree();
      tree.Append(new NonVisualGroupShapeProperties(
         new NonVisualDrawingProperties { Id = 1U, Name = "" },
         new NonVisualGroupShapeDrawingProperties(),
         new ApplicationNonVisualDrawingProperties()));
      tree.Append(new GroupShapeProperties(
         new A.TransformGroup(
            new A.Offset { X = 0L, Y = 0L },
            new A.Extents { Cx = 0L, Cy = 0L },
            new A.ChildOffset { X = 0L, Y = 0L },
            new A.ChildExtents { Cx = 0L, Cy = 0L })));
      return tree;
   }

   private static Shape MakeTextBox(
      ref uint id, string text,
      long x, long y, long cx, long cy,
      int fontSize = 1200,
      bool bold = false,
      bool monospace = false)
   {
      var shapeId = id++;
      var runProps = new A.RunProperties
      {
         Language = "en-US",
         FontSize = fontSize,
         Bold = bold,
         Dirty = false
      };
      if (monospace)
         runProps.Append(new A.LatinFont { Typeface = "Courier New" });

      return new Shape(
         new NonVisualShapeProperties(
            new NonVisualDrawingProperties { Id = shapeId, Name = $"Shape{shapeId}" },
            new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties()),
         new ShapeProperties(
            new A.Transform2D(
               new A.Offset { X = x, Y = y },
               new A.Extents { Cx = cx, Cy = cy }),
            new A.PresetGeometry(new A.AdjustValueList())
               { Preset = A.ShapeTypeValues.Rectangle }),
         new TextBody(
            new A.BodyProperties
            {
               Wrap = A.TextWrappingValues.Square,
               LeftInset = 45_720,
               TopInset = 45_720,
               RightInset = 45_720,
               BottomInset = 45_720,
               Anchor = A.TextAnchoringTypeValues.Top
            },
            new A.ListStyle(),
            new A.Paragraph(
               new A.Run(runProps, new A.Text(text ?? string.Empty)))));
   }

   private static Picture MakePicture(
      ref uint id, string name, string relationshipId,
      long x, long y, long cx, long cy)
   {
      var picId = id++;
      return new Picture(
         new NonVisualPictureProperties(
            new NonVisualDrawingProperties { Id = picId, Name = name },
            new NonVisualPictureDrawingProperties(
               new A.PictureLocks { NoChangeAspect = true }),
            new ApplicationNonVisualDrawingProperties()),
         new BlipFill(
            new A.Blip { Embed = relationshipId },
            new A.Stretch(new A.FillRectangle())),
         new ShapeProperties(
            new A.Transform2D(
               new A.Offset { X = x, Y = y },
               new A.Extents { Cx = cx, Cy = cy }),
            new A.PresetGeometry(new A.AdjustValueList())
               { Preset = A.ShapeTypeValues.Rectangle }));
   }

   // ── Notes slide ───────────────────────────────────────────────────────────

   private static NotesSlide MakeNotesSlide(string scriptText)
   {
      var tree = new ShapeTree();
      tree.Append(new NonVisualGroupShapeProperties(
         new NonVisualDrawingProperties { Id = 1U, Name = "" },
         new NonVisualGroupShapeDrawingProperties(),
         new ApplicationNonVisualDrawingProperties()));
      tree.Append(new GroupShapeProperties(
         new A.TransformGroup(
            new A.Offset { X = 0L, Y = 0L },
            new A.Extents { Cx = 0L, Cy = 0L },
            new A.ChildOffset { X = 0L, Y = 0L },
            new A.ChildExtents { Cx = 0L, Cy = 0L })));

      // Notes body placeholder
      tree.Append(new Shape(
         new NonVisualShapeProperties(
            new NonVisualDrawingProperties { Id = 2U, Name = "Notes Placeholder 1" },
            new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties(
               new PlaceholderShape
               {
                  Type = PlaceholderValues.Body,
                  Index = 1U
               })),
         new ShapeProperties(),
         new TextBody(
            new A.BodyProperties(),
            new A.ListStyle(),
            new A.Paragraph(
               new A.Run(
                  new A.RunProperties { Language = "en-US", FontSize = 1000, Dirty = false },
                  new A.Text(scriptText ?? string.Empty))))));

      return new NotesSlide(
         new CommonSlideData(tree),
         new ColorMapOverride(new A.MasterColorMapping()));
   }

   // ── Minimal slide master & layout ─────────────────────────────────────────

   private static SlideMaster MakeSlideMaster(string layoutRelId)
   {
      var tree = new ShapeTree();
      tree.Append(new NonVisualGroupShapeProperties(
         new NonVisualDrawingProperties { Id = 1U, Name = "" },
         new NonVisualGroupShapeDrawingProperties(),
         new ApplicationNonVisualDrawingProperties()));
      tree.Append(new GroupShapeProperties(new A.TransformGroup()));

      return new SlideMaster(
         new CommonSlideData(tree),
         new A.ColorMap
         {
            Background1 = A.ColorSchemeIndexValues.Light1,
            Text1 = A.ColorSchemeIndexValues.Dark1,
            Background2 = A.ColorSchemeIndexValues.Light2,
            Text2 = A.ColorSchemeIndexValues.Dark2,
            Accent1 = A.ColorSchemeIndexValues.Accent1,
            Accent2 = A.ColorSchemeIndexValues.Accent2,
            Accent3 = A.ColorSchemeIndexValues.Accent3,
            Accent4 = A.ColorSchemeIndexValues.Accent4,
            Accent5 = A.ColorSchemeIndexValues.Accent5,
            Accent6 = A.ColorSchemeIndexValues.Accent6,
            Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
            FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink
         },
         new SlideLayoutIdList(
            new SlideLayoutId { Id = 2049U, RelationshipId = layoutRelId }),
         new TextStyles(
            new TitleStyle(),
            new BodyStyle(),
            new OtherStyle()));
   }

   private static SlideLayout MakeSlideLayout()
   {
      var tree = new ShapeTree();
      tree.Append(new NonVisualGroupShapeProperties(
         new NonVisualDrawingProperties { Id = 1U, Name = "" },
         new NonVisualGroupShapeDrawingProperties(),
         new ApplicationNonVisualDrawingProperties()));
      tree.Append(new GroupShapeProperties(new A.TransformGroup()));

      return new SlideLayout(
         new CommonSlideData(tree),
         new ColorMapOverride(new A.MasterColorMapping()))
      {
         Type = SlideLayoutValues.Blank,
         Preserve = true
      };
   }
}

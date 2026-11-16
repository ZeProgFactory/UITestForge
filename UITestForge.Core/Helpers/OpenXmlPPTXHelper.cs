
// dotnet add package DocumentFormat.OpenXml

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using P = DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;

namespace UITestForge.Helpers
{

   public class OpenXmlPPTXHelper
   {
      /*
      This method provides a helper method to set the master background image in a PowerPoint presentation (.pptx) file using the Open XML SDK.

        Usage
        SetasterBackgroundImage(
            "input.pptx",
            "background.png");

      */

      public static void SetMasterBackgroundImage(string pptxPath, string imagePath)
      {
         using (var document = PresentationDocument.Open(pptxPath, true))
         {
            var presentationPart = document.PresentationPart!;
            var masterPart = presentationPart.SlideMasterParts.First();

            // Add the background image to the master.
            // NOTE: use the ImagePartType overload. The string content-type overload
            // creates the part with a ".bin" extension, which makes PowerPoint report
            // the file as corrupt and drop the image during repair.
            var imagePart = masterPart.AddImagePart(GetImagePartType(imagePath));

            using (var imageStream = File.OpenRead(imagePath))
            {
               imagePart.FeedData(imageStream);
            }

            var relationshipId = masterPart.GetIdOfPart(imagePart);

            // Set the master background to the image.
            // p:bg must be the first child of p:cSld - remove any existing one first.
            var commonSlideData = masterPart.SlideMaster.CommonSlideData!;
            commonSlideData.Background?.Remove();

            commonSlideData.Background =
                new P.Background(
                    new P.BackgroundProperties(
                        // NOTE: must be the DrawingML a:blipFill - p:blipFill is not
                        // allowed inside p:bgPr and makes PowerPoint repair the file.
                        new A.BlipFill(
                            new A.Blip { Embed = relationshipId },
                            new A.Stretch(new A.FillRectangle())
                        ),
                        // a:effectLst is REQUIRED by CT_BackgroundProperties.
                        // Omitting it makes PowerPoint report the file as corrupt.
                        new A.EffectList()
                    )
                );

            masterPart.SlideMaster.Save();

            document.Save();

#if DEBUG
            var validator = new DocumentFormat.OpenXml.Validation.OpenXmlValidator();
            foreach (var error in validator.Validate(document))
            {
               System.Diagnostics.Debug.WriteLine(
                  $"[PPTX-VALIDATION] {error.ErrorType}: {error.Description} " +
                  $"(Part: {error.Part?.Uri}, Path: {error.Path?.XPath})");
            }
#endif
         }
      }


      /*
     This methof provides a helper method to set a background image for all slides in a PowerPoint presentation (.pptx) file using the Open XML SDK.

     Usage
     SetBackgroundImageForAllSlides(
         "input.pptx",
         "background.png");

      */

      static void SetBackgroundImageForAllSlides(string pptxPath, string imagePath)
      {
         using var document = PresentationDocument.Open(pptxPath, true);

         var presentationPart = document.PresentationPart
             ?? throw new InvalidOperationException("Missing presentation part.");

         foreach (var slidePart in presentationPart.SlideParts)
         {
            // Add the image to this slide's package part.
            ImagePart imagePart = slidePart.AddImagePart(
                GetImageContentType(imagePath));

            using (var stream = File.OpenRead(imagePath))
            {
               imagePart.FeedData(stream);
            }

            string relationshipId =
                slidePart.GetIdOfPart(imagePart);

            var slide = slidePart.Slide;
            var commonSlideData = slide.CommonSlideData
                ?? throw new InvalidOperationException("Missing slide data.");

            // Replace the existing background, if any.
            commonSlideData.Background?.Remove();

            var background = new P.Background(
                new P.BackgroundProperties(
                    new P.BlipFill(
                        new A.Blip
                        {
                           Embed = relationshipId
                        },
                        new A.Stretch(
                            new A.FillRectangle()))));

            // Insert background before the shape tree.
            commonSlideData.InsertBefore(
                background,
                commonSlideData.ShapeTree);

            slide.Save();
         }
      }

      public static PartTypeInfo GetImagePartType(string path)
      {
         return Path.GetExtension(path).ToLowerInvariant() switch
         {
            ".png" => ImagePartType.Png,
            ".jpg" or ".jpeg" => ImagePartType.Jpeg,
            ".gif" => ImagePartType.Gif,
            ".bmp" => ImagePartType.Bmp,
            ".tif" or ".tiff" => ImagePartType.Tiff,
            _ => throw new NotSupportedException(
                $"Unsupported image format: {Path.GetExtension(path)}")
         };
      }

      public static string GetImageContentType(string path)
      {
         return Path.GetExtension(path).ToLowerInvariant() switch
         {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".tif" or ".tiff" => "image/tiff",
            _ => throw new NotSupportedException(
                $"Unsupported image format: {Path.GetExtension(path)}")
         };
      }

   }
}

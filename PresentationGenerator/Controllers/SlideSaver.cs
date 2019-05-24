using System;
using System.DrawingCore;
using System.DrawingCore.Drawing2D;
using System.DrawingCore.Imaging;
using System.IO;
using CoreHtmlToImage;
using Presentation_Generator.Controllers.Fonts;
using Presentation_Generator.Models;

namespace Presentation_Generator.Controllers
{
    public static class SlideSaver
    {
        public static void SaveSlideAsJpeg(Slide slide, string outputPath)
        {
            var backgroundPicture = Image.FromFile(slide.PathToBackgroundPicture);
            var resizedBackground = new Bitmap(backgroundPicture, 800, 600);
            Graphics g = Graphics.FromImage(resizedBackground);
            PlaceText(slide, g);
            PlaceTitle(slide, g);
            // resizedBackground.Save("result.png");
            //PlaceTitleOnPicture(slide, resizedBackground);
            //PlaceTextOnPicture(slide, resizedBackground);
            resizedBackground.Save(outputPath, System.DrawingCore.Imaging.ImageFormat.Jpeg);
        }

        private static void PlaceTitle(Slide slide, Graphics g)
        {
            var converter = new HtmlConverter();
            var fulltitle = "<head><meta charset=\"utf - 8\"> </head>" + slide.Title;
            fulltitle = fulltitle.Replace("\n", "<br>");
            fulltitle = fulltitle.Replace("\r", " ");
            var bytesOfTitle = converter.FromHtmlString(fulltitle, width: 800, format: CoreHtmlToImage.ImageFormat.Png);
            Bitmap PngWithHtmlTitle;
            using (var ms = new MemoryStream(bytesOfTitle))
            {
                PngWithHtmlTitle = new Bitmap(ms);
            }
            PngWithHtmlTitle.MakeTransparent();
            int titleOffsetHorizontal = Int32.Parse(slide.TitleHorizontalOffset);
            int titleOffsetVertical = Int32.Parse(slide.TitleVerticalOffset);
            g.DrawImage(PngWithHtmlTitle, titleOffsetHorizontal, titleOffsetVertical);

        }
        private static void PlaceText(Slide slide, Graphics g)
        {
            var converter = new HtmlConverter();
            var fulltext = "<head><meta charset=\"utf - 8\"> </head>" + slide.Text;
            fulltext = fulltext.Replace("\n", "<br>");
            fulltext = fulltext.Replace("\r", " ");
            var bytesOfText = converter.FromHtmlString(fulltext, width: 800, format: CoreHtmlToImage.ImageFormat.Png);
            Bitmap PngWithHtmlText;
            using (var ms = new MemoryStream(bytesOfText))
            {
                PngWithHtmlText = new Bitmap(ms);
            }
            PngWithHtmlText.MakeTransparent();
            int textOffsetHorizontal = Int32.Parse(slide.SlideHorizontalOffset);
            int textOffsetVertical = Int32.Parse(slide.SlideVerticalOffset);
            g.DrawImage(PngWithHtmlText, textOffsetHorizontal, textOffsetVertical);
        }

        private static void PlaceTextOnPicture(Slide slide, Bitmap resizedBackground)
        {
            var offset = new Offset(100, 150, WordStyles.CommonFontSize, 400);
            var words = ExtractWordsFromText(slide);
            var wordStyle = WordStyles.CommonTextStyle;
            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];
                if (words[i].Contains("<"))
                {
                    wordStyle = WordStyles.GetWordStyle(words[i], WordStyles.CommonFontSize);
                    if (words[i].Contains('>'))
                        word = words[i].Remove(0, words[i].IndexOf('>') + 1);
                    else word = "";
                }
                word = word.Replace("$]", "");
                if (word.Contains('\n'))
                {
                    word = word.Replace("\n", "");
                    offset.NewLine();
                }

                DrawWord(resizedBackground, word, wordStyle, offset);
                if (words[i].Contains("$]")) wordStyle = WordStyles.CommonTextStyle;
                offset.TryMakeNewLine();
            }
        }

        private static void DrawWord(Bitmap resizedBackground, string word, WordStyle wordStyle, Offset offset)
        {
            var graphics = Graphics.FromImage(resizedBackground);
            var wordPosition = GetWordPosition(offset);
            graphics.DrawString(word,
                wordStyle.Font,
                wordStyle.SolidBrush, wordPosition,
                new StringFormat(StringFormatFlags.NoClip));
            offset.MoveRight(graphics.MeasureString(word, wordStyle.Font).Width);
        }

        private static RectangleF GetWordPosition(Offset offset)
        {
            return new RectangleF(offset.StartPosX + offset.X,
                offset.StartPosY + offset.Y,
                800, 50);
        }

        private static string[] ExtractWordsFromText(Slide slide)
        {
            var words = slide.Text
                .Split(new[] { " ", "\r" }, StringSplitOptions.RemoveEmptyEntries);
            words[0] = words[0].Replace("\n", " ");
            return words;
        }

        private static void PlaceTitleOnPicture(Slide slide, Bitmap resizedBackground)
        {
            var titleText = slide.Title;
            var titleStyle = WordStyles.TitleStyle;
            var titleGraphic = Graphics.FromImage(resizedBackground);
            var titlePosition = GetTitleTextPosition(titleGraphic, titleText, titleStyle, resizedBackground.Width);

            DrawTitleText(titleGraphic, titleText, titleStyle, titlePosition);
        }

        private static void DrawTitleText(Graphics titleGraphic, string title,
            WordStyle titleStyle, RectangleF titlePosition)
        {
            titleGraphic.DrawString(title,
                titleStyle.Font,
                titleStyle.SolidBrush, titlePosition,
                new StringFormat(StringFormatFlags.NoClip));
        }

        private static RectangleF GetTitleTextPosition(Graphics titleGraphic, string title,
            WordStyle titleStyle, int backGroundWidth)
        {
            var titleMeasurement = titleGraphic.MeasureString(title, titleStyle.Font).Width;
            float titleStartPosX = GetStartX(backGroundWidth, titleMeasurement);
            float titleStartPosY = 50;
            var titlePosition = new RectangleF(titleStartPosX, titleStartPosY, backGroundWidth - 100, 1000);
            return titlePosition;
        }
        private static float GetStartX(int backGroundWidth, float titleMeasurement)
        {
            if (titleMeasurement < (backGroundWidth - 100))
                return (backGroundWidth / 2) - (titleMeasurement / 2);
            return 50;
        }
    }
}
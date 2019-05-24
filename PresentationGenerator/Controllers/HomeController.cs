using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Presentation_Generator.Models;
using System.IO;
using System.Text;
using System.IO.Compression;
using System.Runtime.Serialization.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace Presentation_Generator.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment _env;
        public HomeController(IHostingEnvironment env)
        {
            _env = env;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private string GetServerPath(string path)
        {
            return Path.Combine(_env.WebRootPath, path);
        }

        private void SaveUploadedFile(IFormFile formFile, string path)
        {
            if (formFile.Length > 0)
            {
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    formFile.CopyTo(stream);
                }
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Error()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Slide(int id, string name)
        {
            var slidePaths = Directory.GetFiles(GetServerPath("Presentations/" + name + "/Slides")).ToList();
            var slides = slidePaths
              .Select(path => "~/Presentations/" + name + "/Slides/" + Path.GetFileName(path)).ToList();
            if (id >= slides.Count) id = id - 1;
            if (id < 0) id = 0;
            var jsonFormatter = new DataContractJsonSerializer(typeof(Slide));
            var jsonPath = GetServerPath("Presentations/" + name + "/SlidesJSON/" + id.ToString() + ".json");
            using (var fs = new FileStream(jsonPath, FileMode.OpenOrCreate))
            {
                var slide = (Slide)jsonFormatter.ReadObject(fs);
                ViewBag.SlideText = slide.Text;
                ViewBag.SlideHorizontalOffset = slide.SlideHorizontalOffset;
                ViewBag.SlideVerticalOffset = slide.SlideVerticalOffset;
                ViewBag.TitleHorizontalOffset = slide.TitleHorizontalOffset;
                ViewBag.TitleVerticalOffset = slide.TitleVerticalOffset;

                ViewBag.SlideTitle = slide.Title;
                ViewBag.BackgroundPath = slide.PathToBackgroundPicture;
                ViewBag.Warning = (slide.PathToBackgroundPicture.Contains("default.jpg")) ?
                    "Был загружен стандартный фон, так как архив не был прочитан." : "";
            }
            ViewBag.presDir = GetServerPath("Presentations/" + name);
            ViewBag.SlideId = id;
            ViewBag.SlideName = name;
            ViewBag.SlidePath = slides[id];
            ViewBag.NextSlide = "~/Home/Slide/" + (id + 1).ToString() + "/" + name;
            ViewBag.PreviousSlide = "~/Home/Slide/" + (id - 1).ToString() + "/" + name;
            ViewBag.DownloadLink = "~/Presentations/" + name + "/Slides.zip";
            return View();
        }

        [HttpPost]
        public RedirectResult Index(List<IFormFile> upload)
        {
            var presentationId = GetPresentationId();
            var presentationDir = GetServerPath("Presentations/" + presentationId);
            CreatePresentationDir(presentationDir);
            var textsFile = upload.ToArray()[0];
            var backgroundsFile = upload.ToArray()[1];
            if (backgroundsFile == null
                || !backgroundsFile.FileName.Contains(".zip")
                || !IsBackgroundsExtracted(presentationDir, backgroundsFile))
            {
                LoadDefaultBackground(presentationDir);
            }
            if (textsFile != null && textsFile.FileName.Contains(".txt") && IsSlidesCreated(presentationDir, textsFile))
            {
                return Redirect("/Home/Slide/0/" + presentationId);
            }
            return Redirect("/Home/Error");
        }

        [HttpPost]
        public RedirectResult Slide(string slideText, string slideTitle, string slideBg, string slideDir, string slideId, string slideName, string slideHorizontalOffset, string slideVerticalOffset, string titleHorizontalOffset, string titleVerticalOffset)
        {
            var modifiedSlide = new Slide();
            modifiedSlide.Title = slideTitle;
            modifiedSlide.Text = slideText;
            modifiedSlide.PathToBackgroundPicture = slideBg;
            modifiedSlide.SlideHorizontalOffset = slideHorizontalOffset;
            modifiedSlide.SlideVerticalOffset = slideVerticalOffset;
            modifiedSlide.TitleHorizontalOffset = titleHorizontalOffset;
            modifiedSlide.TitleVerticalOffset = titleVerticalOffset;

            SlideSaver.SaveSlideAsJpeg(modifiedSlide, slideDir + "/Slides/" + slideId + ".jpg");
            DataContractJsonSerializer jsonFormatter = new DataContractJsonSerializer(typeof(Slide));
            using (FileStream fs = new FileStream(slideDir + "/SlidesJSON/" + slideId + ".json", FileMode.Create))
            {
                jsonFormatter.WriteObject(fs, modifiedSlide);
            }
            ArchivePresentation(slideDir);
            return Redirect("/Home/Slide/" + slideId + "/" + slideName);
        }

        private void LoadDefaultBackground(string presentationDir)
        {
            System.IO.File.Copy(GetServerPath("Presentations/default.jpg"), presentationDir + "/Backgrounds/default.jpg");
        }




        public bool IsBackgroundsExtracted(string presentationDir, IFormFile backgroundsFile)
        {
            var fileName = Path.GetFileName(backgroundsFile.FileName);
            var filePath = presentationDir + "/" + fileName;
            SaveUploadedFile(backgroundsFile, filePath);
            try
            {
                ZipFile.ExtractToDirectory(filePath, presentationDir + "/Backgrounds");
                if (Directory.GetFiles(presentationDir + "/Backgrounds")
                    .Where(path => path.Contains(".png") || path.Contains(".jpg")).Count() == 0)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                return false;
            }
            return true;

        }

        private bool IsSlidesCreated(string presentationDir, IFormFile textsFile)
        {
            var fileName = Path.GetFileName(textsFile.FileName);
            var filePath = presentationDir + "/" + fileName;
            SaveUploadedFile(textsFile, filePath);
            //var fileStream = new StreamReader(filePath, Encoding.UTF32);
            //var fileText = fileStream.ReadToEnd();
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var fileText = System.IO.File.ReadAllText(filePath, Encoding.GetEncoding(1251));
            if (!fileText.Contains("СЛАЙД"))
                return false;
            var slideTexts = fileText
                .Split(new[] { "СЛАЙД: ", "СЛАЙД:" }, StringSplitOptions.RemoveEmptyEntries);
            var slides = new List<Slide>();
            foreach (var slideText in slideTexts)
            {
                var slide = new Slide();
                slide.Title = slideText.Split('\n')[0];
                slide.Text = slideText.Replace(slide.Title, "");
                slide.PathToBackgroundPicture = GetRandomBackground(presentationDir);
                slides.Add(slide);
            }
            SaveSlides(slides, presentationDir);
            return true;
        }

        private void CreatePresentationDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Directory.CreateDirectory(path + "/Backgrounds");
                Directory.CreateDirectory(path + "/Slides");
                Directory.CreateDirectory(path + "/SlidesJSON");
            }
        }

        private string GetRandomBackground(string presentationDir)
        {
            var BackgroundPaths = Directory.GetFiles(presentationDir + "/Backgrounds");
            BackgroundPaths = BackgroundPaths
               .Where(path => path.Contains(".png") || path.Contains(".jpg")).ToArray();
            return BackgroundPaths[new Random().Next(BackgroundPaths.Length)];
        }

        private string GetPresentationId()
        {
            var dateId = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            string[] charsToRemove = { ".", " ", ":", "/", "\\" };
            foreach (var symbol in charsToRemove)
            {
                dateId = dateId.Replace(symbol, "");
            }
            return dateId;
        }

        private void SaveSlides(List<Slide> slides, string presentationDir)
        {
            var jsonFormatter = new DataContractJsonSerializer(typeof(Slide));
            for (var i = 0; i < slides.Count; i++)
            {
                SlideSaver.SaveSlideAsJpeg(slides[i], presentationDir + "/Slides/" + i.ToString() + ".jpg");
                using (var fs = new FileStream(presentationDir + "/SlidesJSON/" + i.ToString() + ".json", FileMode.OpenOrCreate))
                {
                    jsonFormatter.WriteObject(fs, slides[i]);
                }
            }
            ArchivePresentation(presentationDir);
        }

        private void ArchivePresentation(string presentationDir)
        {
            if (System.IO.File.Exists(presentationDir + "/Slides.zip"))
                System.IO.File.Delete(presentationDir + "/Slides.zip");
            ZipFile.CreateFromDirectory(presentationDir + "/Slides", presentationDir + "/Slides.zip");
        }
    }
}
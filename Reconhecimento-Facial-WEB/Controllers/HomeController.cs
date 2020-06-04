using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Reconhecimento_Facial_WEB.Models;

namespace Reconhecimento_Facial_WEB.Controllers
{
    public class HomeController : Controller
    {

        private readonly IWebHostEnvironment _webHostEnvironment;

        const string subscriptionKey = "9be49c63cf354796b1f146154b928f6e";

        const string uriBase = "https://brazilsouth.api.cognitive.microsoft.com/face/v1.0/detect";

        private DadosFace DadosFace = null;


        public HomeController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View(new Face());
        }

        [HttpPost]
        public IActionResult Index(Face face)
        {

            ViewBag.DadosFace = null;
            var urlInterna = UploadedFile(face.Imagem);
            if (urlInterna != null)
            {
                MakeAnalysisRequest(new Uri(urlInterna).AbsolutePath);
            }

            var imagemname = new Uri(urlInterna).AbsolutePath;
            imagemname = imagemname.Substring(imagemname.LastIndexOf("/images"));

            while (this.DadosFace == null)
            {
                System.Threading.Thread.Sleep(2000);
            }

            var face2 = new Face()
            {
                url_imagem = imagemname,
                DadosFace = this.DadosFace,
                name = Environment.UserName
            };

            face2.DadosFace.FaceAttributes.Gender = face2.DadosFace.FaceAttributes.Gender == "male" ? "Masculino" : "Feminino";
            face2.DadosFace.FaceAttributes.Glasses = TratarGlasses(face2.DadosFace.FaceAttributes.Glasses);
            face2.DadosFace.FaceAttributes.Emotion.Anger = TrataEmocao(face2.DadosFace.FaceAttributes.Emotion.Anger);
            face2.DadosFace.FaceAttributes.Emotion.Neutral = TrataEmocao(face2.DadosFace.FaceAttributes.Emotion.Neutral);
            face2.DadosFace.FaceAttributes.Emotion.Contempt = TrataEmocao(face2.DadosFace.FaceAttributes.Emotion.Contempt);
            face2.DadosFace.FaceAttributes.Emotion.Disgust = TrataEmocao(face2.DadosFace.FaceAttributes.Emotion.Disgust);
            face2.DadosFace.FaceAttributes.Emotion.Happiness = TrataEmocao(face2.DadosFace.FaceAttributes.Emotion.Happiness);
            face2.DadosFace.FaceAttributes.Emotion.Sadness = TrataEmocao(face2.DadosFace.FaceAttributes.Emotion.Sadness);
            face2.DadosFace.FaceAttributes.Emotion.Surprise = TrataEmocao(face2.DadosFace.FaceAttributes.Emotion.Surprise);

            return View(face2);
        }

        private double TrataEmocao(double valor)
        {
            return valor * 100;
        }

        private string TratarGlasses(string glasses)
        {
            string ret = "";
            switch (glasses)
            {
                case "NoGlasses":
                    ret= "Sem óculos";
                    break;
                case "Reading Glasses":
                    ret = "Óculos de leitura";
                    break;
                case "Sunglasses":
                    ret = "Óculos de Sol";
                    break;
                case "Swimming Goggles":
                    ret = "Óculos de Natação";
                    break;
            }
            return ret;
        }

        private string UploadedFile(IFormFile file)
        {
            string filePath = null;

            if (file != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }
            }
            return filePath;
        }

        public async void MakeAnalysisRequest(string imageFilePath)
        {

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", subscriptionKey);

                string requestParameters = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";

                string uri = uriBase + "?" + requestParameters;

                HttpResponseMessage response;

                // Request body. Posts a locally stored JPEG image.
                byte[] byteData = GetImageAsByteArray(imageFilePath);

                using (ByteArrayContent content = new ByteArrayContent(byteData))
                {
                    // This example uses content type "application/octet-stream".
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Execute the REST API call.
                    response = await client.PostAsync(uri, content);

                    // Get the JSON response.
                    string contentString = await response.Content.ReadAsStringAsync();

                    this.DadosFace = JsonConvert.DeserializeObject<List<DadosFace>>(contentString)[0];
                }
            }

            static byte[] GetImageAsByteArray(string imageFilePath)
            {
                using (FileStream fileStream =
                    new FileStream(imageFilePath, FileMode.Open, FileAccess.Read))
                {
                    BinaryReader binaryReader = new BinaryReader(fileStream);
                    return binaryReader.ReadBytes((int)fileStream.Length);
                }
            }
        }
    }
}

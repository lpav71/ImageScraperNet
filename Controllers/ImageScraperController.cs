using HtmlAgilityPack;
using ImageScraper.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImageScraper.Controllers
{
    public class ImageScraperController : Controller
    {
        // Экземпляр HttpClient для выполнения HTTP-запросов
        private readonly HttpClient _httpClient;

        // Конструктор контроллера, инициализирующий HttpClient
        public ImageScraperController()
        {
            _httpClient = new HttpClient();
        }

        // Действие для отображения начальной страницы с формой ввода URL
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        // Действие для обработки отправки формы и выполнения процесса сбора изображений
        [HttpPost]
        public async Task<IActionResult> Scrape(string url)
        {
            // Проверка на пустое или null значение URL
            if (string.IsNullOrEmpty(url))
            {
                return View("Index");
            }

            // Сбор изображений с указанного URL
            var images = await GetImagesFromUrl(url);

            // Вычисление общего количества изображений и их суммарного размера
            ViewBag.TotalImages = images.Count;
            ViewBag.TotalSize = images.Sum(img => img.Size) / (1024.0 * 1024.0); // Размер в МБ

            // Возвращение результата во View 'Result'
            return View("Result", images);
        }

        // Вспомогательный метод для получения списка изображений с указанного URL
        private async Task<List<ImageInfo>> GetImagesFromUrl(string url)
        {
            var images = new List<ImageInfo>();

            // Выполнение HTTP запроса и получение HTML содержимого страницы
            var response = await _httpClient.GetStringAsync(url);

            // Создание и загрузка HTML документа
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(response);

            // Поиск всех тегов img с атрибутом src
            var imgNodes = htmlDoc.DocumentNode.SelectNodes("//img[@src]");

            // Если тегов img не найдено, возвращаем пустой список
            if (imgNodes == null) return images;

            // Обработка каждого найденного тега img
            foreach (var imgNode in imgNodes)
            {
                // Получение значения атрибута src
                var src = imgNode.Attributes["src"].Value;

                // Преобразование относительного URL в абсолютный
                if (!src.StartsWith("http"))
                {
                    var baseUri = new Uri(url);
                    src = new Uri(baseUri, src).ToString();
                }

                // Получение размера изображения
                var imageInfo = new ImageInfo { Url = src, Size = await GetImageSize(src) };

                // Добавление информации об изображении в список
                images.Add(imageInfo);
            }

            return images;
        }

        // Вспомогательный метод для получения размера изображения по URL
        private async Task<long> GetImageSize(string url)
        {
            // Создание HTTP запроса с методом HEAD
            var request = new HttpRequestMessage(HttpMethod.Head, url);

            // Выполнение HTTP запроса

            var response = await _httpClient.SendAsync(request);

            // Проверка успешного ответа
            if (response.IsSuccessStatusCode)
            {
                // Проверка на наличие заголовка Content-Length и возвращение его значения
                if (response.Content.Headers.ContentLength.HasValue)
                {
                    return response.Content.Headers.ContentLength.Value;
                }
            }

            // Возвращение нуля в случае ошибки определения размера
            return 0;
        }
    }
}

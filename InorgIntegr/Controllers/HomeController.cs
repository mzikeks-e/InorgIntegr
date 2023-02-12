using InorgIntegr.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace InorgIntegr.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(SearchRequest request)
        {
            if (ModelState.IsValid)
                Console.Write(request.Formula);

            
            if (request.ExportAs == ExportType.ToJson)
            {
                Response.Headers.Add("content-disposition",
                    $"attachment;filename={Path.ChangeExtension(request.Filename, null)}.json");
                Response.ContentType = "application/octectstream";

                return Json(await SearchModel.BuildJsonFromResponses(request));
            }

            return View("FindResult", request);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
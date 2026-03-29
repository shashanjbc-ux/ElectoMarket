using Microsoft.AspNetCore.Mvc;
using System.Diagnostics; // Necesario para Activity
using ElectoMarket.Models; // Necesario para ErrorViewModel (Asegúrate de que este sea el namespace de tus modelos)

namespace ElectoMarket.Controllers
{
    public class HomeController : Controller
    {
        // 🏠 PÁGINA PRINCIPAL (Inicio)
        public IActionResult Index()
        {
            // 👇 Aquí está la magia. Le decimos que cargue la vista "Default"
            return View("Default");
        }

        public IActionResult Conocenos()
        {
            return View();
        }

        // 🚨 MANEJADOR DE ERRORES (Páginas 404, 505 y otros)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("/Home/Error/{statusCode?}")]
        public IActionResult Error(int? statusCode = null)
        {
            // 🚩 Si el código es 404, cargamos el archivo "Error.cshtml" (el del teclado/cara)
            if (statusCode == 404)
            {
                return View("Error");
            }

            // 🚩 Si el código es 505, cargamos el archivo "Error505.cshtml" (el del robot)
            if (statusCode == 505)
            {
                return View("Error505");
            }

            // Para cualquier otro error (como el 500), puedes dejar la vista genérica
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
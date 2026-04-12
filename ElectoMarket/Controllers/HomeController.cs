using Microsoft.AspNetCore.Mvc;
using System.Diagnostics; // Necesario para Activity
using ElectoMarket.Models; // Necesario para ErrorViewModel (Asegúrate de que este sea el namespace de tus modelos)

namespace ElectoMarket.Controllers
{
    public class HomeController : Controller
    {
        //  PÁGINA PRINCIPAL (Inicio)
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Index()
        {
            //  Lo dejamos normal para que busque Index.cshtml automáticamente
            return View();
        }

        public IActionResult Conocenos()
        {
            return View();
        }

        //  RUTA PARA EL MINIJUEGO OCULTO
        public IActionResult Minijuego()
        {
            return View();
        }

        // 🎮 EL SALÓN ARCADE Y SUS JUEGOS
        public IActionResult Arcade() => View();
        public IActionResult CyberPong() => View();
        public IActionResult NeonRunner() => View();
        public IActionResult GlitchShooter() => View();

        //  MANEJADOR DE ERRORES (Páginas 404, 505 y otros)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [Route("/Home/Error/{statusCode?}")]
        public IActionResult Error(int? statusCode = null)
        {
            //  Si el código es 404, cargamos el archivo "Error.cshtml" (el del teclado/cara)
            if (statusCode == 404)
            {
                return View("Error");
            }

            //  Si el código es 505, cargamos el archivo "Error505.cshtml" (el del robot)
            if (statusCode == 505)
            {
                return View("Error505");
            }

            // Para cualquier otro error (como el 500), puedes dejar la vista genérica
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
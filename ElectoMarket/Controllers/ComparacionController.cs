using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElectoMarket.Controllers
{
    public class ComparacionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ComparacionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Acceso: /Comparacion
        public async Task<IActionResult> Index()
        {
            // 1. Recuperamos los IDs de la sesión
            var sessionStr = HttpContext.Session.GetString("ListaComparacion");
            var listaIds = string.IsNullOrEmpty(sessionStr)
                ? new List<int>()
                : JsonConvert.DeserializeObject<List<int>>(sessionStr);

            var viewModel = new ComparacionViewModel();

            // 2. Si hay IDs, traemos los productos de la BD
            if (listaIds.Any())
            {
                viewModel.Productos = await _context.Productos
                    .Include(p => p.Usuario)
                    .Where(p => listaIds.Contains(p.IdProducto))
                    .ToListAsync();
            }

            return View(viewModel);
        }

        // Acción para quitar un producto individualmente
        public IActionResult Quitar(int id)
        {
            var sessionStr = HttpContext.Session.GetString("ListaComparacion");
            if (!string.IsNullOrEmpty(sessionStr))
            {
                var listaIds = JsonConvert.DeserializeObject<List<int>>(sessionStr);
                if (listaIds.Contains(id))
                {
                    listaIds.Remove(id);
                    HttpContext.Session.SetString("ListaComparacion", JsonConvert.SerializeObject(listaIds));
                }
            }
            return RedirectToAction("Index");
        }

        // Acción para limpiar toda la comparación
        public IActionResult LimpiarTodo()
        {
            HttpContext.Session.Remove("ListaComparacion");
            return RedirectToAction("Index");
        }
    }
}
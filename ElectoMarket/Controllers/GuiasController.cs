using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ElectoMarket.Controllers
{
    public class GuiasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public GuiasController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============================================================
        // 1. LISTADO (INDEX) - Con Buscador y Estadísticas
        // ============================================================
        public async Task<IActionResult> Index(string buscar)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");

            // --- 📊 LÓGICA DE ESTADÍSTICAS PARA LOS CUADRITOS 📊 ---

            // 1. Contamos todas las guías del sistema
            ViewBag.TotalGuias = await _context.Guias.CountAsync();

            // 2. Sumamos las vistas de TODAS las guías
            ViewBag.VistasTotales = await _context.Guias.SumAsync(g => g.Vistas);

            // 3. Vistas de mis propias guías (Lo que el usuario ha logrado)
            ViewBag.MisVistas = await _context.Guias
                .Where(g => g.IdUsuario == miId)
                .SumAsync(g => (int?)g.Vistas) ?? 0;

            // --- 🔍 LÓGICA DE BÚSQUEDA ---
            ViewData["BuscarActual"] = buscar;

            var query = _context.Guias.Include(g => g.Usuario).AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(g => g.Titulo.Contains(buscar) || g.Descripcion.Contains(buscar));
            }

            // Devolvemos la lista ordenada por las más recientes
            var guias = await query.OrderByDescending(g => g.FechaPublicacion).ToListAsync();
            return View(guias);
        }

        // ============================================================
        // 2. DETALLES (REPRODUCTOR) - Con contador de vistas inteligente
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var guia = await _context.Guias
                .Include(g => g.Usuario)
                .FirstOrDefaultAsync(m => m.IdGuia == id);

            if (guia == null) return NotFound();

            // 🛡️ LÓGICA DE VISITAS: No cuenta si eres el dueño de la guía
            int? miId = HttpContext.Session.GetInt32("UsuarioId");

            if (guia.IdUsuario != miId)
            {
                guia.Vistas++; // Sumamos la visita
                _context.Update(guia);
                await _context.SaveChangesAsync();
            }

            return View(guia);
        }

        // ============================================================
        // 3. SUBIR UN VIDEO (GET/POST)
        // ============================================================
        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UsuarioId") == null)
                return RedirectToAction("Login", "Usuarios");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guia guia, IFormFile? archivoVideo)
        {
            int? idUs = HttpContext.Session.GetInt32("UsuarioId");
            if (idUs == null) return RedirectToAction("Login", "Usuarios");

            guia.IdUsuario = idUs.Value;
            guia.FechaPublicacion = DateTime.Now;
            guia.Vistas = 0; // Inicia en cero siempre

            ModelState.Remove("Usuario");
            ModelState.Remove("VideoUrl");

            if (ModelState.IsValid)
            {
                if (archivoVideo != null && archivoVideo.Length > 0)
                {
                    string wwwRootPath = _webHostEnvironment.WebRootPath;
                    string fileName = Guid.NewGuid().ToString() + "_" + archivoVideo.FileName;
                    string carpetaDestino = Path.Combine(wwwRootPath, "videos", "guias");

                    if (!Directory.Exists(carpetaDestino))
                        Directory.CreateDirectory(carpetaDestino);

                    string pathCompleto = Path.Combine(carpetaDestino, fileName);

                    using (var fileStream = new FileStream(pathCompleto, FileMode.Create))
                    {
                        await archivoVideo.CopyToAsync(fileStream);
                    }

                    guia.VideoUrl = "/videos/guias/" + fileName;
                }
                else if (string.IsNullOrEmpty(guia.VideoUrl))
                {
                    ModelState.AddModelError("", "Debes subir un archivo o pegar un enlace de video.");
                    return View(guia);
                }

                _context.Add(guia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(guia);
        }

        // ============================================================
        // 4. EDITAR VIDEO (GET/POST)
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var guia = await _context.Guias.FindAsync(id);
            if (guia == null) return NotFound();

            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");

            if (guia.IdUsuario != miId && miRol != 1)
            {
                return RedirectToAction(nameof(Index));
            }

            return View(guia);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Guia guia, IFormFile? archivoVideo)
        {
            if (id != guia.IdGuia) return NotFound();

            ModelState.Remove("Usuario");
            ModelState.Remove("archivoVideo");

            if (ModelState.IsValid)
            {
                try
                {
                    if (archivoVideo != null && archivoVideo.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(guia.VideoUrl) && !guia.VideoUrl.StartsWith("http"))
                        {
                            var antiguaRuta = Path.Combine(_webHostEnvironment.WebRootPath, guia.VideoUrl.TrimStart('/'));
                            if (System.IO.File.Exists(antiguaRuta))
                            {
                                System.IO.File.Delete(antiguaRuta);
                            }
                        }

                        string fileName = Guid.NewGuid().ToString() + "_" + archivoVideo.FileName;
                        string path = Path.Combine(_webHostEnvironment.WebRootPath, "videos/guias", fileName);

                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await archivoVideo.CopyToAsync(stream);
                        }
                        guia.VideoUrl = "/videos/guias/" + fileName;
                    }

                    _context.Update(guia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Guias.Any(e => e.IdGuia == guia.IdGuia)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(guia);
        }

        // ============================================================
        // 5. ELIMINAR VIDEO
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var guia = await _context.Guias.FindAsync(id);

            if (guia != null)
            {
                if (!string.IsNullOrEmpty(guia.VideoUrl) && !guia.VideoUrl.StartsWith("http"))
                {
                    var rutaFisica = Path.Combine(_webHostEnvironment.WebRootPath, guia.VideoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(rutaFisica))
                    {
                        System.IO.File.Delete(rutaFisica);
                    }
                }

                _context.Guias.Remove(guia);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
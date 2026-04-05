using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
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

        // ==========================================
        // 1. LISTADO (Catálogo de Guías)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var guias = await _context.Guias
                .Include(g => g.Usuario)
                .OrderByDescending(g => g.FechaPublicacion)
                .ToListAsync();
            return View(guias);
        }

        // ==========================================
        // 2. REPRODUCTOR DE VIDEO
        // ==========================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var guia = await _context.Guias
                .Include(g => g.Usuario)
                .FirstOrDefaultAsync(m => m.IdGuia == id);

            if (guia == null) return NotFound();

            return View(guia);
        }

        // ==========================================
        // 3. SUBIR UN VIDEO (GET)
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UsuarioId") == null)
                return RedirectToAction("Login", "Usuarios");

            return View();
        }

        // ==========================================
        // 3. SUBIR UN VIDEO (POST) - ACTUALIZADO
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Guia guia, IFormFile? archivoVideo)
        {
            int? idUs = HttpContext.Session.GetInt32("UsuarioId");
            if (idUs == null) return RedirectToAction("Login", "Usuarios");

            guia.IdUsuario = idUs.Value;
            guia.FechaPublicacion = DateTime.Now;

            ModelState.Remove("Usuario");
            ModelState.Remove("VideoUrl");

            if (ModelState.IsValid)
            {
                if (archivoVideo != null && archivoVideo.Length > 0)
                {
                    // --- NUEVA LÓGICA DE GUARDADO ---
                    string wwwRootPath = _webHostEnvironment.WebRootPath;

                    // Generamos un nombre único pero conservando el nombre original del archivo
                    string fileName = Guid.NewGuid().ToString() + "_" + archivoVideo.FileName;

                    // Definimos la carpeta destino: wwwroot/videos/guias
                    string carpetaDestino = Path.Combine(wwwRootPath, "videos", "guias");

                    // Nos aseguramos de que la carpeta exista
                    if (!Directory.Exists(carpetaDestino))
                        Directory.CreateDirectory(carpetaDestino);

                    // Ruta completa para guardar el archivo físicamente
                    string pathCompleto = Path.Combine(carpetaDestino, fileName);

                    using (var fileStream = new FileStream(pathCompleto, FileMode.Create))
                    {
                        await archivoVideo.CopyToAsync(fileStream);
                    }

                    // Guardamos la URL relativa para la base de datos
                    guia.VideoUrl = "/videos/guias/" + fileName;
                    // --------------------------------
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

        // ==========================================
        // 4. EDITAR VIDEO (GET)
        // ==========================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var guia = await _context.Guias.FindAsync(id);
            if (guia == null) return NotFound();

            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");
            if (guia.IdUsuario != miId && miRol != 1) return Forbid();

            return View(guia);
        }

        // ==========================================
        // 4. EDITAR VIDEO (POST)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Guia guia)
        {
            if (id != guia.IdGuia) return NotFound();

            ModelState.Remove("Usuario");

            if (ModelState.IsValid)
            {
                try
                {
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

        // ==========================================
        // 5. ELIMINAR VIDEO
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");

            var guia = await _context.Guias.FindAsync(id);

            if (guia != null)
            {
                if (guia.IdUsuario == miId || miRol == 1)
                {
                    // Si el video es local, borrar archivo físico
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
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ElectoMarket.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductosController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ============================================================
        // 1. LISTADO (INDEX) - Catálogo Público con Filtros Avanzados
        // ============================================================
        public async Task<IActionResult> Index(string buscar, string categoria, string vendedor, decimal? min, decimal? max, string orden)
        {
            // Guardamos los valores en ViewData para que persistan en los inputs de la vista
            ViewData["BuscarActual"] = buscar;
            ViewData["CatActual"] = categoria;
            ViewData["VendActual"] = vendedor;
            ViewData["MinActual"] = min;
            ViewData["MaxActual"] = max;
            ViewData["OrdenActual"] = orden;

            // Iniciamos la consulta filtrando para ocultar los productos vendidos
            var query = _context.Productos
                .Include(p => p.Usuario)
                .Where(p => !p.Vendido)
                .AsQueryable();

            // --- APLICACIÓN DE FILTROS ---
            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(p => p.Nombre.Contains(buscar) || p.Descripcion.Contains(buscar));

            if (!string.IsNullOrEmpty(categoria))
                query = query.Where(p => p.Categoria == categoria);

            if (!string.IsNullOrEmpty(vendedor))
                query = query.Where(p => p.Usuario.Nombre.Contains(vendedor));

            if (min.HasValue)
                query = query.Where(p => p.Precio >= min.Value);

            if (max.HasValue)
                query = query.Where(p => p.Precio <= max.Value);

            // --- LÓGICA DE ORDENACIÓN ---
            query = orden switch
            {
                "precio_asc" => query.OrderBy(p => p.Precio),
                "precio_desc" => query.OrderByDescending(p => p.Precio),
                "antiguos" => query.OrderBy(p => p.FechaPublicacion),
                _ => query.OrderByDescending(p => p.FechaPublicacion), // "recientes" por defecto
            };

            return View(await query.ToListAsync());
        }

        // ============================================================
        // 2. DETALLES DEL PRODUCTO
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        // ============================================================
        // 3. PANEL DE ADMINISTRACIÓN (SOLO ADMIN)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Gestionar(string buscar, string categoria, string vendedor, string ordenarPor)
        {
            int? uRol = HttpContext.Session.GetInt32("UsuarioRol");
            if (uRol != 1) return RedirectToAction("Index", "Home");

            var query = _context.Productos.Include(p => p.Usuario).AsQueryable();

            if (!string.IsNullOrEmpty(buscar)) query = query.Where(p => p.Nombre.ToLower().Contains(buscar.ToLower().Trim()));
            if (!string.IsNullOrEmpty(vendedor)) query = query.Where(p => p.Usuario.Nombre.ToLower().Contains(vendedor.ToLower().Trim()));
            if (!string.IsNullOrEmpty(categoria)) query = query.Where(p => p.Categoria == categoria);

            query = ordenarPor switch
            {
                "nom_asc" => query.OrderBy(p => p.Nombre),
                "nom_desc" => query.OrderByDescending(p => p.Nombre),
                "precio_asc" => query.OrderBy(p => p.Precio),
                "precio_desc" => query.OrderByDescending(p => p.Precio),
                _ => query.OrderByDescending(p => p.FechaPublicacion)
            };

            ViewBag.BuscarActual = buscar;
            ViewBag.VendedorActual = vendedor;
            ViewBag.CategoriaActual = categoria;
            ViewBag.OrdenActual = ordenarPor;

            return View(await query.ToListAsync());
        }

        // ============================================================
        // ACCIONES CRUD
        // ============================================================

        [HttpGet]
        public IActionResult Create()
        {
            if (HttpContext.Session.GetInt32("UsuarioId") == null) return RedirectToAction("Login", "Usuarios");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Producto producto, IFormFile archivoFoto)
        {
            int? idUs = HttpContext.Session.GetInt32("UsuarioId");
            if (idUs == null) return RedirectToAction("Login", "Usuarios");

            producto.IdUsuario = idUs.Value;
            producto.FechaPublicacion = DateTime.Now;

            ModelState.Remove("Usuario");
            ModelState.Remove("ImagenUrl");

            if (ModelState.IsValid)
            {
                if (archivoFoto != null && archivoFoto.Length > 0)
                {
                    var extension = Path.GetExtension(archivoFoto.FileName).ToLower();
                    var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".avif" };

                    if (!extensionesPermitidas.Contains(extension))
                    {
                        ModelState.AddModelError("", "❌ ALERTA DE SEGURIDAD: El archivo no es una imagen válida.");
                        return View(producto);
                    }

                    string carpeta = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "productos");
                    if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

                    string nombreUnico = Guid.NewGuid().ToString() + extension;
                    string rutaFisica = Path.Combine(carpeta, nombreUnico);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await archivoFoto.CopyToAsync(stream);
                    }
                    producto.ImagenUrl = $"/imagenes/productos/{nombreUnico}";
                }
                else
                {
                    ModelState.AddModelError("", "Debes subir una foto del producto.");
                    return View(producto);
                }

                _context.Add(producto);
                await _context.SaveChangesAsync();
                TempData["Success"] = "¡Producto publicado con éxito!";
                return RedirectToAction(nameof(Index));
            }

            return View(producto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");
            if (miId == null) return RedirectToAction("Login", "Usuarios");

            if (id == null) return NotFound();
            var producto = await _context.Productos.FindAsync(id);
            if (producto == null) return NotFound();
            if (producto.IdUsuario != miId && miRol != 1) return RedirectToAction("Index");

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto, IFormFile imagenArchivo)
        {
            if (id != producto.IdProducto) return NotFound();

            ModelState.Remove("Usuario");
            ModelState.Remove("imagenArchivo");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imagenArchivo != null && imagenArchivo.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "productos");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imagenArchivo.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imagenArchivo.CopyToAsync(fileStream);
                        }
                        producto.ImagenUrl = "/imagenes/productos/" + uniqueFileName;
                    }

                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.IdProducto)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(producto);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");
            if (miId == null) return RedirectToAction("Login", "Usuarios");

            if (id == null) return NotFound();
            var producto = await _context.Productos.Include(p => p.Usuario).FirstOrDefaultAsync(m => m.IdProducto == id);
            if (producto == null) return NotFound();

            if (producto.IdUsuario != miId && miRol != 1) return RedirectToAction("Index", "Home");

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDefinitivamente(int id)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");

            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                if (producto.IdUsuario == miId || miRol == 1)
                {
                    BorrarImagen(producto.ImagenUrl);
                    _context.Productos.Remove(producto);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Perfil", "Usuarios");
        }

        [HttpGet]
        public IActionResult AgregarAComparacion(int id, bool irAVersus = false)
        {
            var listaIds = ObtenerListaComparacionSesion();
            var existe = _context.Productos.Any(p => p.IdProducto == id);

            if (existe && !listaIds.Contains(id))
            {
                if (listaIds.Count < 4)
                {
                    listaIds.Add(id);
                    HttpContext.Session.SetString("ListaComparacion", JsonConvert.SerializeObject(listaIds));
                }
            }

            return irAVersus ? RedirectToAction("Index", "Comparacion") : RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVendido(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                int? miId = HttpContext.Session.GetInt32("UsuarioId");
                if (producto.IdUsuario == miId)
                {
                    producto.Vendido = !producto.Vendido;
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Perfil", "Usuarios");
        }

        // --- MÉTODOS AUXILIARES ---
        private List<int> ObtenerListaComparacionSesion()
        {
            var sessionStr = HttpContext.Session.GetString("ListaComparacion");
            return string.IsNullOrEmpty(sessionStr) ? new List<int>() : JsonConvert.DeserializeObject<List<int>>(sessionStr);
        }

        private void BorrarImagen(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return;
            var path = Path.Combine(_webHostEnvironment.WebRootPath, ruta.TrimStart('/'));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        private bool ProductoExists(int id) => _context.Productos.Any(e => e.IdProducto == id);
    }
}
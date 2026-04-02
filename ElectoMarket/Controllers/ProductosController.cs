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
        // 1. LISTADO (INDEX) - Catálogo Público Búsqueda Avanzada
        // ============================================================
        public async Task<IActionResult> Index(string buscar, string categoria, string vendedor, decimal? precioMinimo, decimal? precioMaximo, string ordenarPor)
        {
            var query = _context.Productos.Include(p => p.Usuario).AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(p => p.Nombre.Contains(buscar) || p.Descripcion.Contains(buscar));

            if (!string.IsNullOrEmpty(categoria))
                query = query.Where(p => p.Categoria == categoria);

            if (!string.IsNullOrEmpty(vendedor))
                query = query.Where(p => p.Usuario.Nombre.Contains(vendedor));

            if (precioMinimo.HasValue)
                query = query.Where(p => p.Precio >= precioMinimo.Value);

            if (precioMaximo.HasValue)
                query = query.Where(p => p.Precio <= precioMaximo.Value);

            switch (ordenarPor)
            {
                case "precio_asc": query = query.OrderBy(p => p.Precio); break;
                case "precio_desc": query = query.OrderByDescending(p => p.Precio); break;
                case "az": query = query.OrderBy(p => p.Nombre); break;
                case "za": query = query.OrderByDescending(p => p.Nombre); break;
                default: query = query.OrderByDescending(p => p.FechaPublicacion); break;
            }

            ViewBag.BuscarActual = buscar;
            ViewBag.CategoriaActual = categoria;
            ViewBag.VendedorActual = vendedor;
            ViewBag.PrecioMinimoActual = precioMinimo;
            ViewBag.PrecioMaximoActual = precioMaximo;
            ViewBag.OrdenActual = ordenarPor;

            return View(await query.ToListAsync());
        }

        // ============================================================
        // 2. DETALLES DEL PRODUCTO (CON DATOS DEL VENDEDOR)
        // ============================================================
        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Usuario) // ⬅️ ¡ESTA LÍNEA ES VITAL! Trae al vendedor y su teléfono
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        // ============================================================
        // 🔐 3. PANEL DE ADMINISTRACIÓN (SOLO ADMIN)
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
        // 🛒 ACCIONES DE PRODUCTOS (CREAR Y EDITAR)
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
            // 🟢 1. Asignar el ID del usuario de la sesión actual
            int? idUs = HttpContext.Session.GetInt32("UsuarioId");
            if (idUs == null) return RedirectToAction("Login", "Usuarios"); // Si no hay sesión, pa' fuera

            producto.IdUsuario = idUs.Value;

            // 🟢 2. Asignar la fecha y hora de publicación
            producto.FechaPublicacion = DateTime.Now;

            // 🟢 3. Ignorar errores del modelo de cosas que el usuario no llena en el formulario
            ModelState.Remove("Usuario");
            ModelState.Remove("ImagenUrl");

            if (ModelState.IsValid)
            {
                // 🛑 LA BÓVEDA DE SEGURIDAD: Validar que sea una imagen real
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

                // Guardar en base de datos
                _context.Add(producto);
                await _context.SaveChangesAsync(); // ¡AQUÍ YA NO HABRÁ ERROR!

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

        // AQUÍ ESTÁ EL NUEVO POST DE EDITAR INTEGRADO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Producto producto, IFormFile imagenArchivo)
        {
            // Verificamos que el ID coincida
            if (id != producto.IdProducto)
            {
                return NotFound();
            }

            // A veces ModelState falla por propiedades de navegación, limpiamos posibles errores
            ModelState.Remove("Usuario");
            ModelState.Remove("imagenArchivo");

            if (ModelState.IsValid)
            {
                try
                {
                    // 🟢 1. LÓGICA PARA ATRAPAR Y GUARDAR LA NUEVA IMAGEN 🟢
                    if (imagenArchivo != null && imagenArchivo.Length > 0)
                    {
                        // Buscamos la ruta de la carpeta wwwroot/imagenes/productos
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "imagenes", "productos");

                        // Si la carpeta no existe, la creamos
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Creamos un nombre único para la foto (para que no se sobreescriban)
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + imagenArchivo.FileName;
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Copiamos la foto a la carpeta
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imagenArchivo.CopyToAsync(fileStream);
                        }

                        // Le decimos al producto cuál es su nueva dirección de imagen
                        producto.ImagenUrl = "/imagenes/productos/" + uniqueFileName;
                    }
                    // Si "imagenArchivo" viene nulo, el producto conserva la ImagenUrl vieja gracias al input oculto.

                    // 🟢 2. GUARDAMOS LOS CAMBIOS EN LA BASE DE DATOS 🟢
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.IdProducto))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Si algo falla, lo devolvemos a la vista con sus datos
            return View(producto);
        }

        // ============================================================
        // 🗑️ C. ELIMINAR PRODUCTO
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            // 1. Obtenemos quién está logueado
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");

            // Si nadie ha iniciado sesión, al Login
            if (miId == null) return RedirectToAction("Login", "Usuarios");

            if (id == null) return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null) return NotFound();

            // 2. LA MAGIA ESTÁ AQUÍ: 
            // Si el producto NO es tuyo Y TAMPOCO eres administrador (rol 1), te saca al inicio.
            // Pero si el producto SÍ es tuyo, te deja pasar a la pantalla de confirmación.
            if (producto.IdUsuario != miId && miRol != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(producto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarDefinitivamente(int id)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");

            if (miId == null) return RedirectToAction("Login", "Usuarios");

            var producto = await _context.Productos.FindAsync(id);

            if (producto != null)
            {
                // 3. CANDADO DE SEGURIDAD EXTRA: Antes de borrar en la base de datos,
                // confirmamos de nuevo que seas el dueño o el admin.
                if (producto.IdUsuario == miId || miRol == 1)
                {
                    BorrarImagen(producto.ImagenUrl);
                    _context.Productos.Remove(producto);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction("Perfil", "Usuarios");
        }

        // ==========================================
        // ➕ AÑADIR PRODUCTO A COMPARACIÓN (AJAX/GET)
        // ==========================================
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

            if (irAVersus)
            {
                return RedirectToAction("Index", "Comparacion");
            }

            return RedirectToAction("Index");
        }

        // --- MÉTODOS AUXILIARES ---

        private List<int> ObtenerListaComparacionSesion()
        {
            var sessionStr = HttpContext.Session.GetString("ListaComparacion");
            if (string.IsNullOrEmpty(sessionStr))
            {
                return new List<int>();
            }
            return JsonConvert.DeserializeObject<List<int>>(sessionStr);
        }

        private void LimpiarModelState() { ModelState.Remove("Usuario"); ModelState.Remove("ImagenUrl"); }

        private async Task<string> GuardarImagenAsync(IFormFile archivo)
        {
            string nombreUnico = Guid.NewGuid().ToString() + Path.GetExtension(archivo.FileName);
            string ruta = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "productos", nombreUnico);
            using (var stream = new FileStream(ruta, FileMode.Create)) { await archivo.CopyToAsync(stream); }
            return $"/imagenes/productos/{nombreUnico}";
        }

        private void BorrarImagen(string ruta)
        {
            if (string.IsNullOrEmpty(ruta)) return;
            var path = Path.Combine(_webHostEnvironment.WebRootPath, ruta.TrimStart('/'));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }

        // Método agregado para que no falle el DbUpdateConcurrencyException en el Edit
        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.IdProducto == id);
        }
    }
}
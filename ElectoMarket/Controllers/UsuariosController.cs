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
using System.Security.Cryptography; // 🟢 Necesario para SHA-256
using System.Text;                  // 🟢 Necesario para convertir el texto
using System.Text.RegularExpressions; // 🟢 CRITICO para las validaciones de contraseña

namespace ElectoMarket.Controllers
{
  public class UsuariosController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public UsuariosController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
    {
      _context = context;
      _webHostEnvironment = webHostEnvironment;
    }

    // ==========================================
    // 🟢 FUNCIÓN PARA ENCRIPTAR (SHA-256) 🟢
    // ==========================================
    private string EncriptarSHA256(string texto)
    {
      if (string.IsNullOrEmpty(texto)) return "";

      using (SHA256 sha256Hash = SHA256.Create())
      {
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(texto));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
          builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
      }
    }

    // ==========================================
    // 1. REGISTRO
    // ==========================================
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(Usuario usuario)
    {
      ModelState.Remove("Productos");
      ModelState.Remove("Rol");

      if (ModelState.IsValid)
      {
        bool esPrimerUsuario = !_context.Usuarios.Any();
        usuario.RolId = esPrimerUsuario ? 1 : 2;

        usuario.Contrasena = EncriptarSHA256(usuario.Contrasena);

        _context.Add(usuario);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetInt32("UsuarioId", usuario.IdUsuario);
        HttpContext.Session.SetString("UsuarioNombre", usuario.Nombre);
        HttpContext.Session.SetInt32("UsuarioRol", usuario.RolId);

        TempData["Success"] = "¡Cuenta creada con éxito! Bienvenido a ElectroMarket.";

        return RedirectToAction("Index", "Productos");
      }
      return View(usuario);
    }

    // ==========================================
    // 2. LOGIN
    // ==========================================
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
      if (ModelState.IsValid)
      {
        var usuarioDb = await _context.Usuarios
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Correo == modelo.Correo);

        if (usuarioDb != null)
        {
          string hashLogin = EncriptarSHA256(modelo.Contrasena);

          if (usuarioDb.Contrasena == hashLogin) { /* Login exitoso */ }
          else if (usuarioDb.Contrasena == modelo.Contrasena)
          {
            usuarioDb.Contrasena = hashLogin;
            _context.Update(usuarioDb);
            await _context.SaveChangesAsync();
          }
          else
          {
            ModelState.AddModelError(string.Empty, "❌ Correo o contraseña incorrectos.");
            return View(modelo);
          }

          HttpContext.Session.SetInt32("UsuarioId", usuarioDb.IdUsuario);
          HttpContext.Session.SetString("UsuarioNombre", usuarioDb.Nombre);
          HttpContext.Session.SetInt32("UsuarioRol", usuarioDb.RolId);

          if (!string.IsNullOrEmpty(usuarioDb.FotoPerfilUrl))
            HttpContext.Session.SetString("UsuarioFoto", usuarioDb.FotoPerfilUrl);

          return RedirectToAction("Index", "Productos");
        }
        else
        {
          ModelState.AddModelError(string.Empty, "❌ Correo o contraseña incorrectos.");
        }
      }
      return View(modelo);
    }

    // ==========================================
    // 🔑 FLUJO DE RECUPERACIÓN DE CONTRASEÑA 🔑
    // ==========================================

    [HttpGet]
    public IActionResult SolicitarRecuperacion() => View();

    [HttpPost]
    public async Task<IActionResult> SolicitarRecuperacion(string correo)
    {
      var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
      if (usuario == null)
      {
        ViewBag.Error = "El correo electrónico no está registrado.";
        return View();
      }
      return RedirectToAction("RestablecerContrasena", new { correo = usuario.Correo });
    }

    [HttpGet]
    public IActionResult RestablecerContrasena(string correo)
    {
      ViewBag.Correo = correo;
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> RestablecerContrasena(string correo, string nuevaClave, string confirmarClave)
    {
      ViewBag.Correo = correo;

      if (nuevaClave != confirmarClave)
      {
        ViewBag.Error = "Las contraseñas no coinciden.";
        return View();
      }

      // Validar Requerimientos (Regex)
      var tieneMayuscula = new Regex(@"[A-Z]+");
      var tieneNumero = new Regex(@"[0-9]+");
      var tieneEspecial = new Regex(@"[\W|_]+");

      if (nuevaClave.Length < 6 || !tieneMayuscula.IsMatch(nuevaClave) ||
          !tieneNumero.IsMatch(nuevaClave) || !tieneEspecial.IsMatch(nuevaClave))
      {
        ViewBag.Error = "La contraseña debe tener: mínimo 6 caracteres, una mayúscula, un número y un carácter especial.";
        return View();
      }

      var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);
      if (usuario != null)
      {
        // 🔒 Usamos EncriptarSHA256 para mantener consistencia
        usuario.Contrasena = EncriptarSHA256(nuevaClave);
        _context.Update(usuario);
        await _context.SaveChangesAsync();

        TempData["MensajeExito"] = "¡Contraseña actualizada! Ya puedes iniciar sesión.";
        return RedirectToAction("Login");
      }

      return RedirectToAction("Login");
    }

    // ==========================================
    // 3. MI PERFIL
    // ==========================================
    public async Task<IActionResult> Perfil()
    {
      int? id = HttpContext.Session.GetInt32("UsuarioId");
      if (id == null) return RedirectToAction("Login");

      var user = await _context.Usuarios.FindAsync(id);
      if (user == null) return RedirectToAction("Logout");

      ViewBag.MisProductos = await _context.Productos
          .Where(p => p.IdUsuario == id)
          .ToListAsync();

      return View(user);
    }

    // ==========================================
    // 4. EDITAR PERFIL
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> EditPerfil()
    {
      int? id = HttpContext.Session.GetInt32("UsuarioId");
      if (id == null) return RedirectToAction("Login");

      var user = await _context.Usuarios.FindAsync(id);
      if (user == null) return NotFound();

      var model = new EditPerfilViewModel
      {
        Nombre = user.Nombre,
        Correo = user.Correo,
        Telefono = user.Telefono,
        Ciudad = user.Ciudad,
        FotoPerfilUrl = user.FotoPerfilUrl,
        Descripcion = user.Descripcion
      };

      return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditPerfil(EditPerfilViewModel model)
    {
      int? id = HttpContext.Session.GetInt32("UsuarioId");
      var user = await _context.Usuarios.FindAsync(id);
      if (user == null) return NotFound();

      if (ModelState.IsValid)
      {
        user.Nombre = model.Nombre;
        user.Correo = model.Correo;
        user.Ciudad = model.Ciudad;
        user.Descripcion = model.Descripcion;

        if (!string.IsNullOrEmpty(model.Telefono))
        {
          user.Telefono = model.Telefono.StartsWith("+57") ? model.Telefono : "+57 " + model.Telefono;
        }

        if (model.NuevaFoto != null)
        {
          string carpeta = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "perfiles");
          if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);

          if (!string.IsNullOrEmpty(user.FotoPerfilUrl))
          {
            string rutaAnterior = Path.Combine(_webHostEnvironment.WebRootPath, user.FotoPerfilUrl.TrimStart('/'));
            if (System.IO.File.Exists(rutaAnterior)) System.IO.File.Delete(rutaAnterior);
          }

          string nombreUnico = Guid.NewGuid().ToString() + Path.GetExtension(model.NuevaFoto.FileName);
          string rutaFisica = Path.Combine(carpeta, nombreUnico);

          using (var stream = new FileStream(rutaFisica, FileMode.Create))
          {
            await model.NuevaFoto.CopyToAsync(stream);
          }
          user.FotoPerfilUrl = $"/imagenes/perfiles/{nombreUnico}";
        }

        _context.Update(user);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("UsuarioNombre", user.Nombre);
        if (user.FotoPerfilUrl != null) HttpContext.Session.SetString("UsuarioFoto", user.FotoPerfilUrl);

        return RedirectToAction("Perfil");
      }
      return View(model);
    }

    // ============================================================
    // 🗑️ ELIMINAR MI PROPIA CUENTA
    // ============================================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarMiPerfil()
    {
      int? miId = HttpContext.Session.GetInt32("UsuarioId");
      if (miId == null) return RedirectToAction("Login");

      var usuario = await _context.Usuarios
          .Include(u => u.Productos)
          .FirstOrDefaultAsync(u => u.IdUsuario == miId);

      if (usuario != null)
      {
        var chats = await _context.Chats.Where(c => c.Usuario1Id == miId || c.Usuario2Id == miId).ToListAsync();
        foreach (var chat in chats)
        {
          var mensajes = await _context.Mensajes.Where(m => m.ChatId == chat.IdChat).ToListAsync();
          _context.Mensajes.RemoveRange(mensajes);
        }
        _context.Chats.RemoveRange(chats);

        if (usuario.Productos != null)
        {
          _context.Productos.RemoveRange(usuario.Productos);
        }

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();
      }

      HttpContext.Session.Clear();
      return RedirectToAction("Index", "Home");
    }

    // ==========================================
    // 5. CAMBIAR CONTRASEÑA (Desde Perfil)
    // ==========================================
    [HttpGet]
    public IActionResult CambiarPassword()
    {
      if (HttpContext.Session.GetInt32("UsuarioId") == null) return RedirectToAction("Login");
      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordViewModel model)
    {
      int? id = HttpContext.Session.GetInt32("UsuarioId");
      if (id == null) return RedirectToAction("Login");

      var user = await _context.Usuarios.FindAsync(id);
      if (user == null) return NotFound();

      if (!ModelState.IsValid) return View(model);

      string hashActual = EncriptarSHA256(model.PasswordActual);

      if (user.Contrasena != hashActual)
      {
        ModelState.AddModelError("PasswordActual", "❌ La contraseña actual no coincide.");
        return View(model);
      }

      user.Contrasena = EncriptarSHA256(model.PasswordNueva);
      _context.Update(user);
      await _context.SaveChangesAsync();

      HttpContext.Session.Clear();
      return RedirectToAction("Login", "Usuarios");
    }

    // ==========================================
    // 6. PANEL DE ADMINISTRACIÓN
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ListadoUsuarios(string buscarNombre, int? filtroRol)
    {
      int? miRol = HttpContext.Session.GetInt32("UsuarioRol");
      if (miRol != 1) return RedirectToAction("Index", "Home");

      ViewBag.ListaRoles = await _context.Roles.ToListAsync();

      var query = _context.Usuarios.Include(u => u.Rol).AsQueryable();

      if (!string.IsNullOrEmpty(buscarNombre))
        query = query.Where(u => u.Nombre.Contains(buscarNombre));

      if (filtroRol.HasValue)
        query = query.Where(u => u.RolId == filtroRol.Value);

      return View(await query.ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarRol(int idUsuario, int nuevoRolId)
    {
      var adminId = HttpContext.Session.GetInt32("UsuarioId");

      if (idUsuario == adminId)
      {
        TempData["Error"] = "No puedes cambiar tu propio rol.";
        return RedirectToAction("ListadoUsuarios");
      }

      var usuario = await _context.Usuarios.FindAsync(idUsuario);
      if (usuario != null)
      {
        usuario.RolId = nuevoRolId;
        _context.Update(usuario);
        await _context.SaveChangesAsync();
        TempData["Success"] = "¡Rol actualizado con éxito!";
      }
      return RedirectToAction("ListadoUsuarios");
    }

    [HttpPost, ActionName("EliminarConfirmado")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
      int? miRol = HttpContext.Session.GetInt32("UsuarioRol");
      if (miRol != 1) return Forbid();

      var usuario = await _context.Usuarios
          .Include(u => u.Productos)
          .FirstOrDefaultAsync(u => u.IdUsuario == id);

      if (usuario == null) return RedirectToAction("ListadoUsuarios");

      try
      {
        var chats = await _context.Chats.Where(c => c.Usuario1Id == id || c.Usuario2Id == id).ToListAsync();
        foreach (var chat in chats)
        {
          var mensajes = await _context.Mensajes.Where(m => m.ChatId == chat.IdChat).ToListAsync();
          _context.Mensajes.RemoveRange(mensajes);
        }
        _context.Chats.RemoveRange(chats);

        if (usuario.Productos != null) _context.Productos.RemoveRange(usuario.Productos);

        _context.Usuarios.Remove(usuario);
        await _context.SaveChangesAsync();

        return RedirectToAction("ListadoUsuarios");
      }
      catch (Exception ex)
      {
        TempData["Error"] = "Error al eliminar: " + ex.Message;
        return RedirectToAction("ListadoUsuarios");
      }
    }

    // ==========================================
    // 7. EXPORTAR Y OTROS
    // ==========================================
    [HttpGet]
    public async Task<IActionResult> ExportarMisProductos()
    {
      int? id = HttpContext.Session.GetInt32("UsuarioId");
      if (id == null) return RedirectToAction("Login");

      var misProductos = await _context.Productos.Where(p => p.IdUsuario == id).ToListAsync();
      var builder = new System.Text.StringBuilder();
      builder.AppendLine("#;Nombre del Producto;Fecha;Requiere Reparacion;Cantidad;Precio");

      int contador = 1; int totalStock = 0;
      foreach (var p in misProductos)
      {
        builder.AppendLine($"{contador};\"{p.Nombre}\";{p.FechaPublicacion:dd/MM/yyyy};{(p.RequiereReparacion ? "Si" : "No")};{p.Cantidad};{p.Precio:0}");
        contador++; totalStock += p.Cantidad;
      }
      builder.AppendLine($";\"TOTAL EN INVENTARIO\";;;{totalStock};");

      byte[] buffer = System.Text.Encoding.UTF8.GetBytes(builder.ToString());
      byte[] bom = new byte[] { 0xEF, 0xBB, 0xBF };
      byte[] fileBytes = new byte[bom.Length + buffer.Length];
      System.Buffer.BlockCopy(bom, 0, fileBytes, 0, bom.Length);
      System.Buffer.BlockCopy(buffer, 0, fileBytes, bom.Length, buffer.Length);

      return File(fileBytes, "text/csv", $"Inventario_{DateTime.Now:dd-MM-yyyy}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> VerPerfil(int id)
    {
      var user = await _context.Usuarios
          .Include(u => u.Productos)
          .FirstOrDefaultAsync(u => u.IdUsuario == id);

      if (user == null) return NotFound();
      ViewBag.ProductosPublicos = user.Productos?.Where(p => p.Cantidad > 0).ToList();
      return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
      HttpContext.Session.Clear();
      return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult LogoutAFK()
    {
      HttpContext.Session.Clear();
      return RedirectToAction("Login", "Usuarios");
    }
  }
}

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
using Microsoft.AspNetCore.SignalR; // 🟢 Para tiempo real
using ElectoMarket.Hubs;            // 🟢 Referencia a tu Hub

namespace ElectoMarket.Controllers
{
  public class ChatsController : Controller
  {
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IHubContext<ChatHub> _hubContext; // 🟢 El megáfono de SignalR

    public ChatsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment, IHubContext<ChatHub> hubContext)
    {
      _context = context;
      _webHostEnvironment = webHostEnvironment;
      _hubContext = hubContext;
    }

    // ==========================================
    // 1. VISTA PRINCIPAL (Bandeja de Entrada)
    // ==========================================
    public async Task<IActionResult> Index(int? chatId)
    {
      int? miId = HttpContext.Session.GetInt32("UsuarioId");
      if (miId == null) return RedirectToAction("Login", "Usuarios");

      var misChats = await _context.Chats
          .Include(c => c.Usuario1)
          .Include(c => c.Usuario2)
          .Include(c => c.Mensajes)
          .Where(c => (c.Usuario1Id == miId || c.Usuario2Id == miId) && c.Mensajes.Any())
          .OrderByDescending(c => c.Mensajes.Max(m => m.FechaEnvio))
          .AsNoTracking()
          .ToListAsync();

      ViewBag.MisChats = misChats;
      ViewBag.MiId = miId;

      if (chatId.HasValue)
      {
        var chatActivo = await _context.Chats
            .Include(c => c.Usuario1)
            .Include(c => c.Usuario2)
            .Include(c => c.Mensajes)
            .FirstOrDefaultAsync(c => c.IdChat == chatId && (c.Usuario1Id == miId || c.Usuario2Id == miId));

        return View(chatActivo);
      }

      return View(null);
    }

    // ==========================================
    // 2. INICIAR CHAT
    // ==========================================
    public async Task<IActionResult> IniciarChat(int receptorId)
    {
      int? miId = HttpContext.Session.GetInt32("UsuarioId");
      if (miId == null) return RedirectToAction("Login", "Usuarios");
      if (miId == receptorId) return RedirectToAction("Perfil", "Usuarios");

      var chatExistente = await _context.Chats
          .FirstOrDefaultAsync(c =>
              (c.Usuario1Id == miId && c.Usuario2Id == receptorId) ||
              (c.Usuario1Id == receptorId && c.Usuario2Id == miId));

      if (chatExistente != null)
        return RedirectToAction("Index", new { chatId = chatExistente.IdChat });

      var nuevoChat = new Chat { Usuario1Id = miId.Value, Usuario2Id = receptorId };
      _context.Chats.Add(nuevoChat);
      await _context.SaveChangesAsync();

      return RedirectToAction("Index", new { chatId = nuevoChat.IdChat });
    }

    // ==========================================
    // 3. ENVIAR MENSAJE (Multimedia + TIEMPO REAL 🚀)
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> EnviarMensaje(int chatId, string? texto, IFormFile? fotoAdjunta, IFormFile? audioAdjunto)
    {
      int? miId = HttpContext.Session.GetInt32("UsuarioId");
      if (miId == null) return RedirectToAction("Login", "Usuarios");

      if (string.IsNullOrWhiteSpace(texto) && (fotoAdjunta == null) && (audioAdjunto == null))
        return RedirectToAction("Index", new { chatId });

      var mensaje = new Mensaje
      {
        ChatId = chatId,
        RemitenteId = miId.Value,
        Texto = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim(),
        FechaEnvio = DateTime.Now
      };

      // Lógica de Foto
      if (fotoAdjunta != null && fotoAdjunta.Length > 0)
      {
        string carpetaImg = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "chats-multimedia");
        if (!Directory.Exists(carpetaImg)) Directory.CreateDirectory(carpetaImg);
        string nombreImg = Guid.NewGuid().ToString() + Path.GetExtension(fotoAdjunta.FileName);
        using (var stream = new FileStream(Path.Combine(carpetaImg, nombreImg), FileMode.Create))
        {
          await fotoAdjunta.CopyToAsync(stream);
        }
        mensaje.ImagenUrl = $"/imagenes/chats-multimedia/{nombreImg}";
      }

      // Lógica de Audio
      if (audioAdjunto != null && audioAdjunto.Length > 0)
      {
        string carpetaAud = Path.Combine(_webHostEnvironment.WebRootPath, "audios", "chats");
        if (!Directory.Exists(carpetaAud)) Directory.CreateDirectory(carpetaAud);
        string nombreAud = Guid.NewGuid().ToString() + ".webm";
        using (var stream = new FileStream(Path.Combine(carpetaAud, nombreAud), FileMode.Create))
        {
          await audioAdjunto.CopyToAsync(stream);
        }
        mensaje.AudioUrl = $"/audios/chats/{nombreAud}";
      }

      _context.Mensajes.Add(mensaje);
      await _context.SaveChangesAsync(); // 🟢 Aquí se genera el IdMensaje en la Base de Datos

      // 🟢 MAGIA DEL TIEMPO REAL: Ahora sí enviamos el IdMensaje al JavaScript
      await _hubContext.Clients.Group(chatId.ToString()).SendAsync("RecibirMensaje",
          mensaje.RemitenteId,
          mensaje.Texto,
          mensaje.ImagenUrl,
          mensaje.AudioUrl,
          mensaje.FechaEnvio.ToString("HH:mm"),
          mensaje.IdMensaje); // 👈 ¡PARÁMETRO AÑADIDO PARA EL ADMIN!

      return RedirectToAction("Index", new { chatId });
    }

    // ==========================================
    // 4. EDITAR MENSAJE (¡EXCLUSIVO ADMIN!)
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> EditarMensaje(int mensajeId, string nuevoTexto, int chatId)
    {
      int? miRol = HttpContext.Session.GetInt32("UsuarioRol");

      // 🛡️ Seguridad: Si no es Admin (Rol 1), bloqueamos la acción
      if (miRol != 1) return Forbid();

      if (string.IsNullOrWhiteSpace(nuevoTexto)) return RedirectToAction("Index", new { chatId });

      var mensaje = await _context.Mensajes.FindAsync(mensajeId);
      if (mensaje != null)
      {
        // Agregamos una nota automática para transparencia
        mensaje.Texto = nuevoTexto.Trim() + " (Modificado por Admin)";
        mensaje.FueEditado = true;
        _context.Update(mensaje);
        await _context.SaveChangesAsync();
      }
      return RedirectToAction("Index", new { chatId });
    }

    // ==========================================
    // 5. ELIMINAR MENSAJE (¡EXCLUSIVO ADMIN!)
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> EliminarMensaje(int mensajeId, int chatId)
    {
      int? miRol = HttpContext.Session.GetInt32("UsuarioRol");

      // 🛡️ Seguridad: Solo el admin tiene este poder
      if (miRol != 1) return Forbid();

      var mensaje = await _context.Mensajes.FindAsync(mensajeId);
      if (mensaje != null)
      {
        _context.Mensajes.Remove(mensaje);
        await _context.SaveChangesAsync();
      }
      return RedirectToAction("Index", new { chatId });
    }

    // ==========================================
    // 6. PERSONALIZAR CHAT
    // ==========================================
    [HttpPost]
    public async Task<IActionResult> PersonalizarChat(int chatId, string colorBurbuja, IFormFile imagenFondo)
    {
      int? miId = HttpContext.Session.GetInt32("UsuarioId");
      if (miId == null) return RedirectToAction("Login", "Usuarios");

      var chat = await _context.Chats.FindAsync(chatId);
      if (chat == null) return NotFound();

      bool soyUsuario1 = chat.Usuario1Id == miId;
      string nuevaRutaFondo = null;

      if (imagenFondo != null && imagenFondo.Length > 0)
      {
        string carpeta = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "fondos-chat");
        if (!Directory.Exists(carpeta)) Directory.CreateDirectory(carpeta);
        string nombre = Guid.NewGuid().ToString() + Path.GetExtension(imagenFondo.FileName);
        using (var stream = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create))
        {
          await imagenFondo.CopyToAsync(stream);
        }
        nuevaRutaFondo = $"/imagenes/fondos-chat/{nombre}";
      }

      if (soyUsuario1)
      {
        if (!string.IsNullOrEmpty(colorBurbuja)) chat.ColorBurbujaUsuario1 = colorBurbuja;
        if (nuevaRutaFondo != null) chat.FondoPantallaUrlUsuario1 = nuevaRutaFondo;
      }
      else
      {
        if (!string.IsNullOrEmpty(colorBurbuja)) chat.ColorBurbujaUsuario2 = colorBurbuja;
        if (nuevaRutaFondo != null) chat.FondoPantallaUrlUsuario2 = nuevaRutaFondo;
      }

      _context.Update(chat);
      await _context.SaveChangesAsync();
      return RedirectToAction("Index", new { chatId = chat.IdChat });
    }
  }
}

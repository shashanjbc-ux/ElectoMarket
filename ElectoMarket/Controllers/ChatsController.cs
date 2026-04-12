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
using Microsoft.AspNetCore.SignalR;
using ElectoMarket.Hubs;

namespace ElectoMarket.Controllers
{
    public class ChatsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<ChatHub> _hubContext;

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

            // Cargar solo los chats que NO están ocultos para el usuario actual
            var misChats = await _context.Chats
                .Include(c => c.Usuario1)
                .Include(c => c.Usuario2)
                .Include(c => c.Mensajes)
                .Where(c => (c.Usuario1Id == miId && !c.OcultoParaUsuario1) ||
                            (c.Usuario2Id == miId && !c.OcultoParaUsuario2))
                .OrderByDescending(c => c.Mensajes.Any() ? c.Mensajes.Max(m => m.FechaEnvio) : DateTime.MinValue)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.MisChats = misChats;
            ViewBag.MiId = miId;

            if (chatId.HasValue)
            {
                // Marcar como leídos y avisar por SignalR
                var mensajesSinLeer = await _context.Mensajes
                    .Where(m => m.ChatId == chatId.Value && m.RemitenteId != miId && !m.Leido)
                    .ToListAsync();

                if (mensajesSinLeer.Any())
                {
                    foreach (var m in mensajesSinLeer) m.Leido = true;
                    await _context.SaveChangesAsync();
                    await _hubContext.Clients.Group(chatId.Value.ToString()).SendAsync("MensajesLeidos", chatId.Value);
                }

                // Cargar el chat activo
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
        // 2. INICIAR CHAT (Blindado Anti-Crash)
        // ==========================================
        public async Task<IActionResult> IniciarChat(int id)
        {
            var miId = HttpContext.Session.GetInt32("UsuarioId");

            // 1. Si no hay sesión, lo mandamos a login
            if (miId == null) return RedirectToAction("Login", "Usuarios");

            // No puedes iniciar un chat contigo mismo
            if (miId.Value == id) return RedirectToAction("Perfil", "Usuarios");

            // 2. 🛡️ DEFENSA ANTI-CRASH: Verificar que AMBOS usuarios existan realmente en la BD
            var yoExisto = await _context.Usuarios.AnyAsync(u => u.IdUsuario == miId.Value);
            var receptorExiste = await _context.Usuarios.AnyAsync(u => u.IdUsuario == id);

            if (!yoExisto || !receptorExiste)
            {
                // Si alguien no existe (ej. sesión fantasma o usuario eliminado), cerramos la sesión por seguridad
                HttpContext.Session.Clear();
                TempData["Error"] = "El usuario no existe o la sesión caducó.";
                return RedirectToAction("Index", "Productos");
            }

            // 3. Buscar si ya tienen un chat abierto (oculto o visible)
            var chatExistente = await _context.Chats
                .FirstOrDefaultAsync(c => (c.Usuario1Id == miId.Value && c.Usuario2Id == id) ||
                                          (c.Usuario1Id == id && c.Usuario2Id == miId.Value));

            // Si ya existe, lo enviamos ahí
            if (chatExistente != null)
            {
                // Si el chat existía pero lo había borrado (ocultado), lo revivimos
                if (chatExistente.Usuario1Id == miId.Value) chatExistente.OcultoParaUsuario1 = false;
                else if (chatExistente.Usuario2Id == miId.Value) chatExistente.OcultoParaUsuario2 = false;

                _context.Update(chatExistente);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { chatId = chatExistente.IdChat });
            }

            // 4. Si no existe, creamos uno nuevo con total seguridad de que los IDs son válidos
            var nuevoChat = new Chat { Usuario1Id = miId.Value, Usuario2Id = id };
            _context.Chats.Add(nuevoChat);
            await _context.SaveChangesAsync(); // ¡AQUÍ YA NO EXPLOTARÁ!

            return RedirectToAction("Index", new { chatId = nuevoChat.IdChat });
        }

        // ==========================================
        // 3. ENVIAR MENSAJE (Multimedia + TIEMPO REAL)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> EnviarMensaje(int chatId, string? texto, IFormFile? imagen)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            if (miId == null) return Unauthorized(); // Usamos Unauthorized por el Fetch (AJAX)

            texto = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

            if (texto == null && imagen == null)
                return BadRequest("Mensaje vacío");

            var mensaje = new Mensaje
            {
                ChatId = chatId,
                RemitenteId = miId.Value,
                Texto = texto,
                FechaEnvio = DateTime.Now,
                Leido = false
            };

            // Lógica para guardar Fotos
            if (imagen != null && imagen.Length > 0)
            {
                string carpetaImg = Path.Combine(_webHostEnvironment.WebRootPath, "imagenes", "chats-multimedia");
                if (!Directory.Exists(carpetaImg)) Directory.CreateDirectory(carpetaImg);

                string nombreImg = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);
                using (var stream = new FileStream(Path.Combine(carpetaImg, nombreImg), FileMode.Create))
                {
                    await imagen.CopyToAsync(stream);
                }
                mensaje.ImagenUrl = $"/imagenes/chats-multimedia/{nombreImg}";
            }

            _context.Mensajes.Add(mensaje);

            // Al enviar un mensaje, nos aseguramos de que el chat deje de estar oculto para ambos
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat != null)
            {
                chat.OcultoParaUsuario1 = false;
                chat.OcultoParaUsuario2 = false;
                _context.Update(chat);
            }

            await _context.SaveChangesAsync();

            // Enviar notificación a los navegadores
            await _hubContext.Clients.Group(chatId.ToString()).SendAsync("RecibirMensaje",
                mensaje.RemitenteId,
                mensaje.Texto,
                mensaje.ImagenUrl,
                mensaje.FechaEnvio.ToString("HH:mm"),
                mensaje.IdMensaje);

            return Ok();
        }

        // ==========================================
        // 4. OCULTAR CHAT (Eliminación asimétrica tipo WhatsApp)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> OcultarMiChat(int chatId)
        {
            int? miId = HttpContext.Session.GetInt32("UsuarioId");
            if (miId == null) return RedirectToAction("Login", "Usuarios");

            var chat = await _context.Chats.FindAsync(chatId);
            if (chat != null)
            {
                // Solo ocultamos el chat para la persona que presionó el botón de la papelera
                if (chat.Usuario1Id == miId) chat.OcultoParaUsuario1 = true;
                else if (chat.Usuario2Id == miId) chat.OcultoParaUsuario2 = true;

                _context.Update(chat);
                await _context.SaveChangesAsync();
            }

            // Redirigimos al Index sin chatId para que se ponga la pantalla de "Selecciona un chat"
            return RedirectToAction("Index");
        }

        // ==========================================
        // 5. EDITAR MENSAJE (¡EXCLUSIVO ADMIN!)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> EditarMensaje(int mensajeId, string nuevoTexto, int chatId)
        {
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");
            if (miRol != 1) return Forbid();

            if (string.IsNullOrWhiteSpace(nuevoTexto)) return RedirectToAction("Index", new { chatId });

            var mensaje = await _context.Mensajes.FindAsync(mensajeId);
            if (mensaje != null)
            {
                mensaje.Texto = nuevoTexto.Trim() + " (Modificado por Admin)";
                mensaje.FueEditado = true;
                _context.Update(mensaje);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index", new { chatId });
        }

        // ==========================================
        // 6. ELIMINAR MENSAJE (¡EXCLUSIVO ADMIN!)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> EliminarMensaje(int mensajeId, int chatId)
        {
            int? miRol = HttpContext.Session.GetInt32("UsuarioRol");
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
        // 7. PERSONALIZAR CHAT (Colores y Fondos)
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
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    public class ChatsE2ETests
    {
        [Fact]
        public async Task AccesoChat_SinSesion_DebeRedirigirALogin()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 500
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();

            // 1. El robot intenta entrar directo a los chats sin estar logueado
            await page.GotoAsync("https://localhost:7212/Chats");

            // 2. El controlador debe detectar que no hay sesión y mandarlo al Login
            await page.WaitForURLAsync("**/Usuarios/Login");

            // 3. Verificamos que efectivamente estamos en la pantalla de Iniciar Sesión
            var body = await page.InnerHTMLAsync("body");
            Assert.Contains("Iniciar", body);
            Assert.Contains("Sesión", body);
        }

        [Fact]
        public async Task BandejaMensajes_UsuarioNuevo_DebeEstarVacia()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 800 // Modo cine para ver todo el proceso 🍿
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();

            // 1. Creamos un usuario rápido para tener una sesión válida
            await page.GotoAsync("https://localhost:7212/Usuarios/Register");
            await page.FillAsync("input[name='Nombre']", "Usuario Chat");

            string correoUnico = $"chateador_{System.DateTime.Now.Ticks}@test.com";
            await page.FillAsync("input[name='Correo']", correoUnico);

            await page.SelectOptionAsync("select[name='Ciudad']", new[] { "Bogotá" });
            await page.FillAsync("input[name='Contrasena']", "Chat2026!");
            await page.FillAsync("input[name='ConfirmarContrasena']", "Chat2026!");
            await page.ClickAsync("button[type='submit']");

            // 2. Esperamos llegar al catálogo tras el registro
            await page.WaitForURLAsync("**/Productos");

            // 3. Ahora sí, hacemos clic en el icono del Chat en la Navbar (o navegamos directo)
            await page.GotoAsync("https://localhost:7212/Chats");

            // 4. Verificamos que cargó la vista Premium del Chat
            var body = await page.InnerHTMLAsync("body");

            // 🟢 CORRECCIÓN: "Centro de Mensajes" se oculta si no hay chats.
            // Solo buscamos el texto de la pantalla vacía.
            Assert.Contains("Tu centro de mensajes está vacío", body);
        }
    }
}
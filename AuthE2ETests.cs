using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    public class AuthE2ETests : IAsyncLifetime
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;

        // 🟢 CONFIRMA EL PUERTO
        private const string BaseUrl = "https://localhost:7212";

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 500 // Cámara lenta
            });

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            _page = await context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        // ─────────────────────────────────────────────
        // 🤖 PRUEBA E2E: INICIO DE SESIÓN EXITOSO
        // ─────────────────────────────────────────────
        [Fact]
        public async Task Login_ConCredencialesValidas_DeberiaEntrarAlSistema()
        {
            // 1. El robot navega a la página de Login
            await _page.GotoAsync($"{BaseUrl}/Usuarios/Login");

            // ⚠️ OJO: Pon aquí tu correo y contraseña reales de prueba
            await _page.FillAsync("input[name='Correo']", "Shashanjbc@gmail.com");
            await _page.FillAsync("input[name='Contrasena']", "Fenixjuan321@");

            // 3. El robot busca el botón de enviar y hace clic
            await _page.ClickAsync("button[type='submit']");

            // 🔴 ¡MAGIA! CONGELAMOS EL TIEMPO AQUÍ PARA VER QUÉ PASA
            await _page.PauseAsync();

            // 4. Esperamos a que aparezca el botón...
            var botonSalir = _page.Locator("text=Salir");

            await botonSalir.WaitForAsync();

            Assert.True(await botonSalir.IsVisibleAsync(), "El robot no pudo iniciar sesión.");
        }
    }
}
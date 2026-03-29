using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    public class UsuarioJourneyE2ETests : IAsyncLifetime
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;

        private const string BaseUrl = "https://localhost:7212";

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 1000
            });

            var context = await _browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            _page = await context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        [Fact]
        public async Task FlujoCompleto_LoginBuscarYVerDetalles_DeberiaFuncionarSinErrores()
        {
            // FASE 1: LOGIN (Ya vimos que funciona porque en tu foto dice "Hola, Lux")
            await _page.GotoAsync($"{BaseUrl}/Usuarios/Login");
            await _page.FillAsync("input[name='Correo']", "lux158@gmail.com");
            await _page.FillAsync("input[name='Contrasena']", "A12345678!");
            await _page.ClickAsync("button[type='submit']");
            await _page.Locator("text=Salir").WaitForAsync();

            // FASE 2: CATÁLOGO
            await _page.GotoAsync($"{BaseUrl}/Productos");
            await _page.FillAsync("input[name='nombre']", "MicroWave");
            await _page.ClickAsync("button:has-text('Aplicar Filtros')");

            // FASE 3: CLIC EN "VER"
            var tarjeta = _page.Locator(".product-card").First;
            await tarjeta.WaitForAsync();

            // Buscamos el botón "Ver" que está dentro de la tarjeta
            var botonVer = tarjeta.GetByRole(AriaRole.Link, new() { Name = "Ver", Exact = true }).First;
            await botonVer.ClickAsync(new LocatorClickOptions { Force = true });

            // FASE 4: VERIFICACIÓN
            // Esperamos que la URL contenga Details
            await _page.WaitForURLAsync("**/Details/**");
            var tituloVisible = await _page.Locator("h1, h2").First.IsVisibleAsync();

            Assert.True(tituloVisible, "No se pudo cargar la página de detalles del MicroWave.");
        }
    }
}
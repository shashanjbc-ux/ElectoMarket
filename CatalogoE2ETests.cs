using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    // Usamos IAsyncLifetime para preparar el navegador antes de que empiece la prueba
    public class CatalogoE2ETests : IAsyncLifetime
    {
        private IPlaywright _playwright;
        private IBrowser _browser;
        private IPage _page;

        // 🟢 CONFIRMA EL PUERTO: En tu captura vi que usas el 7212
        private const string BaseUrl = "https://localhost:7212";

        public async Task InitializeAsync()
        {
            _playwright = await Playwright.CreateAsync();

            // Headless = false: Significa que SÍ queremos ver el navegador en pantalla 👀
            // SlowMo = 500: Frenamos al robot medio segundo por acción para que alcancemos a ver qué hace
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 500
            });

            // Ignoramos errores de "Sitio no seguro" de localhost
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            _page = await context.NewPageAsync();
        }

        public async Task DisposeAsync()
        {
            await _browser.CloseAsync();
            _playwright.Dispose();
        }

        // ─────────────────────────────────────────────
        // 🤖 PRUEBA E2E: BUSCAR PRODUCTO EN EL CATÁLOGO
        // ─────────────────────────────────────────────
        [Fact]
        public async Task BuscarProducto_FiltrarPorMicroWave_DeberiaMostrarResultados()
        {
            // 1. El robot navega a la página del catálogo
            await _page.GotoAsync($"{BaseUrl}/Productos");

            // 2. El robot busca el input del nombre y escribe "Xbox"
            await _page.FillAsync("input[name='nombre']", "MicroWave");

            // 3. El robot hace clic en el botón de filtros
            await _page.ClickAsync("button:has-text('Aplicar Filtros')");

            // 4. Esperamos a que la página recargue y aparezca al menos una tarjeta de producto
            await _page.WaitForSelectorAsync(".product-card");

            // 5. Contamos cuántos productos salieron
            var tarjetas = await _page.Locator(".product-card").CountAsync();

            // 6. Assert: Comprobamos que el robot encontró al menos 1 producto
            Assert.True(tarjetas > 0, "El robot no encontró ningún producto con la palabra 'Xbox'");
        }
    }
}
using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    public class CatalogoE2ETests
    {
        [Fact]
        public async Task FlujoUsuario_BuscarXboxYVerDetalles()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 800
            });

            //  Ignoramos el error de HTTPS
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();

            await page.GotoAsync("https://localhost:7212/Productos");

            var buscador = page.Locator("input[name='buscar']");
            await buscador.FillAsync("Xbox");
            await buscador.PressAsync("Enter");

            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var body = await page.InnerHTMLAsync("body");
            Assert.Contains("Xbox", body);

            await Task.Delay(1500);
        }
    }
}
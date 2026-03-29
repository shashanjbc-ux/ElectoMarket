using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
  public class AuthE2ETests
  {
    [Fact]
    public async Task RegistroNuevoUsuario_DebeFuncionarCorrectamente()
    {
      using var playwright = await Playwright.CreateAsync();
      await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
      {
        Headless = false,
        SlowMo = 500 // Un poco más rápido pero visible
      });
      var page = await browser.NewPageAsync();

      await page.GotoAsync("https://localhost:7212/Usuarios/Register");

      await page.FillAsync("input[name='Nombre']", "Nuevo Robot");
      await page.FillAsync("input[name='Correo']", "nuevo.bot@test.com"); // Correo nuevo para evitar error de duplicado
      await page.FillAsync("input[name='Ciudad']", "Medellín");
      await page.FillAsync("input[name='Contrasena']", "Segura123!");
      await page.FillAsync("input[name='ConfirmarContrasena']", "Segura123!");

      await page.ClickAsync("button[type='submit']");

      await page.WaitForURLAsync("**/Productos");
      var body = await page.InnerHTMLAsync("body");

      // Buscamos algo que SIEMPRE está en el Layout (como el nombre de tu marca)
      Assert.Contains("ElectroMarket", body);
    }
  }
}

using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    public class AuthE2ETests
    {
        [Fact]
        public async Task RegistroNuevoUsuario_DebeFuncionarConNuevoDiseno()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 800 // Aumentamos un poco para ver cómo interactúa con el nuevo diseño
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();

            await page.GotoAsync("https://localhost:7212/Usuarios/Register");

            // 1. Llenamos el nombre
            await page.FillAsync("input[name='Nombre']", "Robot Tecnologico");

            // Genera un correo único cada vez que corre la prueba para evitar duplicados
            string correoUnico = $"bot_{System.DateTime.Now.Ticks}@test.com";
            await page.FillAsync("input[name='Correo']", correoUnico);

            // 2. 🟢 CAMBIO CLAVE: Ahora seleccionamos del menú desplegable (Select)
            // Usamos 'Pasto' como ejemplo ya que está en tus opciones
            await page.SelectOptionAsync("select[name='Ciudad']", new[] { "Pasto" });

            // 3. Llenamos las contraseñas
            await page.FillAsync("input[name='Contrasena']", "Electro2026!");
            await page.FillAsync("input[name='ConfirmarContrasena']", "Electro2026!");

            // 4. Hacemos clic en el botón neón de registro
            await page.ClickAsync("button[type='submit']");

            // 5. 🟢 CORRECCIÓN: El controlador redirige a Productos, no al Perfil.
            await page.WaitForURLAsync("**/Productos");

            // 6. Verificamos que estamos logueados viendo el nombre en la Navbar
            var body = await page.InnerHTMLAsync("body");
            Assert.Contains("Robot Tecnologico", body);
        }
    }
}
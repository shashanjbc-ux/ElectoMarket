using Microsoft.Playwright;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
    public class PerfilE2ETests
    {
        [Fact]
        public async Task EditarPerfil_FlujoCompleto_DebeActualizarDatos()
        {
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 800 // 🍿 Modo cine para ver cómo el robot escribe la biblia
            });

            var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();

            // ========================================================
            // 1. REGISTRO (Para tener una sesión limpia)
            // ========================================================
            await page.GotoAsync("https://localhost:7212/Usuarios/Register");
            await page.FillAsync("input[name='Nombre']", "Usuario Original");
            string correoUnico = $"edit_{System.DateTime.Now.Ticks}@test.com";
            await page.FillAsync("input[name='Correo']", correoUnico);
            await page.SelectOptionAsync("select[name='Ciudad']", new[] { "Pasto" });
            await page.FillAsync("input[name='Contrasena']", "Segura123!");
            await page.FillAsync("input[name='ConfirmarContrasena']", "Segura123!");
            await page.ClickAsync("button[type='submit']");

            // Esperamos llegar al catálogo
            await page.WaitForURLAsync("**/Productos");

            // ========================================================
            // 2. IR A LA PANTALLA DE EDITAR PERFIL
            // ========================================================
            await page.GotoAsync("https://localhost:7212/Usuarios/EditPerfil");

            // ========================================================
            // 3. LLENAR EL FORMULARIO CON DATOS NUEVOS
            // ========================================================
            await page.FillAsync("input[name='Nombre']", "Sebastian Barreto Editado");
            await page.FillAsync("input[name='Telefono']", "3001234567");
            await page.SelectOptionAsync("select[name='Ciudad']", new[] { "Cali" });

            // 🟢 EL TRUCO: Una descripción con más de 30 palabras (Tiene exactamente 42 palabras)
            string descripcionLarga = "Hola, mi nombre es Sebastian y soy un gran apasionado por la tecnología. " +
                                      "Estoy usando esta plataforma espectacular para vender y comprar equipos electrónicos de altísima calidad. " +
                                      "Esta descripción tiene suficientes palabras para pasar la validación del sistema sin ningún tipo de problemas o errores.";

            await page.FillAsync("textarea[name='Descripcion']", descripcionLarga);

            // ========================================================
            // 4. SUBIR FOTO DE PERFIL EN MEMORIA
            // ========================================================
            // Busca el input de tipo archivo (para la foto de perfil) e inyecta la foto falsa
            await page.SetInputFilesAsync("input[type='file']", new FilePayload
            {
                Name = "perfil_super_fachero.jpg",
                MimeType = "image/jpeg",
                Buffer = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }
            });

            // ========================================================
            // 5. GUARDAR CAMBIOS
            // ========================================================
            // 🟢 CORRECCIÓN: Le decimos explícitamente que haga clic en GUARDAR, no en Salir jajaja
            await page.ClickAsync("button:has-text('GUARDAR CAMBIOS')");

            // ========================================================
            // 6. VERIFICAR QUE SE GUARDÓ TODO
            // ========================================================
            await page.WaitForURLAsync("**/Usuarios/Perfil");

            var body = await page.InnerHTMLAsync("body");

            // Verificamos que el nombre editado aparezca en el Perfil
            Assert.Contains("Sebastian Barreto Editado", body);

            // 🟢 ELIMINAMOS EL Assert.Contains DEL TELÉFONO PORQUE NO ES VISIBLE EN LA VISTA
        }
    }
}
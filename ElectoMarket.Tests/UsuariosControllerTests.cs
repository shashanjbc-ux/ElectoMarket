using ElectoMarket.Controllers;
using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
  public class UsuariosControllerTests
  {
    private static DbContextOptions<ApplicationDbContext> Opciones() =>
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private (UsuariosController controller, FakeSessionPro session) PrepararEntorno(ApplicationDbContext context)
    {
      var mockEnv = new Mock<IWebHostEnvironment>();
      mockEnv.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

      var session = new FakeSessionPro();
      var mockContext = new Mock<HttpContext>();
      mockContext.Setup(c => c.Session).Returns(session);

      var tempData = new TempDataDictionary(mockContext.Object, Mock.Of<ITempDataProvider>());

      var controller = new UsuariosController(context, mockEnv.Object)
      {
        ControllerContext = new ControllerContext { HttpContext = mockContext.Object },
        TempData = tempData
      };
      return (controller, session);
    }

    private string HashTest(string texto)
    {
      using var sha = SHA256.Create();
      byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(texto));
      return BitConverter.ToString(bytes).Replace("-", "").ToLower();
    }

    [Fact]
    public async Task Register_PrimerUsuario_DebeSerAdmin()
    {
      using var context = new ApplicationDbContext(Opciones());
      var (controller, _) = PrepararEntorno(context);
      var nuevoUsuario = new Usuario { Nombre = "A", Correo = "a@a.com", Contrasena = "A123!", ConfirmarContrasena = "A123!", Ciudad = "B" };
      await controller.Register(nuevoUsuario);
      var usuarioEnDb = await context.Usuarios.FirstAsync();
      Assert.Equal(1, usuarioEnDb.RolId);
    }

    [Fact]
    public async Task Login_DatosCorrectos_DebeIniciarSesion()
    {
      using var context = new ApplicationDbContext(Opciones());
      string clave = "Clave123!";
      context.Usuarios.Add(new Usuario { Nombre = "U", Correo = "u@u.com", Contrasena = HashTest(clave), Ciudad = "C" });
      await context.SaveChangesAsync();

      var (controller, session) = PrepararEntorno(context);
      var loginModel = new LoginViewModel { Correo = "u@u.com", Contrasena = clave };

      var resultado = await controller.Login(loginModel);
      Assert.IsType<RedirectToActionResult>(resultado);
      Assert.NotNull(session.Id);
    }

    [Fact]
    public async Task Perfil_SinSesion_DebeRedirigirALogin()
    {
      using var context = new ApplicationDbContext(Opciones());
      var (controller, _) = PrepararEntorno(context);
      var resultado = await controller.Perfil();
      var redirect = Assert.IsType<RedirectToActionResult>(resultado);
      Assert.Equal("Login", redirect.ActionName);
    }
  }
}

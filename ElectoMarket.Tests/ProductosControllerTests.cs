using ElectoMarket.Controllers;
using ElectoMarket.Data;
using ElectoMarket.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ElectoMarket.Tests
{
  public class ProductosControllerTests
  {
    private static DbContextOptions<ApplicationDbContext> Opciones() =>
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

    private (ProductosController controller, FakeSessionPro session) PrepararEntorno(ApplicationDbContext context)
    {
      var mockEnv = new Mock<IWebHostEnvironment>();
      mockEnv.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());

      var session = new FakeSessionPro();
      var mockContext = new Mock<HttpContext>();
      mockContext.Setup(c => c.Session).Returns(session);

      var controller = new ProductosController(context, mockEnv.Object)
      {
        ControllerContext = new ControllerContext { HttpContext = mockContext.Object }
      };
      return (controller, session);
    }

    [Fact]
    public async Task Index_FiltroNombreYPrecio_DebeRetornarResultadoExacto()
    {
      using var context = new ApplicationDbContext(Opciones());
      var user = new Usuario { IdUsuario = 1, Nombre = "V", Correo = "v@v.com", Contrasena = "1", Ciudad = "B" };
      context.Usuarios.Add(user);
      context.Productos.Add(new Producto { Nombre = "Xbox Series X", Precio = 500, Categoria = "Consolas", Descripcion = "D", ImagenUrl = "i", IdUsuario = 1 });
      await context.SaveChangesAsync();

      var (controller, _) = PrepararEntorno(context);
      var resultado = await controller.Index("Xbox", null, null, 400, 600, null);

      var viewResult = Assert.IsType<ViewResult>(resultado);
      var modelo = Assert.IsAssignableFrom<IEnumerable<Producto>>(viewResult.Model);
      Assert.NotEmpty(modelo);
    }

    [Fact]
    public async Task Delete_SiNoEsDuennoNiAdmin_DebeRedirigirAlHome()
    {
      using var context = new ApplicationDbContext(Opciones());
      context.Usuarios.Add(new Usuario { IdUsuario = 10, Nombre = "D", Correo = "d@d.com", Contrasena = "1", Ciudad = "B" });
      context.Productos.Add(new Producto { IdProducto = 50, IdUsuario = 10, Nombre = "P", Categoria = "C", Descripcion = "D", ImagenUrl = "I" });
      await context.SaveChangesAsync();

      var (controller, session) = PrepararEntorno(context);
      session.SetInt32("UsuarioId", 1);
      session.SetInt32("UsuarioRol", 2);

      var resultado = await controller.Delete(50);
      var redirect = Assert.IsType<RedirectToActionResult>(resultado);
      Assert.Equal("Home", redirect.ControllerName);
    }
  }
}

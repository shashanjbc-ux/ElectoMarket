using ElectoMarket.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace ElectoMarket.Tests
{
    public class BaseDeDatosTests
    {
        [Fact]
        public void ValidarUsuario_ConDatosCorrectos_DeberiaSerValido()
        {
            // ==========================================
            // 1. ARRANGE (Preparar el usuario)
            // ==========================================
            var usuario = new Usuario
            {
                Nombre = "Juan Perez",
                Correo = "juanprueba@.com",
                Contrasena = "Password123!", // <-- OJO AQUÍ
                ConfirmarContrasena = "Password123!",
                Ciudad = "Bogotá",
                RolId = 2
            };

            // ==========================================
            // 2. ACT (Llamar al guardia de validación)
            // ==========================================
            var validationContext = new ValidationContext(usuario);
            var resultadosValidacion = new List<ValidationResult>();

            bool esValido = Validator.TryValidateObject(usuario, validationContext, resultadosValidacion, validateAllProperties: true);

            // ==========================================
            // 3. ASSERT (La trampa de fuego)
            // ==========================================
            // Le exigimos a la prueba que el resultado DEBE ser verdadero (True)
            // Si llega a ser falso, la prueba se pondrá en ROJO y mostrará el mensaje que escribimos al lado.
            Assert.True(esValido, "¡ALERTA ROJA! El usuario fue rechazado. Revisa si le falta un carácter especial a la contraseña o algún dato obligatorio.");
        }
    }
}
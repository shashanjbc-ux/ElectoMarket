using ElectoMarket.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONFIGURACIÓN DE SERVICIOS
// ==========================================
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Configuración de la Base de Datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//  Habilitar el acceso al contexto
builder.Services.AddHttpContextAccessor();

// CONFIGURACIÓN DE SESIÓN (Memoria a Largo Plazo)
// 1. Cache de memoria necesario para que las sesiones sean estables
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1); // El servidor recuerda al usuario por 1 día
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(1); // Sobrevive aunque cierren el navegador
});

var app = builder.Build();

// ==========================================
// 2. CONFIGURACIÓN DEL PIPELINE (Middleware)
// ==========================================
// Manejo de errores 404 y excepciones
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting(); //  Primero el ruteo

// sion debe ir DESPUÉS de Routing y ANTES de Authorization para que el servidor 
// sepa de quién es la sesión antes de validar sus permisos.
app.UseSession();

app.UseAuthorization();

// ==========================================
// 3. CONFIGURACIÓN DE RUTAS
// ==========================================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ElectoMarket.Hubs.ChatHub>("/chatHub");

// ==============================================================
//  CREAR LA BASE DE DATOS SOLA
// ==============================================================
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // Esto revisa si la base de datos existe. Si no existe en la PC de tu profesor, ¡la crea al instante!
    context.Database.Migrate();
}
// ==============================================================

app.Run(); //  El código debe ir ANTES de esta línea
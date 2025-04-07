using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using AppMongo;
using Microsoft.AspNetCore.Mvc.Razor; // Añadido para RazorViewEngine

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de servicios (MVC + Rutas personalizadas)
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options => {
        // Ubicación personalizada de vistas
        options.ViewLocationFormats.Add("/Pages/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
    })
    .AddRazorRuntimeCompilation();

builder.Services.AddRazorPages();

// 2. Configuración MongoDB (original)
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// 3. Registro de servicios (original)
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddSingleton<ExportImportController>();

var app = builder.Build();

// Configurar escucha en todas las interfaces (original)
app.Urls.Add("http://0.0.0.0:8080");

// 4. Pipeline (original con ajuste de orden)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Database/Error");
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 5. Ruteo (original)
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Database}/{action=Index}/{id?}");

    endpoints.MapRazorPages();
    endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
});

app.Run();

public class MongoDBSettings
{
    public required string ConnectionString { get; set; }
}
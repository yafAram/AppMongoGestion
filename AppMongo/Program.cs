using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using AppMongo;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de servicios (MVC + Razor Pages)
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddRazorPages();  // <-- Nueva línea agregada

// 2. Configuración MongoDB (existente)
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// 3. Registro de servicios (existente)
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddSingleton<ExportImportController>();

var app = builder.Build();

// Configurar escucha en todas las interfaces (existente)
app.Urls.Add("http://0.0.0.0:8080");

// 4. Configuración del pipeline (modificado)
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

// 5. Ruteo combinado (MVC + Razor Pages)
app.UseEndpoints(endpoints =>
{
    // Mantener ruteo MVC existente
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Database}/{action=Index}/{id?}");

    // Nueva configuración para Razor Pages
    endpoints.MapRazorPages();

    // Mantener endpoint de salud
    endpoints.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));
});

app.Run();

public class MongoDBSettings
{
    public required string ConnectionString { get; set; }
}
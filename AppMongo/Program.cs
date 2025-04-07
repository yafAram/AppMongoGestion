using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using AppMongo;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de servicios
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

// 2. Configuración MongoDB
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

// 3. Registro de servicios
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddSingleton<ExportImportController>();

var app = builder.Build();

// Configurar escucha en todas las interfaces y puerto específico
app.Urls.Add("http://0.0.0.0:8080");  // <- Nueva línea agregada

// 4. Configuración del pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Database/Error");
}
else
{
    app.UseDeveloperExceptionPage();  // <- Mejor manejo de errores en desarrollo
}

// Deshabilitar HTTPS Redirection
// app.UseHttpsRedirection();  // <- Comentado

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 5. Ruteo
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Database}/{action=Index}/{id?}");

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();

public class MongoDBSettings
{
    public required string ConnectionString { get; set; }
}
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using AppMongo.Services;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using AppMongo;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuraci�n de servicios base
builder.Services.AddControllersWithViews();
builder.Services.AddLogging(configure => configure.AddConsole());

// 2. Configuraci�n MongoDB
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDB"));

// 3. Registro de servicios personalizados
builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);

    // Configuraci�n adicional para producci�n
    clientSettings.ServerApi = new ServerApi(ServerApiVersion.V1);
    clientSettings.ConnectTimeout = TimeSpan.FromSeconds(15);
    clientSettings.MaxConnectionPoolSize = 100;

    return new MongoClient(clientSettings);
});

// Registro de servicios de aplicaci�n
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddSingleton<ExportImportService>(); // Ejemplo de otro servicio

// 4. Configuraci�n de la aplicaci�n
var app = builder.Build();

// Pipeline de middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Request en producci�n: {Path}", context.Request.Path);
        await next();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 5. Configuraci�n de endpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Endpoint de salud para Docker/K8s
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// 6. Inicializaci�n de MongoDB (opcional)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var client = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        await client.ListDatabaseNamesAsync(); // Test de conexi�n
        Debug.WriteLine("Conexi�n a MongoDB verificada");
    }
    catch (Exception ex)
    {
        Debug.WriteLine($"Error de conexi�n a MongoDB: {ex.Message}");
        throw;
    }
}

app.Run();

// 7. Clases de configuraci�n
public class MongoDBSettings
{
    public required string ConnectionString { get; set; }
    public string DatabaseName { get; set; } = "admin"; // Default para gesti�n
    public bool EnableSSL { get; set; } = false;
}

// 8. Servicio de importaci�n/exportaci�n mejorado
public class ExportImportService
{
    private readonly IMongoClient _client;
    private readonly ILogger<ExportImportService> _logger;
    private readonly string _backupsPath = "/backups";

    public ExportImportService(
        IMongoClient client,
        ILogger<ExportImportService> logger)
    {
        _client = client;
        _logger = logger;

        if (!Directory.Exists(_backupsPath))
            Directory.CreateDirectory(_backupsPath);
    }

    public async Task<string> ImportCollectionAsync(
        string databaseName,
        string collectionName,
        IFormFile file,
        bool dropExisting = false)
    {
        var filePath = Path.Combine(_backupsPath, file.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var args = $"--uri \"{_client.Settings.Server.ToString()}\" " +
                  $"--db {databaseName} " +
                  $"--collection {collectionName} " +
                  $"--file {filePath} " +
                  (dropExisting ? "--drop " : "") +
                  $"--authenticationDatabase admin " +
                  $"--username {_client.Settings.Credential.Username} " +
                  $"--password {_client.Settings.Credential.Password}";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "mongoimport",
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("Error en importaci�n: {Error}", error);
            throw new ApplicationException($"Import failed: {error}");
        }

        return $"Importado a {databaseName}.{collectionName}";
    }
}
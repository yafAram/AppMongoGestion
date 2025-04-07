using Microsoft.AspNetCore.Mvc;
using AppMongo.Services;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AppMongo.Controllers
{
    public class DatabaseController : Controller
    {
        private readonly IMongoClient _mongoClient;
        private readonly ILogger<DatabaseController> _logger;
        private readonly SecurityService _securityService;

        public DatabaseController(
            IMongoClient mongoClient,
            ILogger<DatabaseController> logger,
            SecurityService securityService)
        {
            _mongoClient = mongoClient;
            _logger = logger;
            _securityService = securityService;
        }

        public IActionResult Index()
        {
            // Obtener lista de bases de datos para mostrar en la vista
            var databases = _mongoClient.ListDatabaseNames().ToList();
            return View(databases);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDatabase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                TempData["Message"] = "El nombre de la base de datos no puede estar vacío";
                return RedirectToAction("Index");
            }

            try
            {
                // MongoDB crea la base de datos al insertar el primer documento
                var database = _mongoClient.GetDatabase(databaseName);

                // Creamos una colección inicial
                await database.CreateCollectionAsync("init_collection");

                // Creamos un usuario admin para esta base de datos
                var userCreated = await _securityService.CreateUserAsync(
                    databaseName,
                    $"{databaseName}_admin",
                    "TempPassword123", // En producción usar generador de contraseñas
                    "dbOwner");

                if (!userCreated)
                {
                    throw new Exception("No se pudo crear el usuario administrador");
                }

                _logger.LogInformation("Base de datos {Database} creada", databaseName);
                TempData["Message"] = $"Base de datos '{databaseName}' creada exitosamente con usuario {databaseName}_admin";
            }
            catch (MongoException ex)
            {
                _logger.LogError(ex, "Error al crear base de datos {Database}", databaseName);
                TempData["Message"] = $"Error MongoDB: {ex.Message}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al crear base de datos");
                TempData["Message"] = $"Error: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDatabase(string databaseName)
        {
            try
            {
                // MongoDB no permite borrar la base de datos 'admin'
                if (databaseName.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["Message"] = "No se puede eliminar la base de datos 'admin'";
                    return RedirectToAction("Index");
                }

                await _mongoClient.DropDatabaseAsync(databaseName);
                _logger.LogInformation("Base de datos {Database} eliminada", databaseName);
                TempData["Message"] = $"Base de datos '{databaseName}' eliminada";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar base de datos {Database}", databaseName);
                TempData["Message"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
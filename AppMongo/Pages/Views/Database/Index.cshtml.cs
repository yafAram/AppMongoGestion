using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AppMongo.Pages.Views.Database
{
    [Route("/db-manager")]
    public class IndexModel : PageModel
    {
        private readonly IMongoClient _mongoClient;
        private readonly ILogger<IndexModel> _logger;
        private readonly SecurityService _securityService;

        [TempData]
        public string? Message { get; set; }

        public List<string> Databases { get; set; } = new();

        public IndexModel(
            IMongoClient mongoClient,
            ILogger<IndexModel> logger,
            SecurityService securityService)
        {
            _mongoClient = mongoClient;
            _logger = logger;
            _securityService = securityService;
        }

        public void OnGet()
        {
            Databases = _mongoClient.ListDatabaseNames().ToList();
        }

        public async Task<IActionResult> OnPostCreateDatabaseAsync(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                Message = "El nombre no puede estar vacío";
                return RedirectToPage();
            }

            try
            {
                var database = _mongoClient.GetDatabase(databaseName);
                await database.CreateCollectionAsync("init_collection");

                await _securityService.CreateUserAsync(
                    databaseName,
                    $"{databaseName}_admin",
                    "TempPassword123",
                    "dbOwner");

                _logger.LogInformation("Base de datos {Database} creada", databaseName);
                Message = $"Base de datos '{databaseName}' creada exitosamente";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear base de datos");
                Message = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteDatabaseAsync(string databaseName)
        {
            try
            {
                if (databaseName.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    Message = "No se puede eliminar la base de datos 'admin'";
                    return RedirectToPage();
                }

                await _mongoClient.DropDatabaseAsync(databaseName);
                _logger.LogInformation("Base de datos {Database} eliminada", databaseName);
                Message = $"Base de datos '{databaseName}' eliminada";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar base de datos");
                Message = $"Error: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
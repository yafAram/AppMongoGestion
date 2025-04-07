using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AppMongo
{
    public class ExportImportController : Controller
    {
        private readonly string _backupsPath = "/backups";
        private readonly ILogger<ExportImportController> _logger;

        public ExportImportController(ILogger<ExportImportController> logger)
        {
            _logger = logger;

            if (!Directory.Exists(_backupsPath))
            {
                Directory.CreateDirectory(_backupsPath);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Import(string databaseName, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("Archivo no válido");
                }

                var filePath = Path.Combine(_backupsPath, file.FileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "mongoimport",
                        Arguments = $"--uri=\"mongodb://root:example@mongodb:27017/{databaseName}?authSource=admin\" " +
                                    $"--collection {Path.GetFileNameWithoutExtension(file.FileName)} " +
                                    $"--file {filePath} " +
                                    "--drop " +
                                    "--gzip",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogInformation("Iniciando importación para {Database}", databaseName);
                process.Start();

                var errors = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Error en importación: {Error}", errors);
                    return BadRequest($"Error en importación: {errors}");
                }

                _logger.LogInformation("Importación completada para {Database}", databaseName);
                return RedirectToAction("Index", new { message = "Importación completada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en importación");
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AppMongo.Controllers
{
    public class ExportImportController : Controller
    {
        private readonly string _uploadsPath = "/backups/uploads"; // Usamos el volumen compartido
        private readonly string _mongoUri = "mongodb://root:example@mongodb:27017?authSource=admin";

        [HttpPost]
        public async Task<IActionResult> Import(string databaseName, IFormFile file)
        {
            // 1. Guardar el archivo subido
            if (!Directory.Exists(_uploadsPath))
            {
                Directory.CreateDirectory(_uploadsPath);
            }

            var filePath = Path.Combine(_uploadsPath, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 2. Ejecutar mongoimport (directamente desde el contenedor webapp)
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "mongoimport", // No usamos docker exec
                    Arguments = $"--uri=\"{_mongoUri}/{databaseName}?authSource=admin\" " +
                               $"--collection {Path.GetFileNameWithoutExtension(file.FileName)} " +
                               $"--file {filePath} " +
                               $"--drop", // Opcional: Borra la colección existente
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            // 3. Manejar resultado
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                return BadRequest($"Error en importación: {error}");
            }

            return RedirectToAction("Index");
        }
    }
}
using System.Diagnostics;

namespace AppMongo
{
    public class BackupService
    {
        public async Task<string> CreateBackupAsync(string databaseName)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var backupPath = $"/backups/{databaseName}_{timestamp}"; // Ruta correcta

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "mongodump", // Ejecutar directamente desde el contenedor webapp
                    Arguments = $"--uri=\"mongodb://root:example@mongodb:27017/{databaseName}?authSource=admin\" --out {backupPath}",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            return process.ExitCode == 0
                ? $"Backup creado en: {backupPath}"
                : $"Error: {await process.StandardError.ReadToEndAsync()}";
        }
    }
}
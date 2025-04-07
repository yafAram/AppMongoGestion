using System.Diagnostics;

namespace AppMongo.Services
{
    public class BackupService
    {
        private readonly ILogger<BackupService> _logger;

        public BackupService(ILogger<BackupService> logger)
        {
            _logger = logger;
        }

        public async Task<string> CreateBackupAsync(string databaseName)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupPath = $"/backups/{databaseName}_{timestamp}";

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "mongodump",
                        Arguments = $"--uri=\"mongodb://root:example@mongodb:27017/{databaseName}?authSource=admin\" " +
                                    $"--out {backupPath} " +
                                    "--gzip",
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                _logger.LogInformation("Iniciando backup para {Database}", databaseName);
                process.Start();

                var errors = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Error en backup: {Error}", errors);
                    return $"Error: {errors}";
                }

                _logger.LogInformation("Backup completado para {Database}", databaseName);
                return $"Backup creado en: {backupPath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear backup");
                return $"Error: {ex.Message}";
            }
        }
    }
}
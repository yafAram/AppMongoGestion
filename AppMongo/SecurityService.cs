using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AppMongo.Services
{
    public class SecurityService
    {
        private readonly IMongoClient _client;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(
            IMongoClient client,
            ILogger<SecurityService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> CreateUserAsync(
            string database,
            string username,
            string password,
            string role,
            string customData = null)
        {
            try
            {
                var adminDb = _client.GetDatabase("admin"); // Siempre usar admin para gestión de usuarios

                var command = new BsonDocument
                {
                    ["createUser"] = username,
                    ["pwd"] = password,
                    ["roles"] = new BsonArray
                    {
                        new BsonDocument
                        {
                            ["role"] = role,
                            ["db"] = database
                        }
                    },
                    ["customData"] = customData != null ? new BsonDocument
                    {
                        ["description"] = customData
                    } : BsonNull.Value,
                    ["mechanisms"] = new BsonArray("SCRAM-SHA-256"), // Fuerza mecanismo seguro
                    ["digestPassword"] = true
                };

                var result = await adminDb.RunCommandAsync<BsonDocument>(
                    command,
                    cancellationToken: default);

                _logger.LogInformation("Usuario {Username} creado en {Database} con rol {Role}",
                    username, database, role);

                return result["ok"].AsDouble == 1.0;
            }
            catch (MongoCommandException ex) when (ex.Code == 51003) // Usuario ya existe
            {
                _logger.LogWarning(ex, "El usuario {Username} ya existe", username);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario {Username}", username);
                throw new ApplicationException($"Error al crear usuario: {ex.Message}", ex);
            }
        }

        // Método adicional recomendado para eliminar usuarios
        public async Task<bool> DeleteUserAsync(string database, string username)
        {
            try
            {
                var adminDb = _client.GetDatabase("admin");
                var command = new BsonDocument
                {
                    ["dropUser"] = username
                };

                var result = await adminDb.RunCommandAsync<BsonDocument>(command);
                return result["ok"].AsDouble == 1.0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario {Username}", username);
                return false;
            }
        }
    }
}
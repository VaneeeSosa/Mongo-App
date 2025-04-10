using MongoDB.Driver;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication1.Services
{
    public class MongoDBService
    {
        private readonly IMongoClient _client;
        private readonly ILogger<MongoDBService> _logger;

        public MongoDBService(IMongoClient client, ILogger<MongoDBService> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task CreateDatabaseAsync(string databaseName)
        {
            try
            {
                await _client.GetDatabase(databaseName)
                    .CreateCollectionAsync("DefaultCollection");
                _logger.LogInformation("Base de datos {Database} creada", databaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear base de datos");
                throw;
            }
        }

        // Método para exportar (hacer backup) de la base de datos usando mongodump directamente
        public async Task ExportDatabaseAsync(string databaseName, string backupFolder)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "mongodump",
                    Arguments = $"--uri=mongodb://admin:AdminPassword123@mongodb:27017/ " +
                                $"--authenticationDatabase=admin " +
                                $"--db={databaseName} " +
                                $"--out={backupFolder}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Error en exportación: {Error}", error);
                    throw new Exception($"Error en exportación: {error}");
                }

                _logger.LogInformation("Backup de {Database} creado en: {Folder}", databaseName, backupFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar base de datos");
                throw;
            }
        }

        // Método para restaurar la base de datos usando mongorestore directamente (restauración desde archivo ZIP)
        public async Task RestoreBackupAsync(string databaseName, string zipFilePath)
        {
            try
            {
                // Se asume que zipFilePath es la ruta completa del archivo ZIP recibido
                // Se genera una ruta temporal para extraer el contenido del ZIP
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(zipFilePath);
                var tempExtractPath = Path.Combine("/tmp", fileNameWithoutExt);

                // Si ya existe el directorio de extracción, se elimina para evitar conflictos
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);

                // Extraer el ZIP en el directorio temporal
                ZipFile.ExtractToDirectory(zipFilePath, tempExtractPath, overwriteFiles: true);

                // Se asume que dentro del ZIP existe una carpeta con el nombre de la base de datos
                var restoreFolder = Path.Combine(tempExtractPath, databaseName);

                var processInfo = new ProcessStartInfo
                {
                    FileName = "mongorestore",
                    Arguments = $"--uri=mongodb://admin:AdminPassword123@mongodb:27017/ " +
                                $"--authenticationDatabase=admin " +
                                $"--db={databaseName} --drop {restoreFolder}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Error al restaurar backup: {Error}", error);
                    throw new Exception($"Error al restaurar backup: {error}");
                }

                _logger.LogInformation("Backup restaurado exitosamente para {Database}", databaseName);

                // (Opcional) Eliminar el directorio temporal extraído después de restaurar
                if (Directory.Exists(tempExtractPath))
                    Directory.Delete(tempExtractPath, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RestoreBackupAsync");
                throw;
            }
        }

        // Método para restaurar la base de datos usando mongorestore directamente (por carpeta)
        public async Task RestoreDatabaseAsync(string databaseName, string backupFolder)
        {
            // Aseguramos que la ruta use barras '/' (Linux)
            backupFolder = backupFolder.Replace("\\", "/");

            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "mongorestore",
                    Arguments = $"--uri=mongodb://admin:AdminPassword123@mongodb:27017/ " +
                                $"--authenticationDatabase=admin " +
                                $"--db={databaseName} " +
                                $"--drop {backupFolder}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Error en restauración: {Error}", error);
                    throw new Exception($"Error en restauración: {error}");
                }

                _logger.LogInformation("Backup de {Database} restaurado desde: {Folder}", databaseName, backupFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al restaurar backup");
                throw;
            }
        }

        // Método para exportar una colección específica usando mongoexport directamente
        public async Task ExportCollectionAsync(string databaseName, string collectionName)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var exportPath = $"/data/exports/{timestamp}_{collectionName}.json";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "mongoexport",
                    Arguments = $"--uri=mongodb://admin:AdminPassword123@mongodb:27017/ " +
                                $"--authenticationDatabase=admin " +
                                $"--db {databaseName} " +
                                $"--collection {collectionName} " +
                                $"--out {exportPath} " +
                                $"--jsonArray",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Error en exportación: {Error}", error);
                    throw new Exception($"Error en exportación: {error}");
                }

                _logger.LogInformation("Colección {Collection} exportada a: {Path}",
                    collectionName, exportPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ExportCollectionAsync");
                throw;
            }
        }

        public async Task<List<string>> ListDatabasesAsync()
        {
            try
            {
                var databases = await _client.ListDatabaseNamesAsync();
                return databases.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar bases de datos");
                throw;
            }
        }

        // Listado de backups disponibles (actualizado según tu entorno Linux)
        public List<string> GetAvailableBackups()
        {
            try
            {
                const string backupsPath = "/backup";
                return Directory.GetFiles(backupsPath, "*.zip")
                    .Select(Path.GetFileName)
                    .OrderByDescending(f => f)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar backups");
                throw;
            }
        }
    }
}

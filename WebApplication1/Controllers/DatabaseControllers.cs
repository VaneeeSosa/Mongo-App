using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System;

namespace WebApplication1.Controllers
{
    public class DatabaseController : Controller
    {
        private readonly MongoDBService _mongoService;
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(MongoDBService mongoService, ILogger<DatabaseController> logger)
        {
            _mongoService = mongoService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var databases = await _mongoService.ListDatabasesAsync();
            return View(databases);
        }

        [HttpPost]
        public async Task<IActionResult> CreateDatabase(string databaseName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(databaseName))
                {
                    TempData["Error"] = "El nombre de la base de datos no puede estar vacío";
                    return RedirectToAction("Index");
                }

                await _mongoService.CreateDatabaseAsync(databaseName);
                TempData["Success"] = $"Base de datos '{databaseName}' creada exitosamente!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error: {ex.Message}";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateBackup(string databaseName)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var backupPath = $"/data/backups/{timestamp}";

                var processInfo = new ProcessStartInfo
                {
                    FileName = "mongodump",
                    Arguments = $"--host=mongodb " +
                                $"--db {databaseName} " +
                                $"--authenticationDatabase admin " +
                                $"-u admin " +
                                $"-p adminpassword " +
                                $"--out {backupPath}",
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
                    _logger.LogError("Error en backup: {Error}", error);
                    TempData["Error"] = $"Error en backup: {error}";
                    return RedirectToAction("Index");
                }

                _logger.LogInformation("Backup de {Database} creado en: {Path}", databaseName, backupPath);
                TempData["Success"] = $"Backup de {databaseName} creado en: {backupPath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CreateBackup");
                TempData["Error"] = $"Error en CreateBackup: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    public class ExportImportController : Controller
    {
        private readonly MongoDBService _mongoService;
        private readonly IMongoClient _mongoClient;

        public ExportImportController(MongoDBService mongoService, IMongoClient mongoClient)
        {
            _mongoService = mongoService;
            _mongoClient = mongoClient;
        }

        // Muestra la vista exclusiva para la gestión de Backups (lista de archivos ZIP)
        public IActionResult Backups()
        {
            string backupFolder = "/backup";
            List<string> backupFiles = new List<string>();

            if (Directory.Exists(backupFolder))
            {
                backupFiles = Directory.GetFiles(backupFolder, "*.zip").ToList();
            }

            return View(backupFiles); // Model: List<string> (paths completos de los ZIP)
        }

        // Muestra la vista exclusiva para Importar/Exportar
        public async Task<IActionResult> ImportExport()
        {
            var databaseNames = (await _mongoClient.ListDatabaseNamesAsync()).ToList();
            return View(databaseNames); // Model: List<string>
        }

        [HttpPost]
        public async Task<IActionResult> Export(string database)
        {
            try
            {
                // Ruta: usar el volumen compartido /backup
                var backupFolder = Path.Combine("/backup", database, DateTime.Now.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(backupFolder);

                await _mongoService.ExportDatabaseAsync(database, backupFolder);

                // Crear ZIP en el mismo volumen
                var zipPath = Path.Combine("/backup", $"{database}-backup-{DateTime.Now:yyyyMMddHHmmss}.zip");
                if (System.IO.File.Exists(zipPath))
                    System.IO.File.Delete(zipPath);

                ZipFile.CreateFromDirectory(backupFolder, zipPath);

                // Descargar el ZIP generado
                return PhysicalFile(zipPath, "application/zip", $"{database}.zip");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }


        [HttpPost]
        public async Task<IActionResult> Import(string database, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No se ha seleccionado archivo");

                // Construir la ruta completa donde se guardará el archivo ZIP
                var zipPath = Path.Combine("/backup", file.FileName);

                // Guardar el archivo en el volumen compartido /backup
                using (var stream = new FileStream(zipPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Se pasa la ruta completa del archivo ZIP al método de restauración
                await _mongoService.RestoreBackupAsync(database, zipPath);

                TempData["Success"] = $"Backup restaurado exitosamente en la base de datos '{database}'";
                return RedirectToAction("ImportExport");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al importar backup: {ex.Message}";
                return RedirectToAction("ImportExport");
            }
        }


        public async Task<IActionResult> GetCollections(string database)
        {
            var db = _mongoClient.GetDatabase(database);
            var collections = await db.ListCollectionNamesAsync();
            return Json(collections.ToList());
        }
    }
}

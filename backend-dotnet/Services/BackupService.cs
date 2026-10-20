using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace MyKopdes.Services;

public class BackupService : IBackupService
{
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public BackupService(IConfiguration config, IWebHostEnvironment env)
    {
        _config = config;
        _env = env;
    }

    public async Task<Dictionary<string, object>> CreateBackupAsync(string? outputFile = null)
    {
        var connectionString = _config.GetConnectionString("DefaultConnection");
        var backupDir = Path.Combine(_env.ContentRootPath, "backups");
        Directory.CreateDirectory(backupDir);

        outputFile ??= Path.Combine(backupDir, $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql");

        // Parse connection string for mysqldump
        var server = "mysql";
        var port = "3306";
        var database = "koperasi";
        var user = "koperasi";
        var password = "koperasi123";

        var psi = new ProcessStartInfo
        {
            FileName = "mysqldump",
            Arguments = $"-h {server} -P {port} -u {user} -p{password} {database} -r \"{outputFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using var process = Process.Start(psi);
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                return new Dictionary<string, object> { ["success"] = false, ["error"] = error };
            }

            var fileInfo = new FileInfo(outputFile);
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["file"] = outputFile,
                ["size"] = fileInfo.Length,
                ["created_at"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object> { ["success"] = false, ["error"] = ex.Message };
        }
    }

    public async Task<string> GetLastBackupPathAsync()
    {
        var backupDir = Path.Combine(_env.ContentRootPath, "backups");
        if (!Directory.Exists(backupDir)) return "";

        var files = Directory.GetFiles(backupDir, "backup_*.sql")
            .OrderByDescending(f => new FileInfo(f).LastWriteTime)
            .ToList();

        return files.FirstOrDefault() ?? "";
    }

    public async Task<bool> RestoreBackupAsync(string backupFile)
    {
        if (!File.Exists(backupFile)) return false;

        var connectionString = _config.GetConnectionString("DefaultConnection");
        var server = "mysql";
        var port = "3306";
        var database = "koperasi";
        var user = "koperasi";
        var password = "koperasi123";

        var psi = new ProcessStartInfo
        {
            FileName = "mysql",
            Arguments = $"-h {server} -P {port} -u {user} -p{password} {database} -e \"source {backupFile}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        try
        {
            using var process = Process.Start(psi);
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

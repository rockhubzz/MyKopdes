using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;

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
        var backupDir = GetBackupDirectory();
        Directory.CreateDirectory(backupDir);

        // Only accept a filename from the API. This keeps the dump inside the
        // mounted backup volume even when an old client supplies outputFile.
        var fileName = string.IsNullOrWhiteSpace(outputFile)
            ? $"backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql.gz"
            : Path.GetFileName(outputFile);
        if (!fileName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
            !fileName.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase))
            fileName += ".sql.gz";
        var path = Path.Combine(backupDir, fileName);
        var compressed = fileName.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase);
        var dumpPath = compressed ? Path.Combine(backupDir, $".{fileName}.{Guid.NewGuid():N}.tmp.sql") : path;

        try
        {
            var process = StartMySqlClient("mysqldump", includeDatabase: true);
            process.StartInfo.ArgumentList.Add("--single-transaction");
            process.StartInfo.ArgumentList.Add("--result-file");
            process.StartInfo.ArgumentList.Add(dumpPath);
            // The process must be started after all arguments are configured.
            // StartMySqlClient creates a not-yet-started process for this path.
            if (!process.Start())
                return Failure("Unable to start mysqldump");
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                return Failure(error);
            }

            if (compressed)
            {
                await using var input = File.OpenRead(dumpPath);
                await using var output = File.Create(path);
                await using var gzip = new GZipStream(output, CompressionLevel.Fastest);
                await input.CopyToAsync(gzip);
                File.Delete(dumpPath);
            }

            var fileInfo = new FileInfo(path);
            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["file"] = path,
                ["size"] = fileInfo.Length,
                ["created_at"] = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            return Failure(ex.Message);
        }
    }

    public Task<IReadOnlyList<BackupFileInfo>> ListBackupFilesAsync()
    {
        var directory = GetBackupDirectory();
        if (!Directory.Exists(directory))
            return Task.FromResult<IReadOnlyList<BackupFileInfo>>(Array.Empty<BackupFileInfo>());

        var files = Directory.GetFiles(directory, "backup_*")
            .Where(path => path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .Select(file => new BackupFileInfo
            {
                Name = file.Name,
                SizeBytes = file.Length,
                CreatedAt = new DateTimeOffset(file.CreationTimeUtc).ToUnixTimeSeconds()
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupFileInfo>>(files);
    }

    public Task<string> GetLastBackupPathAsync()
    {
        var directory = GetBackupDirectory();
        if (!Directory.Exists(directory)) return Task.FromResult("");

        var path = Directory.GetFiles(directory, "backup_*.sql")
            .OrderByDescending(f => new FileInfo(f).CreationTimeUtc)
            .FirstOrDefault() ?? "";
        return Task.FromResult(path);
    }

    public Task<string?> ResolveBackupPathAsync(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return Task.FromResult<string?>(null);

        var safeName = Path.GetFileName(filename);
        if (!string.Equals(safeName, filename, StringComparison.Ordinal) ||
            !safeName.StartsWith("backup_", StringComparison.OrdinalIgnoreCase) ||
            (!safeName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
             !safeName.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult<string?>(null);

        var path = Path.Combine(GetBackupDirectory(), safeName);
        return Task.FromResult<string?>(File.Exists(path) ? path : null);
    }

    public async Task<bool> RestoreBackupAsync(string backupFile)
    {
        if (!File.Exists(backupFile)) return false;

        try
        {
            var process = StartMySqlClient("mysql", includeDatabase: true);
            if (!process.Start()) return false;

            await using var input = File.OpenRead(backupFile);
            if (backupFile.EndsWith(".sql.gz", StringComparison.OrdinalIgnoreCase))
            {
                await using var gzip = new GZipStream(input, CompressionMode.Decompress);
                await gzip.CopyToAsync(process.StandardInput.BaseStream);
            }
            else
            {
                await input.CopyToAsync(process.StandardInput.BaseStream);
            }
            process.StandardInput.Close();
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string GetBackupDirectory() => Path.Combine(_env.ContentRootPath, "backups");

    private static Dictionary<string, object> Failure(string error) => new()
    {
        ["success"] = false,
        ["error"] = error
    };

    private Process StartMySqlClient(string executable, bool includeDatabase)
    {
        var settings = ReadDatabaseSettings();
        var info = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardInput = executable == "mysql",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        info.ArgumentList.Add("-h");
        info.ArgumentList.Add(settings.Server);
        info.ArgumentList.Add("-P");
        info.ArgumentList.Add(settings.Port);
        info.ArgumentList.Add("-u");
        info.ArgumentList.Add(settings.User);
        info.ArgumentList.Add($"-p{settings.Password}");
        if (includeDatabase) info.ArgumentList.Add(settings.Database);
        return new Process { StartInfo = info };
    }

    private DatabaseSettings ReadDatabaseSettings()
    {
        var connectionString = _config.GetConnectionString("DefaultConnection")
            ?? _config["DB_CONNECTION_STRING"]
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException(
                "FATAL: No DB_CONNECTION_STRING configured for the backup service.");

        var values = new DbConnectionStringBuilder { ConnectionString = connectionString };
        return new DatabaseSettings(
            Get(values, "server", "mysql"),
            Get(values, "port", "3306"),
            Get(values, "user", Get(values, "uid", "koperasi")),
            Get(values, "password", ""),
            Get(values, "database", "koperasi"));
    }

    private static string Get(DbConnectionStringBuilder values, string key, string fallback)
        => values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(Convert.ToString(value))
            ? Convert.ToString(value)!
            : fallback;

    private sealed record DatabaseSettings(string Server, string Port, string User, string Password, string Database);
}

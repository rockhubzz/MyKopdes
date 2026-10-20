using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IBackupService
{
    Task<Dictionary<string, object>> CreateBackupAsync(string? outputFile = null);
    Task<string> GetLastBackupPathAsync();
    Task<bool> RestoreBackupAsync(string backupFile);
}

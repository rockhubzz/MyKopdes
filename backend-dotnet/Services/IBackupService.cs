using MyKopdes.Models;

namespace MyKopdes.Services;

public interface IBackupService
{
    Task<Dictionary<string, object>> CreateBackupAsync(string? outputFile = null);
    Task<IReadOnlyList<BackupFileInfo>> ListBackupFilesAsync();
    Task<string> GetLastBackupPathAsync();
    Task<string?> ResolveBackupPathAsync(string filename);
    Task<bool> RestoreBackupAsync(string backupFile);
}

public sealed class BackupFileInfo
{
    public string Name { get; init; } = null!;
    public long SizeBytes { get; init; }
    public long CreatedAt { get; init; }
}

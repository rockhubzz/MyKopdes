using Microsoft.AspNetCore.Http;

namespace MyKopdes.Services;

/// <summary>
/// Centralized handling of user-uploaded images (item photos, staff and
/// member avatars). Files are written under <c>wwwroot/storage/{category}/</c>
/// inside the backend container and the DB stores the RELATIVE path
/// ("avatars/user_1_123.png") so the value is directly servable at
/// <c>/storage/{relativePath}</c> (nginx proxies that to the backend's
/// static-files middleware).
/// </summary>
public interface IFileUploadService
{
    /// <summary>Saves an uploaded image and returns its storage-relative path,
    /// or null when the file has an unsupported type / exceeds the size cap.</summary>
    Task<string?> SaveImageAsync(IFormFile file, string category, string namePrefix);

    /// <summary>Deletes a previously stored folder-relative path (tolerates
    /// legacy bare filenames from before paths were stored relative).</summary>
    bool DeleteStoredImage(string? storedPath);
}

public class FileUploadService : IFileUploadService
{
    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

    private const long MaxBytes = 10 * 1024 * 1024; // nginx allows 20M

    private readonly string _storageRoot;

    public FileUploadService(IWebHostEnvironment env)
    {
        // Must exist before the static-files middleware mounts it.
        _storageRoot = Path.Combine(env.ContentRootPath, "wwwroot", "storage");
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string?> SaveImageAsync(IFormFile file, string category, string namePrefix)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxBytes) return null;

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            return null;

        var uploadsDir = Path.Combine(_storageRoot, category);
        Directory.CreateDirectory(uploadsDir);

        var filename = $"{namePrefix}_{DateTime.UtcNow.Ticks}{extension}";
        var filepath = Path.Combine(uploadsDir, filename);
        using (var stream = new FileStream(filepath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return $"{category}/{filename}";
    }

    public bool DeleteStoredImage(string? storedPath)
    {
        if (string.IsNullOrEmpty(storedPath)) return false;

        // Today's values are "category/name.ext". A bare legacy filename
        // (pre-fix rows) has no separator — try the known categories too.
        if (storedPath.Contains('/'))
        {
            return TryDelete(Path.Combine(_storageRoot, storedPath));
        }
        return TryDelete(Path.Combine(_storageRoot, "avatars", storedPath))
            || TryDelete(Path.Combine(_storageRoot, "items", storedPath));
    }

    private static bool TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
        }
        catch (IOException)
        {
            // Deleting a stale file must never fail the request.
        }
        return false;
    }
}

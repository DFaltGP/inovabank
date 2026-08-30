namespace InovaBank.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(string base64Image, string fileName, CancellationToken ct);
    Task DeleteAsync(string filePath, CancellationToken ct);
}

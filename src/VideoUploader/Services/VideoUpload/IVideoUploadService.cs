namespace VideoUploader.Services.VideoUpload;

public interface IVideoUploadService
{
    Task<Guid> UploadVideoAsync(Stream stream, string fileFileName, CancellationToken ct);
}
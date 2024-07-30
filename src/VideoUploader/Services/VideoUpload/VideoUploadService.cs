using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using VideoUploader.Dto;

namespace VideoUploader.Services.VideoUpload;

public class VideoUploadService : IVideoUploadService
{
    private readonly BlobContainerClient _containerClient;
    private readonly QueueClient _queueClient;
    
    public VideoUploadService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobConnectionString");
        _containerClient = new BlobContainerClient(connectionString, "raw-videos");
        _queueClient = new QueueClient(connectionString, "video-processing");
    }
    public async Task<Guid> UploadVideoAsync(Stream stream, string fileName, CancellationToken ct)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: ct);
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: ct);
        
        var videoId = Guid.NewGuid();
        var blobName = $"raw-video-{videoId}.webm";
        
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(stream, cancellationToken: ct);
        
        
        await _queueClient.SendMessageAsync(new BinaryData(JsonConvert.SerializeObject(new VideoMessage
        {
            VideoId = videoId,
            BlobName = blobName,
            FileName = fileName
            
        })), cancellationToken: ct);
        return videoId;
    }
}
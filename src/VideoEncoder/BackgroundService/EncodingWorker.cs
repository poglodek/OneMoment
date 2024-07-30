using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Newtonsoft.Json;
using VideoEncoder.Dto;

namespace VideoEncoder.BackgroundService;

public class EncodingWorker : Microsoft.Extensions.Hosting.BackgroundService 
{
    private readonly QueueClient _queueClient;
    private readonly QueueClient _queueClientProcessed;
    private readonly BlobContainerClient _containerClient;
    private readonly BlobContainerClient _containerChunkClient;

    public EncodingWorker(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BlobConnectionString");
        _queueClient = new (connectionString, "video-processing");
        _queueClientProcessed = new (connectionString, "video-completed-queue");
        _containerClient = new (connectionString, "raw-videos");
        _containerChunkClient= new (connectionString, "chunk-videos");
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _queueClient.CreateIfNotExistsAsync(cancellationToken:stoppingToken);
            await _queueClientProcessed.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
            await _containerChunkClient.CreateIfNotExistsAsync(cancellationToken: stoppingToken);
            
            var response = await _queueClient.ReceiveMessageAsync(cancellationToken: stoppingToken);
            try
            {

                if (!response.HasValue)
                {
                    await Task.Delay(3000, stoppingToken);
                }

                var message = response.Value.Body.ToObjectFromJson<VideoMessage>();
                var client = _containerClient.GetBlobClient(message.BlobName);
                
                await using var stream = new MemoryStream();
                await client.DownloadToAsync(stream, cancellationToken: stoppingToken);

                stream.Position = 0;

                const int chunkSize =  1024 * 1024 * 1; // 1MB 
                var buffer = new byte[chunkSize];
                var chunkIndex = 0;
                var chunkUrls = new List<string>();

                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, chunkSize);
                    if (bytesRead == 0)
                        break;

                    var chunkId = $"{message.VideoId}-{chunkIndex:D3}";
                    var chunkBlobClient = _containerChunkClient.GetBlobClient($"{chunkId}.webm");

                    await using var chunkStream = new MemoryStream(buffer, 0, bytesRead);
                    await chunkBlobClient.UploadAsync(chunkStream, true, stoppingToken);

                    chunkUrls.Add(chunkBlobClient.Uri.ToString());
                    chunkIndex++;
                }
                
                
                var completionMessage = new
                {
                    VideoId = message.VideoId,
                    Chunks = chunkUrls
                };
                await _queueClientProcessed.SendMessageAsync(JsonConvert.SerializeObject(completionMessage), stoppingToken);
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                await _queueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt, stoppingToken);

            }
        }
    }
}


namespace VideoEncoder.Dto;

public record VideoMessage
{
    public Guid VideoId { get; init; }
    public string BlobName { get; init; }
    public string FileName { get; init; }
}
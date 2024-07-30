using Microsoft.AspNetCore.Mvc;
using VideoUploader.Services.VideoUpload;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 104857600; // 100 MB limit
});


builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new IgnoreAntiforgeryTokenAttribute());
});

builder.Services.AddSingleton<IVideoUploadService, VideoUploadService>();

var app = builder.Build();



app.MapPost("/api/video/upload", async (IFormFile file, IVideoUploadService videoUploadService, CancellationToken ct = default) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest("No file was uploaded");
    }

    await using var stream = file.OpenReadStream();
    
    var videoId = await videoUploadService.UploadVideoAsync(stream, file.FileName, ct);
 

    return Results.Accepted(value: new {VideoId = videoId});
}) .DisableAntiforgery();

await app.RunAsync();


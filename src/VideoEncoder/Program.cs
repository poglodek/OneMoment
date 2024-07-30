using VideoEncoder.BackgroundService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<EncodingWorker>();

var app = builder.Build();


app.UseHttpsRedirection();



await app.RunAsync();


using Microsoft.Extensions.Hosting.WindowsServices;
using OpenMultiSeat.Service;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService();
builder.Services.AddSingleton<MultiSeatWorker>();
builder.Services.AddHostedService<WindowsServiceWorker>();

IHost host = builder.Build();
await host.RunAsync();

using OpenMultiSeat.Service;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsServiceSupport();
builder.Services.AddSingleton<MultiSeatWorker>();
builder.Services.AddHostedService<WindowsServiceWorker>();

IHost host = builder.Build();
await host.RunAsync();

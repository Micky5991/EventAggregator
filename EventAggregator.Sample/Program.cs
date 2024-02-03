using System.Threading.Tasks;
using Micky5991.EventAggregator.Interfaces;
using Micky5991.EventAggregator.Sample;
using Micky5991.EventAggregator.Sample.Events;
using Micky5991.EventAggregator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var serviceProvider = new ServiceCollection()
    .AddLogging(x => x.AddConsole())
    .AddSingleton<IEventAggregator, EventAggregatorService>()
    .AddSingleton<SampleService>()
    .BuildServiceProvider();

var eventAggregator = serviceProvider.GetRequiredService<IEventAggregator>();
var service = serviceProvider.GetRequiredService<SampleService>();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

service.Initialize();

logger.LogInformation("Starting sample program");

// simple event
eventAggregator.Publish(new UserConnectedEvent("Micky5991"));

// Cancellable event
service.SendMessage("Micky5991", "Hello!");
service.SendMessage("Guest", "Hey, I should not be here!");

// Data changable event
service.PurchaseItem("Micky5991", 1000, "10OFF");

// Event priority
var granted = service.HasPermission("Micky5991", "Moderator");
logger.LogInformation("Access for Micky5991 has been {Result}", granted ? "granted" : "denied");

var guestGranted = service.HasPermission("Guest", "Guest");
logger.LogInformation("Access for Guest has been {Result}", guestGranted ? "granted" : "denied");

await Task.Delay(10000);

# EventAggregator

This library implements a variation of the [Event Aggregator](https://martinfowler.com/eaaDev/EventAggregator.html) pattern to provide a type-safe way for components to communicate without direct dependencies.

### Features

- **Type-safe events**: Subscribe and publish events using strongly-typed classes.
- **Event cancelling**: Prevent further execution of handlers or signal the publisher to abort.
- **Data changing**: Modify event data that is then passed back to the publisher.
- **Subscription priorities**: Control the order in which handlers are executed.
- **Thread targets**: Dispatch handlers in the publisher's thread, a background thread, or the UI/Main thread.
- **Asynchronous handlers**: Support for async handlers (fire-and-forget style).

## NuGet Package

This library is available for .NET 8.0, .NET 9.0, and .NET 10.0 on [NuGet](https://www.nuget.org/packages/Micky5991.EventAggregator).

```powershell
dotnet add package Micky5991.EventAggregator
```

## Getting Started

### Requirements

- .NET 8 / .NET 9 / .NET 10
- [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) 8.0+
- [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/) 8.0+

### Registration

Register the `IEventAggregator` to your `IServiceCollection`:

```cs
services.AddSingleton<IEventAggregator, EventAggregatorService>();
```

## Usage

### 1. Create an Event

All events must implement [`IEvent`](EventAggregator/Interfaces/IEvent.cs). Usually, you should inherit from [`EventBase`](EventAggregator/Elements/EventBase.cs).

```cs
public class UserLoggedInEvent : EventBase
{
    public string Username { get; }

    public UserLoggedInEvent(string username)
    {
        Username = username;
    }
}
```

### 2. Subscribe to an Event

Inject `IEventAggregator` and subscribe to your event.

```cs
// Simple subscription
var subscription = eventAggregator.Subscribe<UserLoggedInEvent>(e => 
{
    Console.WriteLine($"User {e.Username} logged in!");
});

// Subscription with options
eventAggregator.Subscribe<UserLoggedInEvent>(OnUserLoggedIn, options => 
{
    options.EventPriority = EventPriority.High;
    options.ThreadTarget = ThreadTarget.BackgroundThread;
});

// Async handler (fire-and-forget)
eventAggregator.Subscribe<UserLoggedInEvent>(async e => 
{
    await Task.Delay(100);
    Console.WriteLine("Processed async");
});

// Remember to unsubscribe when needed
subscription.Dispose();
// OR
eventAggregator.Unsubscribe(subscription);
```

### 3. Publish an Event

```cs
var eventData = new UserLoggedInEvent("Micky5991");

eventAggregator.Publish(eventData);
```

## Advanced Features

### Subscription Priorities

Use `EventPriority` to control the execution order (from `Lowest` to `Highest`, then `Monitor`).

```cs
options.EventPriority = EventPriority.Highest;
```

### Thread Targets

| Target | Description |
|---|---|
| `PublisherThread` | (Default) Executes in the same thread as `Publish`. Required for `IDataChangingEvent` and `ICancellableEvent`. |
| `BackgroundThread` | Executes in a `ThreadPool` thread. |
| `MainThread` | Executes in the captured `SynchronizationContext`. |

To use `ThreadTarget.MainThread`, you must first set the context (e.g., in a UI application):

```cs
eventAggregator.SetMainThreadSynchronizationContext(SynchronizationContext.Current);
```

### Cancellable Events

Inherit from [`CancellableEventBase`](EventAggregator/Elements/CancellableEventBase.cs) to allow handlers to cancel the event.

```cs
public class FileUploadEvent : CancellableEventBase { ... }

// Subscriber
eventAggregator.Subscribe<FileUploadEvent>(e => 
{
    if (e.FileSize > 100) e.Cancelled = true;
});

// Publisher
var e = eventAggregator.Publish(new FileUploadEvent(file));
if (e.Cancelled) { /* Abort upload */ }
```

### Data Changing Events

Implement [`IDataChangingEvent`](EventAggregator/Interfaces/IDataChangingEvent.cs) for events where handlers are expected to modify data.

```cs
public class PriceCalculationEvent : EventBase, IDataChangingEvent 
{
    public double Price { get; set; }
}
```

## Example

Check the [Sample project](EventAggregator.Sample/) for more detailed examples.

## License

```
MIT License

Copyright (c) 2022-2026 Micky5991

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

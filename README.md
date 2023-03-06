# EventAggregator

This library implements a variation of the eventaggregator pattern. 

### Features

- Event cancelling
- Subscription priorites
- Dispatching in specified threads

## NuGet Package

This library is also available as .NET Standard 2.1 library from [NuGet](https://www.nuget.org/packages/Micky5991.EventAggregator).

```
PM> Install-Package Micky5991.EventAggregator
```

## Getting Started

### Requirements

- .NET Standard 2.1 / .NET 6 / .NET 7 / .NET 8
- [Microsoft.Extensions.DependencyInjection.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/) 6.0+
- [Microsoft.Extensions.Logging.Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/) 6.0+
- [System.Collections.Immutable](https://www.nuget.org/packages/System.Collections.Immutable/) 6.0+

### Registration

In order to use this library, you need to first register the `IEventAggregator` to your `IServiceCollection`.

```cs
return new ServiceCollection()

    .AddSingleton<IEventAggregator, EventAggregatorService>() // Add this line to your registration chain...
    .AddScoped<IEventAggregator, EventAggregatorService>() // ... or this one if you want to scope it

    .BuildServiceProvider();
```

## Create your first event

All have to implement the interface [`Micky5991.EventAggregator.Interfaces.IEvent`](EventAggregator/Interfaces/IEvent.cs). 

### Simple events

To create your first event you just have to inherit from the abstract class [`Micky5991.EventAggregator.Elements.EventBase`](EventAggregator/Elements/EventBase.cs).

### Data changing events

Data changing events implement the interface [`Micky5991.EventAggregator.Interfaces.IDataChangingEvent`](EventAggregator/Interfaces/IDataChangingEvent.cs) and prevent any subscribers to subscribe from other ThreadTargets than PublishersThread.

### Cancellable events

Cancellable events implement [`Micky5991.EventAggregator.Interfaces.ICancellableEvent`](EventAggregator/Interfaces/ICancellableEvent.cs) which enable cancellation for other handlers and to react after the publish by the publisher.

All rules from the [data changing events](#data-changing-events) also apply.

To create cancellable event you just have to inherit from the abstract class [`Micky5991.EventAggregator.Elements.CancellableEventBase`](EventAggregator/Elements/EventBase.cs).

## Example

You can find a sample project in [here](EventAggregator/EventAggregator.Sample/).


## License

```
MIT License

Copyright (c) 2022-2023 Micky5991

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

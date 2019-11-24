# EventAggregator

This library implements a variation of the eventaggregator pattern. 

## NuGet Package

This library is also available as .NET Standard 2.0 library from [NuGet](https://www.nuget.org/packages/Micky5991.EventAggregator).

```
PM> Install-Package Micky5991.EventAggregator
```

## Usage

### Registration

In order to use this library, you need to first register the `IEventAggregator` to your `IServiceCollection`.

```cs
return new ServiceCollection()

    .AddEventAggregator() // Add this line to your registration chain

    .BuildServiceProvider();
```

## License

```
MIT License

Copyright (c) 2019 Micky5991

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
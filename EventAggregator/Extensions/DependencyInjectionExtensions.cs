using Micky5991.EventAggregator.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Micky5991.EventAggregator.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddEventAggregator(this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddSingleton<IEventAggregator, Services.EventAggregatorService>();
        }
    }
}

using FakeOrm.AzureTables.Configurations;
using FakeOrm.AzureTables.Repositories;
using FakeOrm.AzureTables.Repositories.Interface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FakeOrm.AzureTables.DependencyInjection
{
    public static class DependencyInjection
    {
        public static IServiceCollection UseAzureTablesRepository(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ConnectionStrings>(configuration.GetSection("ConnectionStrings"))
                .AddSingleton(s => s.GetService<IOptions<ConnectionStrings>>().Value);

            services.AddTransient(typeof(IAzureTableRepository<>), typeof(AzureTableRepository<>));  

            return services;
        }

        public static IServiceCollection UseAzureTablesRepository(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton(s => new ConnectionStrings() { AzureTableConnection = connectionString }); ;

            services.AddScoped(typeof(IAzureTableRepository<>), typeof(AzureTableRepository<>));

            return services;
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TKSC.Application.Interfaces;
using TKSC.Infrastructure.Services;
using TKSC.Infrastructure.Repositories;
using TKSC.Infrastructure.DbContext;
using TKSC.Infrastructure.Interfaces;
using TKSC.Infrastructure.Configurations;

namespace TKSC.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
<<<<<<< HEAD
        services.AddDbContext<Sep490Context>(options => options.UseSqlServer(
            config.GetConnectionString("Sep490") ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490.")));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
=======
        services.AddDbContext<Sep490Context>(options => options.AddInterceptors(new be_da.Authorization.SportCenterConnectionInterceptor()).UseSqlServer(
            config.GetConnectionString("Sep490") ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490.")));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<be_da.Application.Abstractions.ILateEntryStore,be_da.Infrastructure.Persistence.SqlLateEntryStore>();
>>>>>>> 0d40ca35efaf07c5d4887d6f8febc66c57f5578c
        services.AddScoped<IPaymentGateway, PayOsGateway>();
        services.AddSingleton<IPayOsClientFactory, PayOsClientFactory>();
        return services;
    }
}

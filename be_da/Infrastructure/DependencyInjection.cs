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
        services.AddDbContext<Sep490Context>(options => options.UseSqlServer(
            config.GetConnectionString("Sep490") ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490.")));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentGateway, PayOsGateway>();
        services.AddSingleton<IPayOsClientFactory, PayOsClientFactory>();
        return services;
    }
}

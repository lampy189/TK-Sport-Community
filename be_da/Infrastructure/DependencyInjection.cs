using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TKSC.Application.Interfaces.Services;
using TKSC.Application.Interfaces.Repositories;
using TKSC.Application.Interfaces.External;
using TKSC.Infrastructure.ExternalServices;
using TKSC.Infrastructure.Persistence.Repositories;
using TKSC.Infrastructure.Persistence.Contexts;
using TKSC.Infrastructure.ExternalServices.Interfaces;
using TKSC.Infrastructure.Persistence.Configurations;

namespace TKSC.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<Sep490Context>((sp, options) => options.AddInterceptors(new be_da.Infrastructure.Persistence.SportCenterConnectionInterceptor(sp.GetRequiredService<be_da.Application.Interfaces.External.IDataAccessContext>())).UseSqlServer(config.GetConnectionString("Sep490") ?? throw new InvalidOperationException("Configure ConnectionStrings:Sep490.")));
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<be_da.Application.Interfaces.Repositories.ILateEntryStore, be_da.Infrastructure.Persistence.Repositories.SqlLateEntryStore>();
        services.AddScoped<IPaymentGateway, PayOsGateway>();
        services.AddSingleton<IPayOsClientFactory, PayOsClientFactory>();
        return services;
    }
}

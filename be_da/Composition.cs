using be_da.Application.Interfaces.Services;

namespace be_da;
public static class Composition
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITicketCheckInService>(sp => sp.GetRequiredService<global::TKSC.Application.Services.TicketCheckInService>());
        services.AddScoped<ITicketAftercareService>(sp => sp.GetRequiredService<global::TKSC.Application.Services.TicketAftercareService>());
        services.AddScoped<TKSC.Application.Interfaces.Repositories.ITicketAftercareRepository, TKSC.Infrastructure.Persistence.Repositories.TicketAftercareRepository>();
        services.AddScoped<ITicketSupportService>(sp => sp.GetRequiredService<global::TKSC.Application.Services.TicketSupportService>());
        services.AddScoped<TKSC.Application.Interfaces.Repositories.ITicketSupportRepository, TKSC.Infrastructure.Persistence.Repositories.TicketSupportRepository>();
        services.AddScoped<ISkillAssessmentService>(sp => sp.GetRequiredService<global::TKSC.Application.Services.SkillAssessmentService>());
        services.AddScoped<TKSC.Application.Interfaces.Repositories.ISkillAssessmentRepository, TKSC.Infrastructure.Persistence.Repositories.SkillAssessmentRepository>();
        services.AddScoped<ISkillReviewService>(sp => sp.GetRequiredService<global::TKSC.Application.Services.SkillReviewService>());
        services.AddScoped<TKSC.Application.Interfaces.Repositories.ISkillReviewRepository, TKSC.Infrastructure.Persistence.Repositories.SkillReviewRepository>();
        services.AddScoped<IGoodsPaymentService>(sp => sp.GetRequiredService<global::TKSC.Application.Services.GoodsPaymentService>());
        services.AddScoped<TKSC.Application.Interfaces.Repositories.IGoodsPaymentRepository, TKSC.Infrastructure.Persistence.Repositories.GoodsPaymentRepository>();
        services.AddScoped<IPlayerChatbot>(sp => sp.GetRequiredService<global::be_da.Application.Services.Chatbot.PlayerChatbot>());
        services.AddScoped<IGoogleSignIn>(sp => sp.GetRequiredService<global::be_da.Application.Services.Accounts.GoogleSignIn>());
        services.AddScoped<IAccountService, be_da.Application.Services.AccountService>();
        services.AddScoped<ILateEntryService, be_da.Application.Services.LateEntryService>();
        services.AddScoped<IProfileService, be_da.Application.Services.ProfileService>();
        services.AddScoped<ISessionService, be_da.Application.Services.SessionService>();
        services.AddScoped<ISessionSupplyService, be_da.Application.Services.SessionSupplyService>();
        services.AddScoped<ISkillProfileService, be_da.Application.Services.SkillProfileService>();
        services.AddScoped<IStockService, be_da.Application.Services.StockService>();
        services.AddScoped<INotificationService, TKSC.Application.Services.NotificationService>();
        services.AddScoped<TKSC.Application.Interfaces.Repositories.INotificationRepository, TKSC.Infrastructure.Persistence.Repositories.NotificationRepository>();
        services.AddScoped<IStoreService, TKSC.Application.Services.StoreService>();
        services.AddScoped<TKSC.Application.Interfaces.Repositories.IStoreRepository, TKSC.Infrastructure.Persistence.Repositories.StoreRepository>();
        services.AddScoped<IPlayerSkillResultService, TKSC.Application.Services.PlayerSkillResultService>();
        services.AddScoped<TKSC.Application.Interfaces.Repositories.IPlayerSkillResultRepository, TKSC.Infrastructure.Persistence.Repositories.PlayerSkillResultRepository>();
        services.AddScoped<IRevenueService, be_da.Application.Services.RevenueService>();
        services.AddScoped<be_da.Application.Interfaces.Repositories.IRevenueRepository, be_da.Infrastructure.Persistence.Repositories.RevenueRepository>();
        services.AddScoped<ISportCenterService, be_da.Application.Services.SportCenterService>();
        services.AddScoped<IInvitationService, be_da.Application.Services.InvitationService>();
        services.AddScoped<IOperationsService, be_da.Application.Services.OperationsService>();
        services.AddScoped<be_da.Application.Interfaces.Repositories.IOperationsRepository, be_da.Infrastructure.Persistence.Repositories.OperationsRepository>();
        services.AddScoped<be_da.Application.Interfaces.Repositories.IInvitationRepository, be_da.Infrastructure.Persistence.Repositories.InvitationRepository>();
        services.AddScoped<be_da.Application.Interfaces.Repositories.ISportCenterRepository, be_da.Infrastructure.Persistence.Repositories.SportCenterRepository>();
        services.AddScoped<be_da.Application.Interfaces.External.IAvatarStorage, be_da.Infrastructure.ExternalServices.LocalAvatarStorage>();
        services.AddScoped<ICommunityService, be_da.Application.Services.CommunityService>();
        services.AddScoped<ITicketEmailService, TKSC.Application.Services.TicketEmailService>();
        services.AddScoped<TKSC.Application.Interfaces.Repositories.ITicketEmailRepository, TKSC.Infrastructure.Persistence.Repositories.TicketEmailRepository>();
        services.AddScoped<TKSC.Application.Interfaces.External.ITicketEmailSender, TKSC.Infrastructure.ExternalServices.SmtpTicketEmailSender>();
        services.AddSingleton<TKSC.Application.Interfaces.Repositories.IPersistenceFailureClassifier, TKSC.Infrastructure.Persistence.PersistenceFailureClassifier>();
        return services;
    }
}

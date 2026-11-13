using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealTimeApps.Applications.Commons.Options;
using RealTimeApps.Applications.Ports;
using RealTimeApps.Domains.Interfaces;
using RealTimeApps.Infrastructures.Identity;
using RealTimeApps.Infrastructures.Persistence;
using RealTimeApps.Infrastructures.Repositories;
using RealTimeApps.Infrastructures.Services;
using RealTimeApps.Models;

namespace RealTimeApps.Infrastructures;

public static class DependentyInjection
{

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration
    )
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName)
        );

        services.AddDbContext<AppDbContext>(options => 
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .UseSnakeCaseNamingConvention());

        services.AddIdentityCore<PersonalUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false; 
            options.User.RequireUniqueEmail = true; 
            options.Password.RequiredLength = 8;    
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IServerRepository, ServerRepository>(); 
        services.AddScoped<IChannelRepository, ChannelRepository>(); 
        services.AddScoped<IMessageRepository, MessageRepository>(); 
        services.AddScoped<IMessageReactionRepository, MessageReactionRepository>();
        services.AddScoped<IServerMemberRepository, ServerMemberRepository>(); 
        services.AddScoped<IUnitOfWork, UnitOfWorks>();

        services.AddScoped<IIdentityService, IdentityService>(); 
        services.AddScoped<ITokenService, TokenService>(); 
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddSingleton<IInviteCodeGenerator, InviteCodeGenerator>();

        return services;
    }
}

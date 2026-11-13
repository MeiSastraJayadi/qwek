using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Qwek.Applications.Commons.Options;
using Qwek.Applications.Ports;
using Qwek.Domains.Interfaces;
using Qwek.Infrastructures.Identity;
using Qwek.Infrastructures.Persistence;
using Qwek.Infrastructures.Repositories;
using Qwek.Infrastructures.Security;
using Qwek.Infrastructures.Services;
using Qwek.Infrastructures.Storage;
using Qwek.Models;

namespace Qwek.Infrastructures;

public static class DependentyInjection
{

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration
    )
    {
        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName)
        );

        services.Configure<StorageOptions>(
            configuration.GetSection(StorageOptions.SectionName)
        );

        services.AddSingleton<IFileStorage>(provider =>
        {
            IOptions<StorageOptions> options = provider
                .GetRequiredService<IOptions<StorageOptions>>();
            string storageProvider = options.Value.Provider;

            if (storageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
            {
                return new LocalFileStorage(
                    options,
                    provider.GetRequiredService<IHostEnvironment>());
            }

            if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            {
                return new S3FileStorage(options);
            }

            throw new InvalidOperationException(
                $"Unsupported storage provider '{storageProvider}'.");
        });

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
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IStorageQuota, StorageQuota>();
        services.Configure<AntivirusOptions>(
            configuration.GetSection(AntivirusOptions.SectionName)
        );
        services.AddSingleton<IFileScanner, ClamAvFileScanner>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddSingleton<IInviteCodeGenerator, InviteCodeGenerator>();

        return services;
    }
}

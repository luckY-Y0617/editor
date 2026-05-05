using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Northstar.Application.Common;
using Northstar.Application.Files;
using Northstar.Application.Knowledge;
using Northstar.Application.Security;
using Northstar.Application.Workspaces;
using Northstar.Infrastructure.Files;
using Northstar.Infrastructure.Knowledge;
using Northstar.Infrastructure.Persistence;
using Northstar.Infrastructure.Security;
using Northstar.Infrastructure.Workspaces;

namespace Northstar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<MfaOptions>(configuration.GetSection(MfaOptions.SectionName));
        services.Configure<FilesOptions>(configuration.GetSection(FilesOptions.SectionName));
        services.Configure<PermissionPublicShareOptions>(configuration.GetSection(PermissionPublicShareOptions.SectionName));
        services.Configure<EmailInviteDeliveryOptions>(configuration.GetSection(EmailInviteDeliveryOptions.SectionName));
        services.Configure<PermissionExpiryNotificationOptions>(configuration.GetSection(PermissionExpiryNotificationOptions.SectionName));
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<FilesOptions>>().Value);
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<AuthOptions>>().Value);
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<MfaOptions>>().Value);
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<PermissionPublicShareOptions>>().Value);
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<EmailInviteDeliveryOptions>>().Value);

        services.AddDbContext<NorthstarDbContext>((serviceProvider, options) =>
        {
            var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName)
                ?? databaseOptions.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"PostgreSQL connection string is missing. Configure ConnectionStrings:{DatabaseOptions.ConnectionStringName} or {DatabaseOptions.SectionName}:ConnectionString.");
            }

            options.UseNpgsql(
                connectionString,
                npgsql =>
                {
                    npgsql.MigrationsAssembly(typeof(NorthstarDbContext).Assembly.FullName);
                    npgsql.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
                });
        });

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<NorthstarDbContext>());
        services.AddScoped<ITransactionRunner, EfTransactionRunner>();
        services.AddScoped<INorthstarDataSeeder, NorthstarDataSeeder>();
        services.AddScoped<IKnowledgeQueryService, EfKnowledgeQueryService>();
        services.AddScoped<IDocumentRepository, EfDocumentRepository>();
        services.AddScoped<ICommentRepository, EfCommentRepository>();
        services.AddScoped<IDocumentDerivedDataWriter, EfDocumentDerivedDataWriter>();
        services.AddScoped<IDocumentContextQueryService, EfDocumentContextQueryService>();
        services.AddScoped<IDocumentActivityQueryService, EfDocumentActivityQueryService>();
        services.AddScoped<ISearchQueryService, EfSearchQueryService>();
        services.AddScoped<ISpaceTransferRepository, EfSpaceTransferRepository>();
        services.AddScoped<IAuthRepository, EfAuthRepository>();
        services.AddSingleton<IIdpLoginPolicy, ConfiguredIdpLoginPolicy>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IMfaSecretProtector, AesGcmMfaSecretProtector>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IWorkspaceMembershipQueryService, EfWorkspaceMembershipQueryService>();
        services.AddScoped<IResourceWorkspaceResolver, EfResourceWorkspaceResolver>();
        services.AddScoped<IResourcePermissionRepository, EfResourcePermissionRepository>();
        services.AddScoped<IWorkspaceGroupRepository, EfWorkspaceGroupRepository>();
        services.AddScoped<IPermissionAuditRepository, EfPermissionAuditRepository>();
        services.AddScoped<IAccessRequestRepository, EfAccessRequestRepository>();
        services.AddScoped<IPermissionNotificationRepository, EfPermissionNotificationRepository>();
        services.AddScoped<IPermissionNotificationPreferenceRepository, EfPermissionNotificationPreferenceRepository>();
        services.AddScoped<IShareLinkRepository, EfShareLinkRepository>();
        services.AddScoped<IPublicShareCollectionQueryService, EfPublicShareCollectionQueryService>();
        services.AddScoped<IEmailInviteRepository, EfEmailInviteRepository>();
        services.AddScoped<IEmailInviteDeliveryOutboxRepository, EfEmailInviteDeliveryOutboxRepository>();
        services.AddScoped<IScimTokenRepository, EfScimTokenRepository>();
        services.AddScoped<IScimProvisioningRepository, EfScimProvisioningRepository>();
        services.AddScoped<NoopEmailInviteDeliveryService>();
        services.AddScoped<SmtpEmailInviteDeliveryService>();
        services.AddScoped<IEmailInviteDeliveryService>(provider =>
        {
            var options = provider.GetRequiredService<EmailInviteDeliveryOptions>();
            return string.Equals(options.Provider?.Trim(), "smtp", StringComparison.OrdinalIgnoreCase)
                ? provider.GetRequiredService<SmtpEmailInviteDeliveryService>()
                : provider.GetRequiredService<NoopEmailInviteDeliveryService>();
        });
        services.AddScoped<IPermissionUserRepository, EfPermissionUserRepository>();
        services.AddScoped<PermissionExpiryNotificationProcessor>();
        services.AddScoped<IWorkspaceMemberRepository, EfWorkspaceMemberRepository>();
        services.AddScoped<IIamSyncRepository, EfIamSyncRepository>();
        services.AddScoped<IFileRepository, EfFileRepository>();
        services.AddScoped<IObjectStorage, LocalFileStorage>();
        services.AddHostedService<PermissionExpiryNotificationHostedService>();

        return services;
    }
}

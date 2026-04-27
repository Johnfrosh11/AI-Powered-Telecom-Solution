using Azure.Messaging.ServiceBus;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using NaijaShield.Application.Common.Interfaces;
using NaijaShield.Infrastructure.AI;
using NaijaShield.Infrastructure.Integrations;
using NaijaShield.Infrastructure.Persistence;
using NaijaShield.Infrastructure.Persistence.Repositories;
using NaijaShield.Infrastructure.Services;

namespace NaijaShield.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // EF Core
        services.AddDbContext<AppDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IScamCallRepository, ScamCallRepository>();
        services.AddScoped<IScamPatternRepository, ScamPatternRepository>();
        services.AddScoped<IWatchlistedNumberRepository, WatchlistedNumberRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IRegulatoryReportRepository, RegulatoryReportRepository>();
        services.AddScoped<IPromptTemplateRepository, PromptTemplateRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // App Services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuditLogger, AuditLoggerService>();
        services.AddSingleton<IPermissionCache, PermissionCacheService>();
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();

        // Redis Cache
        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = config.GetConnectionString("Redis"));

        // Semantic Kernel + Azure OpenAI
        services.AddSingleton(sp =>
        {
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: config["AzureOpenAI:DeploymentName"] ?? "gpt-4o",
                endpoint: config["AzureOpenAI:Endpoint"] ?? throw new InvalidOperationException("AzureOpenAI endpoint required"),
                apiKey: config["AzureOpenAI:ApiKey"] ?? throw new InvalidOperationException("AzureOpenAI key required"));
            return builder.Build();
        });

        services.AddScoped<IScamDetectionAiService, SemanticKernelScamDetectionService>();

        // Azure Storage
        services.AddSingleton(sp =>
            new BlobServiceClient(config.GetConnectionString("AzureStorage")));
        services.AddScoped<IAzureBlobStorage, AzureBlobStorageService>();

        // Azure Service Bus
        services.AddSingleton(sp =>
            new ServiceBusClient(config.GetConnectionString("ServiceBus")));
        services.AddScoped<IAzureServiceBus, AzureServiceBusService>();

        // HTTP Clients for integrations
        services.AddHttpClient("AfricasTalking");
        services.AddHttpClient("WhatsApp");
        services.AddHttpClient("NCC");
        services.AddScoped<ISmsGateway, AfricasTalkingGateway>();
        services.AddScoped<IWhatsAppGateway, WhatsAppBusinessGateway>();
        services.AddScoped<INccReportingClient, NccReportingClient>();
        services.AddScoped<ICbnAntiFraudClient, CbnAntiFraudClient>();
        services.AddScoped<IEfccReportingClient, EfccReportingClient>();
        services.AddScoped<INimcNinClient, NimcNinClient>();

        return services;
    }
}

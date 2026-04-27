using AutoMapper;
using NaijaShield.Application.Features.AIStudio;
using NaijaShield.Application.Features.Audit;
using NaijaShield.Application.Features.Auth;
using NaijaShield.Application.Features.Conversations;
using NaijaShield.Application.Features.Fraud;
using NaijaShield.Application.Features.Integrations;
using NaijaShield.Application.Features.Reports;
using NaijaShield.Application.Features.Settings;
using NaijaShield.Application.Features.Users;
using NaijaShield.Domain.Aggregates.AIStudio;
using NaijaShield.Domain.Aggregates.Audit;
using NaijaShield.Domain.Aggregates.Conversations;
using NaijaShield.Domain.Aggregates.Identity;
using NaijaShield.Domain.Aggregates.Reports;
using NaijaShield.Domain.Aggregates.ScamDetection;
using NaijaShield.Domain.Aggregates.Tenants;

namespace NaijaShield.Application.Common.Mappings;

/// <summary>AutoMapper profiles for all entity DTOs mappings.</summary>
public class MappingProfiles : Profile
{
    public MappingProfiles()
    {
        CreateMap<AppUser, UserListItemDto>()
            .ForCtorParam("roles",
                opt => opt.MapFrom(s => (IReadOnlyList<string>)s.UserRoles.Select(ur => ur.RoleId.ToString()).ToList()));

        CreateMap<AppUser, UserProfileDto>()
            .ForCtorParam("roles",
                opt => opt.MapFrom(s => (IReadOnlyList<string>)s.UserRoles.Select(ur => ur.RoleId.ToString()).ToList()))
            .ForCtorParam("permissions",
                opt => opt.MapFrom(_ => (IReadOnlyList<string>)new List<string>()));

        CreateMap<ScamCall, ScamCallDto>()
            .ForCtorParam("status", opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<ScamPattern, ScamPatternDto>()
            .ForCtorParam("severity", opt => opt.MapFrom(s => s.Severity.ToString()));

        CreateMap<WatchlistedNumber, WatchlistedNumberDto>()
            .ForCtorParam("status", opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<Conversation, ConversationDto>()
            .ForCtorParam("customerMsisdn", opt => opt.MapFrom(_ => string.Empty))
            .ForCtorParam("channel", opt => opt.MapFrom(s => s.CurrentChannel))
            .ForCtorParam("status", opt => opt.MapFrom(s => s.Status.ToString()))
            .ForCtorParam("language", opt => opt.MapFrom(s => s.DetectedLanguage))
            .ForCtorParam("assignedAgentId", opt => opt.MapFrom(s => s.AssignedAgentId.HasValue ? s.AssignedAgentId.ToString() : null))
            .ForCtorParam("summary", opt => opt.MapFrom(s => s.AiSummary))
            .ForCtorParam("lastMessageAt", opt => opt.MapFrom(s => s.StartedAt))
            .ForCtorParam("messageCount", opt => opt.MapFrom(s => s.Messages.Count));

        CreateMap<Message, MessageDto>()
            .ForCtorParam("content", opt => opt.MapFrom(s => s.ContentOriginal))
            .ForCtorParam("contentEn", opt => opt.MapFrom(s => s.ContentEnglish))
            .ForCtorParam("messageType", opt => opt.MapFrom(s => s.Type.ToString()))
            .ForCtorParam("isFromCustomer", opt => opt.MapFrom(s => s.Direction == "inbound"))
            .ForCtorParam("isTranslated", opt => opt.MapFrom(s => s.Language != "en"));

        CreateMap<RegulatoryReport, ReportDto>()
            .ForCtorParam("type", opt => opt.MapFrom(s => s.Type.ToString()))
            .ForCtorParam("status", opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<PromptTemplate, PromptTemplateDto>();

        CreateMap<AuditLog, AuditLogDto>()
            .ForCtorParam("result", opt => opt.MapFrom(s => s.Result.ToString()))
            .ForCtorParam("sensitivity", opt => opt.MapFrom(s => s.Sensitivity.ToString()));

        CreateMap<Tenant, TenantSettingsDto>()
            .ForCtorParam("plan", opt => opt.MapFrom(s => s.Plan.ToString()))
            .ForCtorParam("status", opt => opt.MapFrom(s => s.Status.ToString()));

        CreateMap<Integration, IntegrationDto>()
            .ForCtorParam("name", opt => opt.MapFrom(s => s.Provider))
            .ForCtorParam("category", opt => opt.MapFrom(s => s.Category.ToString()))
            .ForCtorParam("isActive", opt => opt.MapFrom(s => s.IsConnected))
            .ForCtorParam("status", opt => opt.MapFrom(s => s.IsConnected ? "Connected" : "Disconnected"))
            .ForCtorParam("syncFrequencyMinutes", opt => opt.MapFrom(_ => 60));
    }
}

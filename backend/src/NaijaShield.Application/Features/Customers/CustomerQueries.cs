using MediatR;
using NaijaShield.Application.Common;
using NaijaShield.Application.Common.Interfaces;

namespace NaijaShield.Application.Features.Customers;

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record CustomerDto(
    Guid Id, string MaskedMsisdn, string? Region, string Status,
    int TotalInteractions, int FraudInteractions, decimal FraudRiskScore,
    DateTime FirstSeen, DateTime? LastContactedAt, decimal LifetimeValueKobo, Guid TenantId);

public record CustomerTimelineEvent(
    Guid Id, string EventType, string Description, DateTime OccurredAt, string Channel);

// ── Search Customers ──────────────────────────────────────────────────────────

public record SearchCustomersQuery(
    Guid TenantId, string? Region, string? Status, int Page = 1, int PageSize = 50)
    : IRequest<Result<PagedResult<CustomerDto>>>;

public class SearchCustomersQueryHandler(ICustomerRepository customers)
    : IRequestHandler<SearchCustomersQuery, Result<PagedResult<CustomerDto>>>
{
    public async Task<Result<PagedResult<CustomerDto>>> Handle(SearchCustomersQuery q, CancellationToken ct)
    {
        var items = await customers.ListAsync(q.TenantId, q.Region, q.Page, q.PageSize, ct);
        var total = await customers.CountAsync(q.TenantId, q.Status, ct);

        var dtos = items.Select(c => new CustomerDto(
            c.Id, c.MaskedMsisdn, c.Region, c.Status.ToString(),
            c.TotalInteractions, c.FraudInteractions, c.FraudRiskScore,
            c.FirstSeen, c.LastContactedAt, c.LifetimeValueKobo, c.TenantId)).ToList();

        return Result.Success(new PagedResult<CustomerDto>(dtos, total, q.Page, q.PageSize));
    }
}

// ── Get Customer ──────────────────────────────────────────────────────────────

public record GetCustomerQuery(Guid CustomerId, Guid TenantId)
    : IRequest<Result<CustomerDto>>;

public class GetCustomerQueryHandler(ICustomerRepository customers)
    : IRequestHandler<GetCustomerQuery, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(GetCustomerQuery q, CancellationToken ct)
    {
        var c = await customers.GetByIdAsync(q.CustomerId, ct);
        if (c is null || c.TenantId != q.TenantId)
            return Result.Failure<CustomerDto>("Customer not found.");

        return Result.Success(new CustomerDto(
            c.Id, c.MaskedMsisdn, c.Region, c.Status.ToString(),
            c.TotalInteractions, c.FraudInteractions, c.FraudRiskScore,
            c.FirstSeen, c.LastContactedAt, c.LifetimeValueKobo, c.TenantId));
    }
}

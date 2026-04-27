# Security Policy — NaijaShield AI

NaijaShield AI processes sensitive Nigerian telecoms fraud data, PII, and financial intelligence. We take security seriously and appreciate the responsible disclosure of vulnerabilities.

---

## Supported Versions

We actively maintain security patches for the following versions:

| Version | Status |
|---------|--------|
| `main` branch (latest) | ✅ Actively supported |
| Releases < 6 months old | ✅ Security patches only |
| Releases > 6 months old | ❌ End of life — upgrade recommended |

---

## Reporting a Vulnerability

### ⚠️ DO NOT open a public GitHub issue for security vulnerabilities.

Public disclosure before a patch is available puts our Nigerian telco customers and their subscribers at risk.

### Responsible Disclosure Process

1. **Email** security findings to: **security@naijashield.ai**
2. Use **PGP encryption** if reporting highly sensitive issues (key available at `https://naijashield.ai/.well-known/pgp-key.asc`)
3. Include in your report:
   - **Summary** of the vulnerability
   - **Steps to reproduce** (proof-of-concept if possible)
   - **Potential impact** (CVSS score estimate if known)
   - **Affected component** (API, infrastructure, auth, etc.)
   - Your **contact details** for follow-up

### What to Expect

| Timeline | Action |
|----------|--------|
| **24 hours** | Acknowledgement of your report |
| **72 hours** | Initial severity assessment and triage |
| **14 days** | Fix committed to a private branch (critical/high) |
| **30 days** | Fix deployed to production + public disclosure |
| **90 days** | Maximum embargo period for any severity |

We follow [coordinated vulnerability disclosure](https://en.wikipedia.org/wiki/Coordinated_vulnerability_disclosure). If you request public credit, we will acknowledge you in the release notes.

---

## Scope

### In Scope

The following systems are in scope for security research:

- `api.naijashield.ai` — REST API
- `api-staging.naijashield.ai` — Staging environment (preferred for testing)
- Authentication and authorisation logic (JWT, role/permission system)
- Multi-tenant data isolation
- The NaijaShield GitHub repository code

### Out of Scope

- Denial-of-service (DoS) attacks
- Social engineering of NaijaShield employees
- Physical attacks
- Issues requiring physical access to infrastructure
- Third-party services (Azure, GitHub, etc.)
- Automated scanning results without a confirmed exploit

---

## Security Mitigations in Place

### Authentication & Authorisation

- JWT Bearer tokens with short expiry (15 minutes access / 7 days refresh)
- Refresh token rotation — single use, hashed at rest
- RBAC with fine-grained permissions (`NaijaShield.Domain.Constants.Permissions`)
- Multi-tenant query filters enforced at the EF Core DbContext level
- MFA support per tenant (configurable)

### Data Protection

- Passwords hashed with **BCrypt** (work factor 12)
- MSISDN fields masked (last 4 digits only) in non-PII contexts
- PII access gated behind `CustomersViewPii` permission
- Data encrypted at rest (Azure SQL TDE + Storage SSE)
- TLS 1.2+ enforced everywhere

### API Security

- Rate limiting (per-IP and per-user) via ASP.NET Core Rate Limiter
- CORS restricted to known frontend origins in production
- Security headers middleware: `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy`, `Strict-Transport-Security`
- All inputs validated via FluentValidation before handler execution
- EF Core parameterised queries only — no raw SQL with user input

### Infrastructure

- Azure Key Vault for all secrets (no secrets in config files or environment variables in prod)
- Managed Identity for service-to-service auth where possible
- Azure Front Door WAF in production (OWASP Core Rule Set 3.2)
- SQL Server firewall: public network access disabled in production
- Container images scanned for CVEs in CI via Trivy (coming soon)
- Audit log for all sensitive operations, immutable (no delete endpoint)

### Supply Chain

- Dependabot enabled for NuGet and GitHub Actions
- All production dependencies pinned to exact versions
- NuGet Central Package Management enforced

---

## Common Vulnerability Classes — NaijaShield Context

Given our domain, please pay special attention to:

| Risk | Area |
|------|------|
| Tenant data leakage | All queries must filter by `TenantId` |
| MSISDN exposure | PII masking in `WatchlistedNumber.MaskedNumber` |
| JWT algorithm confusion | We accept only `HS256` — verify header |
| IDOR on scam call actions | `ConfirmScamCall`, `BlockNumber` must verify ownership |
| Privilege escalation | Role assignment endpoint requires `roles.manage` permission |
| Audit log tampering | Audit entries are append-only — no update/delete |

---

## Patching Timeline

| Severity (CVSS) | Fix SLA |
|-----------------|---------|
| Critical (9.0–10.0) | 24 hours to patch, same-day deployment |
| High (7.0–8.9) | 7 days |
| Medium (4.0–6.9) | 30 days |
| Low (0.1–3.9) | Next scheduled release |

---

## Contact

- **Security email**: security@naijashield.ai
- **General contact**: hello@naijashield.ai
- **Repository**: https://github.com/Johnfrosh11/AI-Powered-Telecom-Solution

# Multi-stage build for NaijaShield API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files first for layer caching
COPY ["src/NaijaShield.Api/NaijaShield.Api.csproj", "src/NaijaShield.Api/"]
COPY ["src/NaijaShield.Application/NaijaShield.Application.csproj", "src/NaijaShield.Application/"]
COPY ["src/NaijaShield.Domain/NaijaShield.Domain.csproj", "src/NaijaShield.Domain/"]
COPY ["src/NaijaShield.Infrastructure/NaijaShield.Infrastructure.csproj", "src/NaijaShield.Infrastructure/"]

RUN dotnet restore "src/NaijaShield.Api/NaijaShield.Api.csproj"

COPY . .
RUN dotnet build "src/NaijaShield.Api/NaijaShield.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/NaijaShield.Api/NaijaShield.Api.csproj" -c Release -o /app/publish \
    --no-restore /p:UseAppHost=false

# Runtime image — non-root user
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN addgroup --system --gid 1001 naijashield && \
    adduser --system --uid 1001 --ingroup naijashield naijashield

COPY --from=publish /app/publish .

# Set non-root user
USER naijashield

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "NaijaShield.Api.dll"]

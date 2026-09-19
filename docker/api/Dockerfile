# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props NafasLand.Admin.sln ./
COPY src/Api/NafasLand.Admin.Api.csproj src/Api/
COPY src/Shared/Kernel/NafasLand.Admin.Shared.Kernel.csproj src/Shared/Kernel/
COPY src/Shared/Infrastructure/NafasLand.Admin.Shared.Infrastructure.csproj src/Shared/Infrastructure/
COPY src/Modules/Sample/NafasLand.Admin.Modules.Sample.csproj src/Modules/Sample/
COPY src/Modules/Identity/NafasLand.Admin.Modules.Identity.csproj src/Modules/Identity/
RUN dotnet restore src/Api/NafasLand.Admin.Api.csproj

COPY src/ src/
RUN dotnet publish src/Api/NafasLand.Admin.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# curl is only needed for the container's healthcheck (ADR-043)
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd --system nafasland \
    && useradd --system --gid nafasland --shell /usr/sbin/nologin nafasland

COPY --from=build /app ./
RUN mkdir -p /app/logs && chown -R nafasland:nafasland /app

USER nafasland
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "NafasLand.Admin.Api.dll"]

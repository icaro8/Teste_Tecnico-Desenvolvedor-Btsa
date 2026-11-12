# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb AS restore
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props .editorconfig Transfers.sln ./
COPY src/Transfers.Domain/Transfers.Domain.csproj src/Transfers.Domain/
COPY src/Transfers.Application/Transfers.Application.csproj src/Transfers.Application/
COPY src/Transfers.Infrastructure/Transfers.Infrastructure.csproj src/Transfers.Infrastructure/
COPY src/Transfers.Api/Transfers.Api.csproj src/Transfers.Api/
COPY tests/Transfers.UnitTests/Transfers.UnitTests.csproj tests/Transfers.UnitTests/
COPY tests/Transfers.IntegrationTests/Transfers.IntegrationTests.csproj tests/Transfers.IntegrationTests/
RUN dotnet restore src/Transfers.Api/Transfers.Api.csproj

FROM restore AS build
COPY src/ src/
RUN dotnet publish src/Transfers.Api/Transfers.Api.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM restore AS test
RUN dotnet restore Transfers.sln
COPY src/ src/
COPY tests/ tests/
RUN dotnet build Transfers.sln -c Release --no-restore
ENTRYPOINT ["dotnet", "test", "Transfers.sln", "-c", "Release", "--no-build", "--logger", "console;verbosity=normal"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_gcServer=0
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=20s --retries=3 \
  CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /health HTTP/1.0\\r\\nHost: localhost\\r\\n\\r\\n' >&3 && grep -q '200 OK' <&3"]
ENTRYPOINT ["dotnet", "Transfers.Api.dll"]

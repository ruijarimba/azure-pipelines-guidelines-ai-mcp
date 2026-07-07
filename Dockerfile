# syntax=docker/dockerfile:1

# ── Stage 1: build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the central version and build props first so layer caching is effective.
COPY Directory.Build.props Directory.Packages.props global.json ./

# Copy only the projects that Mcp.Host needs, preserving the relative paths
# the .csproj ProjectReference elements expect.
COPY src/AzurePipelines.Guidelines.Core/                src/AzurePipelines.Guidelines.Core/
COPY src/AzurePipelines.Guidelines.Parsing/             src/AzurePipelines.Guidelines.Parsing/
COPY src/AzurePipelines.Guidelines.Rules/               src/AzurePipelines.Guidelines.Rules/
COPY src/AzurePipelines.Guidelines.Analysis/            src/AzurePipelines.Guidelines.Analysis/
COPY src/AzurePipelines.Guidelines.Mcp/                 src/AzurePipelines.Guidelines.Mcp/
COPY tools/AzurePipelines.Guidelines.Mcp.Host/          tools/AzurePipelines.Guidelines.Mcp.Host/
COPY tools/Directory.Build.props                        tools/Directory.Build.props

# Restore — separate layer so it is cached unless a .csproj or props file changes.
RUN dotnet restore tools/AzurePipelines.Guidelines.Mcp.Host/AzurePipelines.Guidelines.Mcp.Host.csproj

# Publish a self-contained-trimmed binary is explicitly NOT used here because
# the .NET runtime image is already lean and avoids trimming compatibility risks.
RUN dotnet publish tools/AzurePipelines.Guidelines.Mcp.Host/AzurePipelines.Guidelines.Mcp.Host.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish

# ── Stage 2: final image ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

# Run as a non-root user for security best practice.
RUN addgroup --system --gid 1001 mcpgroup \
 && adduser  --system --uid 1001 --ingroup mcpgroup --no-create-home mcpuser

WORKDIR /app
COPY --from=build /app/publish .

USER mcpuser

# MCP servers communicate over stdin/stdout; there is no HTTP port to expose.
# The container must be started with -i (keep stdin open), e.g.:
#   docker run -i ruijarimba/azure-pipelines-guidelines-mcp:latest
ENTRYPOINT ["dotnet", "AzurePipelines.Guidelines.Mcp.Host.dll"]

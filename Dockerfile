# syntax=docker/dockerfile:1

# Build the MCP host image for containerized HTTP transport. The default runtime
# is Streamable HTTP on port 8080 so the container is usable without extra flags.
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
# the ASP.NET runtime image supplies the required shared framework and avoids trimming risks.
RUN dotnet publish tools/AzurePipelines.Guidelines.Mcp.Host/AzurePipelines.Guidelines.Mcp.Host.csproj \
    --no-restore \
    --configuration Release \
    --output /app/publish

# ── Stage 2: final image ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# Run as a non-root user for security best practice.
RUN groupadd --system --gid 1001 mcpgroup \
 && useradd --system --uid 1001 --gid mcpgroup --no-create-home mcpuser

WORKDIR /app
COPY --from=build /app/publish .

USER mcpuser

# Use Streamable HTTP for independently hosted containers. Set MCP_TRANSPORT=stdio
# and keep stdin open when an MCP client launches the container as a child process.
ENV MCP_TRANSPORT=http \
    ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "AzurePipelines.Guidelines.Mcp.Host.dll"]

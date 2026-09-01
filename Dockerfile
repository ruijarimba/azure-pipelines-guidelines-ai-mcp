# syntax=docker/dockerfile:1

# Build the MCP host image for containerized HTTP transport. The default runtime
# is Streamable HTTP on port 8080 so the container is usable without extra flags.
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution, central version, build props, and test settings first so
# restore remains cacheable until project or package configuration changes.
COPY \
    AzurePipelinesGuidelines.slnx \
    Directory.Build.props \
    Directory.Packages.props \
    coverlet.runsettings \
    global.json \
    ./

# Copy the complete source, test, and tool trees. This keeps the build context
# simple and automatically includes future projects while preserving references.
COPY src/ src/
COPY tests/ tests/
COPY tools/ tools/

# Restore the complete solution because the image build runs the complete test suite.
RUN dotnet restore AzurePipelinesGuidelines.slnx

# Build the complete solution once so the following test and publish steps can reuse the outputs.
RUN dotnet build AzurePipelinesGuidelines.slnx \
    --configuration Release \
    --no-restore \
    -p:RunAnalyzers=false

# Validate the source tree inside the Docker build without compiling it again.
# Test projects and their output remain in this intermediate stage only.
RUN dotnet test AzurePipelinesGuidelines.slnx \
    --configuration Release \
    --no-restore \
    --no-build

# A self-contained, trimmed binary is explicitly not used because
# the ASP.NET runtime image supplies the required shared framework and avoids trimming risks.
RUN dotnet publish tools/AzurePipelines.Guidelines.Mcp.Host/AzurePipelines.Guidelines.Mcp.Host.csproj \
    --configuration Release \
    --no-restore \
    --no-build \
    --output /app/publish

# Final runtime image
# Use aspnet instead of runtime: Mcp.Host references ModelContextProtocol.AspNetCore,
# which requires the Microsoft.AspNetCore.App shared framework at process startup.
# One aspnet image supports both stdio and HTTP. Using runtime would require separately built,
# tested, published, and documented stdio-only and HTTP images.
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

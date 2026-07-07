---
applyTo: "src/**/*.csproj,tools/**/*.csproj,Directory.Build.props,Directory.Packages.props"
---

# NuGet packaging rules

These rules govern how NuGet packages are declared and published in this repository.
They apply whenever you create or modify project files, central version management, or
CI/release workflows.

---

## 1. Central package management

All package versions are declared once in **`Directory.Packages.props`** at the repository
root. Individual `<PackageReference>` elements in `.csproj` files must **not** include a
`Version` attribute — this is enforced by `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`.

```xml
<!-- Directory.Packages.props — correct -->
<PackageVersion Include="YamlDotNet" Version="16.3.0" />

<!-- any .csproj — correct (no Version) -->
<PackageReference Include="YamlDotNet" />
```

---

## 2. Shared metadata — `src/Directory.Build.props`

Common NuGet metadata is declared once in `src/Directory.Build.props` and applies
automatically to all `src/` libraries. Do not repeat these in individual `.csproj` files:
`IsPackable`, `GenerateDocumentationFile`, `Authors`, `PackageProjectUrl`, `RepositoryUrl`,
`RepositoryType`, `PackageLicenseExpression`, `PackageTags`.

`PackageVersion` must **never** be hard-coded. It is injected at release time via
`/p:PackageVersion=x.y.z`.

---

## 3. Required per-project metadata

Each `.csproj` under `src/` must declare exactly:

```xml
<PropertyGroup>
  <PackageId>AzurePipelines.Guidelines.{LayerName}</PackageId>
  <Description>One or two sentences describing what this package provides and who should reference it.</Description>
</PropertyGroup>
```

---

## 4. `tools/` projects — not packable by default

`tools/Directory.Build.props` sets `<IsPackable>false</IsPackable>` for all tool
executables. The **single exception** is the CLI global tool, which must explicitly
override this:

```xml
<!-- tools/AzurePipelines.Guidelines.Cli/AzurePipelines.Guidelines.Cli.csproj -->
<IsPackable>true</IsPackable>
<PackAsTool>true</PackAsTool>
<ToolCommandName>adog</ToolCommandName>
```

Do not set `IsPackable=true` on any other `tools/` project.

---

## 5. Dependency rules for library packages

`src/` libraries must stay composable:

- Prefer `*.Abstractions` packages over full runtime packages:
  - ✅ `Microsoft.Extensions.DependencyInjection.Abstractions`
  - ❌ `Microsoft.Extensions.DependencyInjection` (concrete runtime — belongs in `tools/`)
  - ✅ `Microsoft.Extensions.Hosting.Abstractions`
  - ❌ `Microsoft.Extensions.Hosting` (belongs in `tools/`)
- `AzurePipelines.Guidelines.Core` must have **no** `PackageReference` or `ProjectReference`
  entries — it is the foundation layer.
- Never force consumers to take on concrete runtime dependencies they did not request.
- Declare only **direct** dependencies; never re-declare transitive packages.

---

## 6. SemVer and breaking changes

Follow [Semantic Versioning 2.0](https://semver.org/) strictly:

- Breaking public-API change → **major** bump.
- New additive public API → **minor** bump.
- Bug fix, no API change → **patch** bump.
- Pre-release suffixes (`-preview.1`, `-beta.1`) are allowed for unstable builds.

---

## 7. Packing and publishing workflow

### Local verification

```powershell
# Pack a src/ library
dotnet pack src/AzurePipelines.Guidelines.Core/AzurePipelines.Guidelines.Core.csproj `
  --configuration Release /p:PackageVersion=0.0.0-local --output nupkgs

# Pack the CLI global tool
dotnet pack tools/AzurePipelines.Guidelines.Cli/AzurePipelines.Guidelines.Cli.csproj `
  --configuration Release /p:PackageVersion=0.0.0-local --output nupkgs
```

### CI (every push / pull request)

The `verify-pack` job in `.github/workflows/ci.yml` builds the solution and packs all
packages with version `0.0.0-ci` to verify the pack step always succeeds. It does not
publish to NuGet.org.

### Release (manual gate, on version tag)

`.github/workflows/release.yml` triggers on a tag matching `v[0-9]+.[0-9]+.[0-9]+*`.
It requires a **`release` environment** with a human reviewer and a `NUGET_API_KEY` secret.

To cut a release:
1. Merge all intended changes to the target branch.
2. Push a version tag: `git tag v1.0.0 && git push origin v1.0.0`.
3. Approve the environment gate in GitHub Actions.
4. Verify packages appear on NuGet.org.

---

## 8. Public API surface

- Minimize `public` types; use `internal` for all implementation details.
- Every `public` and `protected` member must carry an XML doc comment.
- Avoid `public` sealed class hierarchies consumers cannot extend — prefer interfaces.

---

## 9. What is never packaged

- Test projects (`tests/Directory.Build.props` sets `<IsPackable>false</IsPackable>`).
- `tools/AzurePipelines.Guidelines.Mcp.Host` (the MCP stdio host executable).
- Any file containing secrets, API keys, or environment-specific configuration.

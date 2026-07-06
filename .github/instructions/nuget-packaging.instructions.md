---
applyTo: "src/**/*.csproj"
---

# NuGet packaging rules

## All `src/` projects are NuGet packages

`src/Directory.Build.props` sets `<IsPackable>true</IsPackable>` for the entire `src/` tree.
Every project here will be published as an independent NuGet package.

## Required per-project metadata

Each `.csproj` under `src/` must set:

```xml
<PackageId>AzurePipelines.Guidelines.{Name}</PackageId>
<Description>One-sentence description of what the package provides.</Description>
```

Do not set `<PackageVersion>` in `.csproj` files. Versions are injected at CI time
via `/p:PackageVersion=x.y.z`.

## Versioning

- Follow **SemVer 2.0** strictly.
- A breaking public-API change requires a **major** version bump.
- New public API additions require a **minor** version bump.
- Bug fixes with no API changes require a **patch** version bump.

## Transitive dependency hygiene

- Declare only **direct** dependencies in `<PackageReference>` — never re-declare a package
  that is already a transitive dependency.
- Use `*.Abstractions` variants in `src/` libraries instead of the full runtime packages:
  - ✅ `Microsoft.Extensions.DependencyInjection.Abstractions`
  - ❌ `Microsoft.Extensions.DependencyInjection` (concrete, belongs in `tools/`)
  - ✅ `Microsoft.Extensions.Hosting.Abstractions`
  - ❌ `Microsoft.Extensions.Hosting` (belongs in `tools/`)
- Do not include `Version` attributes in `<PackageReference>` elements — all versions are
  centrally managed in the root `Directory.Packages.props`.

## Public API surface

- Minimize `public` types; use `internal` for all implementation details.
- Every `public` and `protected` member must have an XML doc comment.
- Avoid `public` sealed class hierarchies that consumers cannot extend — use interfaces.

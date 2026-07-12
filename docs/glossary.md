# Glossary

**Single source of truth** for domain vocabulary. Other files link here; do not duplicate definitions.

---

## Core domain

| Term | Definition |
| --- | --- |
| **Guideline** | A single recommendation from the [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines). Each guideline maps to a stable `GuidelineId`. |
| **GuidelineId** | Stable unique identifier: `ADOG-{CATEGORY}-{NNN}` (for example, `ADOG-STEPS-001`). Never reused after a guideline is renamed or removed. |
| **GuidelineCategory** | One of: `General`, `Jobs`, `Parameters`, `Pipelines`, `Stages`, `Steps`, `Variables`. |
| **GuidelineSeverity** | Indicates how strongly a recommendation should be followed: `Do`, `DoNot`, `Avoid`, `Consider`. |
| **DetectionHint** | Machine-readable guidance for detecting a violation. Includes `kind`, `pattern`, `appliesTo`, and `message`. |
| **DetectionKind** | Type of detection hint: `Regex` (match raw YAML text), `YamlPath` (parsed YAML path or key condition), or `Heuristic` (LLM or custom logic). |
| **Diagnostic** | A violation found in a pipeline file. References a `GuidelineId`, includes severity, message, and location (line and column). |
| **FixGuidance** | Remediation instructions for a detected violation. Includes `summary`, `autofixable` flag, ordered `steps`, and `exampleRef`. |
| **PipelineDocument** | Parsed AST representation of an Azure Pipelines YAML file. Root of the domain model for pipeline structure. |

---

## Severity → Diagnostic level mapping

| `GuidelineSeverity` | `DiagnosticSeverity` | Meaning |
| --- | --- | --- |
| `Do` | `Error` | You should almost always follow this. |
| `DoNot` | `Error` | You should almost never do this. |
| `Avoid` | `Warning` | Generally not a good idea, but there are exceptions. |
| `Consider` | `Info` | Should generally be followed, but legitimate exceptions exist. |

This mapping is used by the analysis engine to translate manifest severities into CLI/MCP diagnostic levels.

### Two notations for the same severity

The companion manifest (`guidelines.json`) uses lowercase, hyphenated strings.
The C# `GuidelineSeverity` enum uses PascalCase. They mean the same thing:

| Manifest form | C# enum form |
| --- | --- |
| `do` | `Do` |
| `do-not` | `DoNot` |
| `avoid` | `Avoid` |
| `consider` | `Consider` |

When you see `do-not` in a manifest or a prompt file, and `DoNot` in C# code or this glossary,
they are the same value. Parsing code maps between the two forms.

---

## Analysis pipeline

| Term | Definition |
| --- | --- |
| **IGuidelineRule** | Interface for a single guideline rule. Takes a `PipelineDocument`, returns an async stream of `Diagnostic` instances. |
| **IGuidelineRepository** | Loads `GuidelineDefinition` records from the manifest (`data/guidelines.json`). |
| **IPipelineParser** | Parses YAML text into a `PipelineDocument`. Implemented in `Parsing` via YamlDotNet. |
| **IAnalysisEngine** | Orchestrates the full pipeline: parse → filter rules → run rules → aggregate `AnalysisResult`. |
| **AnalysisResult** | Output of the analysis engine: list of `Diagnostic`, summary statistics, elapsed time. |
| **AnalysisOptions** | Filtering parameters: category, minimum severity, `appliesTo` scopes. Passed to `IAnalysisEngine`. |

---

## MCP (Model Context Protocol)

| Term | Definition |
| --- | --- |
| **MCP server** | A process that exposes capabilities (tools and resources) to AI assistants via the Model Context Protocol standard. |
| **MCP tool** | A callable function exposed by the server. For this project: guideline lookup, YAML analysis, fix suggestions. |
| **MCP resource** | A queryable data source. For this project: guideline catalogue, per-rule metadata. |

---

## CLI (Command Line Interface)

| Term | Definition |
| --- | --- |
| **adog** | The command-line tool name (short for "Azure DevOps Guidelines"). Published as a .NET global tool (`dotnet tool install -g adog`). |
| **Exit code** | `0` = no violations at threshold; `1` = violations found; `2` = analysis error (invalid YAML or file not found). |

---

## Packaging & distribution

| Term | Definition |
| --- | --- |
| **NuGet package** | All `src/` projects are published as independent packages to NuGet.org under the `AzurePipelines.Guidelines.*` namespace. |
| **.NET global tool** | The CLI (`adog`) is published as a tool package; installed via `dotnet tool install -g adog`. |
| **SemVer** | Semantic Versioning 2.0. Breaking changes require a major version bump. |

---

## Naming conventions

| Element | Convention | Example |
| --- | --- | --- |
| Types, methods, properties | `PascalCase` | `GuidelineDefinition`, `AnalyzeAsync` |
| Private fields | `_camelCase` | `_analysisEngine` |
| Parameters, locals | `camelCase` | `guidelineId`, `pipelineDocument` |
| Async methods | Suffix `Async` | `AnalyzeAsync`, `GetByIdAsync` |
| Interfaces | Prefix `I` + noun/verb phrase | `IGuidelineRule`, `IGuidelineRepository` |
| Rule classes | `{Behaviour}Rule` | `AbsoluteTemplatePathRule` |
| Test methods | `Method_GivenContext_ShouldOutcome` | `Analyze_GivenEmptyDocument_ShouldReturnNoDiagnostics` |

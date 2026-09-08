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
| **Guideline recommendation label** | User-facing label used by MCP prompts: `DO`, `DO-NOT`, `AVOID`, or `CONSIDER`. |
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

This mapping is used internally by the analysis engine to translate manifest severities into diagnostic levels. MCP tool output uses the recommendation labels (`do`, `donot`, `avoid`, `consider`) for all user-facing payloads.

### Two notations for the same severity

The companion manifest (`guidelines.json`) uses lowercase, hyphenated strings. The C# `GuidelineSeverity` enum uses PascalCase. They mean the same thing:

| Manifest form | C# enum form |
| --- | --- |
| `do` | `Do` |
| `do-not` | `DoNot` |
| `avoid` | `Avoid` |
| `consider` | `Consider` |

When you see `do-not` in a manifest or a prompt file, and `DoNot` in C# code or this glossary, they are the same value. Parsing code maps between the two forms.

---

## Internal C# APIs

| Term | Definition |
| --- | --- |
| **IGuidelineRule** | Interface for a single guideline rule. Takes a `PipelineDocument`, returns an async stream of `Diagnostic` instances. |
| **IGuidelineRepository** | Loads `GuidelineDefinition` records from the manifest (`data/guidelines.json`). |
| **IPipelineParser** | Parses YAML text into a `PipelineDocument`. Implemented in `Parsing` via YamlDotNet. |
| **IAnalysisEngine** | Orchestrates the full pipeline: parse → filter rules → run rules → aggregate `AnalysisResult`. |
| **AnalysisResult** | Output of the analysis engine: list of `Diagnostic`, summary statistics, elapsed time. |
| **AnalysisOptions** | Filtering parameters: category, minimum severity, `appliesTo` scopes. Passed to `IAnalysisEngine`. |

---

## Container images and Docker

| Term | Definition |
| --- | --- |
| **Container image** | A packaged application and its runtime dependencies. This project publishes a multi-platform image to Docker Hub. |
| **Docker Buildx** | Docker's build tool for building images with BuildKit, including images for multiple platforms and build attestations. |
| **Docker Scout** | Docker's tool for inspecting container images, including their software bill of materials and vulnerabilities. |
| **SBOM** | Software bill of materials: a machine-readable list of the components in a software artifact, such as a container image. |
| **Attestation** | Signed or verifiable metadata attached to an artifact. This project attaches SBOM and build-provenance attestations to published images. |
| **Build provenance** | Metadata that describes where, how, and from which inputs an artifact was built. |
| **SLSA** | Supply-chain Levels for Software Artifacts, a framework for improving the security and integrity of software build processes. |
| **OCI** | Open Container Initiative, the standard used for container image formats and related registry metadata. |

---

## MCP (Model Context Protocol)

| Term | Definition |
| --- | --- |
| **MCP server** | A process that exposes capabilities (tools and resources) to AI assistants via the Model Context Protocol standard. |
| **MCP tool** | A callable function exposed by the server. For this project: guideline lookup and YAML analysis. |
| **MCP resource** | A queryable data source. For this project: guideline catalogue, per-rule metadata. |
| **MCP prompt** | A reusable instruction template exposed by the server. It guides an AI assistant to use MCP tools and present results without performing analysis itself. |

---

## Packaging and distribution

| Term | Definition |
| --- | --- |
| **NuGet package** | A locally packable artifact. This repository does not publish NuGet packages. |
| **.NET global tool** | A package that provides a command-line executable. This project retains local packaging configuration for `adog-mcp`; it is not published by this repository. |
| **Docker Hub image** | The published container image available as `ruijarimba/azure-pipelines-guidelines-mcp:latest`. |
| **SemVer** | Semantic Versioning 2.0. Breaking changes require a major version bump. |

---


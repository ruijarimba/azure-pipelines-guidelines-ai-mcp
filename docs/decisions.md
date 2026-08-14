# Architecture Decision Records

Use this document to record important technical decisions, why we made them, and what consequences they have. It is not a changelog for every code or documentation change.

When deciding whether to add an ADR, ask:

> Will someone later need to understand why we chose this approach?

When an agent considers changing an existing decision, it must re-read the rationale first. If the context has changed, document the reversal here.

## ADR index

| ADR | Date | Summary |
| --- | --- | --- |
| [ADR-001](#adr-001-target-framework) | 2026-07-06 | Target framework |
| [ADR-002](#adr-002-central-package-management) | 2026-07-06 | Central package management |
| [ADR-003](#adr-003-layered-architecture) | 2026-07-06 | Layered architecture |
| [ADR-004](#adr-004-test-libraries) | 2026-07-06 | Test libraries |
| [ADR-005](#adr-005-solution-file-format) | 2026-07-06 | Solution file format |
| [ADR-006](#adr-006-warnings-as-errors) | 2026-07-06 | Warnings as errors |
| [ADR-007](#adr-007-guidelines-manifest-ownership) | 2026-07-06 | Guidelines manifest ownership |
| [ADR-008](#adr-008-nuget-package-structure) | 2026-07-06 | NuGet package structure |
| [ADR-009](#adr-009-yaml-parser) | 2026-07-06 | YAML parser |
| [ADR-010](#adr-010-agent-behaviour-guardrails) | 2026-07-06 | Agent behaviour guardrails |
| [ADR-011](#adr-011-human-readability) | 2026-07-06 | Human readability |
| [ADR-012](#adr-012-csharp-implementation-patterns) | 2026-07-07 | CSharp implementation patterns |
| [ADR-013](#adr-013-heuristic-detection) | 2026-07-07 | Heuristic detection |
| [ADR-014](#adr-014-debuggability) | 2026-07-08 | Debuggability |
| [ADR-015](#adr-015-mcp-transports) | 2026-07-29 | MCP transports |

---

## ADR-001 Target framework

**Date:** 2026-07-06  
**Context:** Need to choose a .NET version that balances latest language features with stability.  
**Decision:** Target `net10.0` exclusively and use the latest language version supported by the selected .NET SDK; no multi-targeting.  
**Rationale:**

- Latest stable SDK at project start (10.0.301).
- Current C# language features, including primary constructors and collection expressions, reduce boilerplate.
- Single TFM simplifies build/test/package matrix.
- NuGet consumers on older runtimes can use older major versions if needed (SemVer).

**Consequences:**

- Minimum consumer requirement: .NET 10 runtime.
- No need to maintain `#if` preprocessor branches.

---

## ADR-002 Central package management

**Date:** 2026-07-06  
**Context:** 13 projects, shared dependencies — want single source of truth for package versions.  
**Decision:** Use MSBuild central package management (`ManagePackageVersionsCentrally`).  
**Rationale:**

- All `<PackageReference>` elements omit `Version` — declared once in `Directory.Packages.props`.
- Prevents version skew across projects.
- Easier to audit and upgrade dependencies.

**Consequences:**

- All version changes must go through the root `Directory.Packages.props` file.
- No per-project version overrides allowed without explicit justification.

---

## ADR-003 Layered architecture

**Date:** 2026-07-06  
**Context:** Want maintainable, testable code that resists architectural drift.  
**Decision:** Enforce strict dependency graph via project references; Core has zero internal deps.  
**Rationale:**

- `Core` defines all contracts — prevents coupling to parsing/rules/protocols.
- Each layer has a single, well-defined responsibility.
- Dependency inversion: `Analysis` and `Mcp` depend on `Core` interfaces, not concrete classes.

**Consequences:**

- Adding a new project dependency requires reviewing the graph in `docs/architecture.md`.
- Circular references are build errors — enforced by MSBuild.

---

## ADR-004 Test libraries

**Date:** 2026-07-06  
**Context:** Consistency in test style and readability across the codebase.  
**Decision:** Single stack for all tests; no mixing of assertion or mocking libraries.  
**Rationale:**

- xUnit, FluentAssertions, and NSubstitute provide a consistent, readable test stack with strong tooling support.

**Consequences:**

- Test conventions and coverage expectations are defined in [testing.instructions.md](../.github/instructions/testing.instructions.md).

---

## ADR-005 Solution file format

**Date:** 2026-07-06 (revised 2026-07-08)  
**Context:** MSBuild 17.11+ supports `.slnx` (XML solution format), but not all tooling does.  
**Original decision:** Use traditional `.sln` format for broadest toolchain compatibility.  
**Reversal (2026-07-08):** The repository uses `.slnx` in practice. The original compatibility concern no longer applies — Visual Studio 2022 17.11+, Visual Studio 2026, and the .NET CLI all support `.slnx` fully. The XML format is human-readable, produces clean diffs, and is the preferred format for new projects going forward.

**Consequences:**

- The solution file is `AzurePipelinesGuidelines.slnx` (XML format, not the legacy binary-text hybrid).
- New files and projects must be registered in the `.slnx` file following the rules in
  `solution-files.instructions.md`.

---

## ADR-006 Warnings as errors

**Date:** 2026-07-06  
**Context:** Want zero-tolerance for code quality issues.  

**Decision:** Set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in root `Directory.Build.props`.  
**Rationale:**

- Treating warnings as errors prevents quality issues from accumulating unnoticed.

**Consequences:**

- The build configuration remains the source of truth for enforcement and suppression details.

---

## ADR-007 Guidelines manifest ownership

**Date:** 2026-07-06  
**Context:** Need a single source of truth for rule definitions.  
**Decision:** Consume `data/guidelines.json` from the [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines).

**Rationale:**

- Guidelines evolve independently of the tooling.
- Manifest schema is versioned and stable (`schemaVersion` field).
- This repository **implements** rules; it does not **define** them.

**Consequences:**

- Rule IDs (`ADOG-{CATEGORY}-{NNN}`) are assigned in the companion repository.
- Implementation must handle unknown rule IDs gracefully (log warning, skip).
- Breaking manifest schema changes require a coordinated update across both repositories.

---

## ADR-008 NuGet package structure

**Date:** 2026-07-06  
**Context:** Want reusable libraries that consumers can compose as needed.  
**Decision:** Every project under `src/` sets `<IsPackable>true</IsPackable>` and retains independent package metadata. This preserves local packing and library boundaries; the repository does not publish NuGet packages.

**Rationale:**

- Separate packages let consumers use only the parsing, rules, or analysis components they need while preserving clear layer boundaries.

**Consequences:**

- `src/` libraries retain package metadata for local builds, while `tools/` projects remain executables and package publication stays out of scope.

---

## ADR-009 YAML parser

**Date:** 2026-07-06  
**Context:** Azure Pipelines YAML schema is complex and non-standard.  
**Decision:** Use YamlDotNet for parsing; keep it internal to `Parsing`.  
**Rationale:**

- Battle-tested, actively maintained, supports YAML 1.1/1.2.
- Handles multi-document YAML, anchors, and Azure Pipelines quirks.
- Alternative (System.Text.Json) does not support YAML natively.

**Consequences:**

- YamlDotNet types must never appear in public APIs — only `Core` domain models.
- `Parsing` is the only project that references YamlDotNet.

---

## ADR-010 Agent behaviour guardrails

**Date:** 2026-07-06  
**Context:** AI agents working in this repository need clear, principled guardrails covering destructive actions, human authority, epistemic honesty, and other safety-relevant behaviours.
Ad-hoc or informal safety notes drift and conflict across sessions.  
**Decision:** Adopt a coherent set of agent behaviour principles grounded in the following publicly available frameworks. Record them as the canonical reference so any future change to the guardrails must re-consult these sources and update this ADR.

| Source | Key principles used |
| --- | --- |
| [MCP spec — Security best practices](https://modelcontextprotocol.io/specification/latest/basic/security_best_practices) | User consent, least privilege, local-server security, prompt injection resistance, token and session boundaries |
| [Anthropic — Building effective agents](https://www.anthropic.com/research/building-effective-agents) | Minimal agents, human-in-the-loop checkpoints, explicit uncertainty |
| [Google PAIR — People + AI Guidebook](https://pair.withgoogle.com/guidebook/) | Progressive disclosure of actions, reversibility preference, graceful degradation |
| [Microsoft Responsible AI principles](https://learn.microsoft.com/en-us/azure/machine-learning/concept-responsible-ai) | Accountability, transparency, harm avoidance |
| [OWASP GenAI LLM Top 10 2026](https://genai.owasp.org/resource/owasp-genai-llm-top-10-2026/) | Prompt injection, excessive agency, output handling, least privilege, human approval |
| [GitHub Copilot — Responsible use](https://docs.github.com/en/copilot/responsible-use-of-github-copilot-features/responsible-use-of-github-copilot-chat-in-your-ide) | Agent confirmation before destructive Git operations |

**Rationale:**

- These sources provide a durable rationale for human oversight, minimal agency, explicit uncertainty,
  reversibility, least privilege, output validation, and evolving safety.
- They are directly relevant because this repository reads untrusted YAML pipeline files and exposes
  tools to AI clients.

**Consequences:**

- The operational guardrails live in [agent-behaviour.instructions.md](../.github/instructions/agent-behaviour.instructions.md); this ADR records why they exist.
- The guardrails require explicit approval for high-risk actions, validation of generated output,
  least-privilege execution, restricted local processes where available, and adversarial testing
  for security-sensitive workflows.

---

## ADR-011 Human readability

**Date:** 2026-07-06  
**Context:** AI agents can generate large volumes of code quickly. Without explicit constraints, this leads to oversized files, long methods, speculative scaffolding, and "clever" constructs that are hard to follow without the original context. The codebase must remain maintainable by humans who do not have access to an AI assistant, including contributors who are not native
English speakers.  
**Decision:** Adopt explicit, measurable maintainability rules for code and documentation, grounded in the following public style guides. These apply to agent-generated and human-written code equally.

| Source | Key principles used |
| --- | --- |
| [Microsoft C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) | Layout rules, comment style, `#region` discouraged, one statement per line |
| [Microsoft .NET Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/) | Small focused types, max ~4 parameters, no deep inheritance, prefer interfaces |
| [Google C# Style Guide](https://google.github.io/styleguide/csharp-style.html) | 100-char column limit, one top-level type per file, short focused methods |
| [Oracle Java Code Conventions](https://www.oracle.com/java/technologies/javase/codeconventions-introduction.html) | Methods fit on one screen; classes over 500 lines are a review trigger |
| [Microsoft Writing Style Guide](https://learn.microsoft.com/en-us/style-guide/welcome/) | Plain English, active voice, ≤ 25 words per sentence, define acronyms on first use |
| [GOV.UK Content Design guide](https://www.gov.uk/guidance/content-design/writing-for-gov-uk) | Short sentences, common words, no idioms — written for non-native readers |
| [Plain Language guidelines (plainlanguage.gov)](https://www.plainlanguage.gov/guidelines/) | Write for the reader, use "you", prefer simple words, one idea per sentence |

**Rationale:**

- Explicit limits and plain-language guidance make agent-generated code easier for humans to review and maintain.

**Consequences:**

- Detailed C# and Markdown standards live in [maintainability.instructions.md](../.github/instructions/maintainability.instructions.md) and [markdown.instructions.md](../.github/instructions/markdown.instructions.md).

---

## ADR-012 CSharp implementation patterns

**Date:** 2026-07-07  
**Context:** Agents implementing new `IGuidelineRule` classes repeatedly introduced the same quality issues: a redundant `await Task.CompletedTask` no-op, `HashSet<string>` used in place of `FrozenSet<T>` for static lookup sets, logging source-generation patterns that added unnecessary package coupling to library projects, overly complex regex alternations, and diagnostic messages that repeated information already available in structured fields. These issues were not covered by the
existing instruction files.  
**Decision:** Add `.github/instructions/csharp-patterns.instructions.md` that documents the
correct pattern for each concern, with before/after examples drawn from real code.  
**Rationale:**

- These patterns make guideline rules predictable, readable, efficient, and compatible with the dependency boundaries of `src/` libraries.

**Consequences:**

- Detailed rule implementation patterns live in [csharp-patterns.instructions.md](../.github/instructions/csharp-patterns.instructions.md).

---

## ADR-013 Heuristic detection

**Date:** 2026-07-07  
**Context:** The former phase-one roadmap said "Implement rules for all `ADOG-{CATEGORY}-{NNN}` guidelines in the manifest." Some guideline entries require reasoning about intent, architecture, repository context, or external policy that a static YAML analyser cannot reliably express. Phase 2 explicitly lists "LLM-assisted analysis
for `heuristic` detection rules" as a future enhancement.  
**Decision:** The Phase 1 "all rules implemented" criterion applies only to rules with deterministic local detection. Rules classified as `heuristic` or `not automatable` are deferred to later analysis or remain available for lookup without deterministic diagnostics.  
**Rationale:**

- A static analyser cannot reliably detect heuristic patterns without producing
  excessive false positives or false negatives.
- Implementing stub rules that always return no diagnostics provides no value and
  misleads users.
- Phase 2 LLM-assisted analysis is the correct vehicle for heuristic rules.
- The split aligns with the detection kinds defined in the manifest itself.

**Consequences:**

- Do not claim that a `heuristic` rule is deterministically enforced solely because a rule
  class exists. Record its local automation status and rationale in the metadata provider.
- The `IGuidelineRepository` and `IGuidelineLoader` load all manifest rules at runtime, so heuristic rules are available for lookup via
  `list_guidelines` and `get_guideline` even without a
  corresponding `IGuidelineRule` implementation.
- When Phase 2 begins, re-read this ADR to understand the design boundary.

---

## ADR-014 Debuggability

**Date:** 2026-07-08  
**Context:** Domain model types (`PipelineDocument`, node records, `Diagnostic`, etc.) lacked consistent debugger-friendly representations. The auto-generated positional record `ToString()` can dump every property in declaration order, including large nested collections and multi-kilobyte YAML strings. This made watch-window inspection slow, hover tooltips unreadable, and test failure messages hard to diagnose without expanding each object manually.  
**Decision:** Treat debuggability as a first-class quality concern, on par with testability. All domain types in `Core/` must implement `ToString()`, `[DebuggerDisplay]`, and `[property: DebuggerBrowsable(Never)]` where applicable, following the four rules in `csharp-patterns.instructions.md` section 9.

**Rationale:**

- Concise debugger representations make large domain objects easier to inspect and make test failures easier to understand.

**Consequences:**

- Every new domain type in `Core/` must include a concise `ToString()` and `[DebuggerDisplay]`
  where useful from day one. Existing Core types should be brought to the same standard as they are touched.
- Detailed debugger-display patterns and test expectations live in [csharp-patterns.instructions.md](../.github/instructions/csharp-patterns.instructions.md).

---

## ADR-015 MCP transports

**Date:** 2026-07-29  
**Context:** The MCP server must run in more than one context. A local AI client can start the server as a child process, while an IDE debugger or a separately hosted service needs an HTTP endpoint. Earlier documentation called `stdio` the primary transport and described the HTTP endpoint as SSE-only. That wording incorrectly treated an executable default as a product
preference and did not reflect the current MCP transport direction.  
**Decision:** Support both `stdio` and HTTP transports. Keep `stdio` as the executable's default because it works with process-launching clients and the existing Docker command. Use the HTTP transport when the client connects to an already-running server, including Visual Studio debugging and future remote hosting. The host exposes the HTTP endpoint at `/mcp`. The existing
`SSE` profile names remain as compatibility selectors for the HTTP transport.  
**Rationale:**

- `stdio` uses the standard input and output streams of a locally started process. It avoids a
  listening port and lets the client manage the server lifetime.
- HTTP separates the client and server lifecycles. It supports an already-running server and
  allows the server to be reached across a network when deployment, authentication, transport
  security, and access controls are configured.
- The MCP transport specification recommends Streamable HTTP and treats the earlier HTTP+SSE
  transport as a legacy compatibility option.
- No transport is universally better. The correct transport depends on the client, deployment
  boundary, and operational requirements.

**Consequences:**

- Transport setup, endpoint details, compatibility behavior, and remote security requirements live in [mcp-reference.md](mcp-reference.md).

The current SDK and container compatibility details are maintained in `global.json`, the Docker documentation, and [mcp-reference.md](mcp-reference.md).

---

Copy this block when recording a new decision:

```markdown
## ADR-NNN [Short topic]

**Date:** YYYY-MM-DD  
**Context:** [What problem are we solving?]  
**Decision:** [What did we decide?]  
**Rationale:**

- [Why?]

**Consequences:**

- [What does this imply?]

```

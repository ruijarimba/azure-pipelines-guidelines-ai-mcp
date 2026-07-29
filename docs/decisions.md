# Architecture Decision Records

Lightweight log of significant decisions and rationale. When an agent considers changing one of
these, it must re-read the rationale first; if the context has changed, document the reversal here.

## ADR index

| ADR | Date | Summary |
| --- | --- | --- |
| ADR-001 | 2026-07-06 | .NET 10 as target framework |
| ADR-002 | 2026-07-06 | Central package management via `Directory.Packages.props` |
| ADR-003 | 2026-07-06 | Strict layered architecture with no cycles |
| ADR-004 | 2026-07-06 | xUnit, FluentAssertions, and NSubstitute as the only test libraries |
| ADR-005 | 2026-07-06 | Solution file format uses `.slnx` |
| ADR-006 | 2026-07-06 | `TreatWarningsAsErrors = true` everywhere |
| ADR-007 | 2026-07-06 | Guidelines manifest lives in the companion repository |
| ADR-008 | 2026-07-06 | All `src/` projects are NuGet packages |
| ADR-009 | 2026-07-06 | Parser uses YamlDotNet |
| ADR-010 | 2026-07-06 | Agent behaviour is governed by published collaboration frameworks |
| ADR-011 | 2026-07-06 | Code patterns and maintainability standards |
| ADR-012 | 2026-07-07 | Maintainability rules and pattern guidance |
| ADR-013 | 2026-07-07 | Heuristic detection rules are deferred to Phase 2 |
| ADR-014 | 2026-07-08 | Debuggability is a first-class concern |
| ADR-015 | 2026-07-29 | Support both local and HTTP MCP transports |

---

## ADR-001: .NET 10 as target framework

**Date:** 2026-07-06  
**Context:** Need to choose a .NET version that balances latest language features with stability.  
**Decision:** Target `net10.0` (C# 13) exclusively; no multi-targeting.  
**Rationale:**  
- Latest stable SDK at project start (10.0.301).
- C# 13 features (primary constructors, collection expressions) reduce boilerplate.
- Single TFM simplifies build/test/package matrix.
- NuGet consumers on older runtimes can use older major versions if needed (SemVer).

**Consequences:**  
- Minimum consumer requirement: .NET 10 runtime.
- No need to maintain `#if` preprocessor branches.

---

## ADR-002: Central package management via `Directory.Packages.props`

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

## ADR-003: Strict layered architecture (no cycles)

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

## ADR-004: xUnit, FluentAssertions, NSubstitute — no other test libraries

**Date:** 2026-07-06  
**Context:** Consistency in test style and readability across the codebase.  
**Decision:** Single stack for all tests; no mixing of assertion or mocking libraries.  
**Rationale:**  
- xUnit: industry-standard for .NET, VS/CLI/Rider integration excellent.
- FluentAssertions: readable, expressive assertions (`result.Should().NotBeNull()`).
- NSubstitute: minimal ceremony, natural syntax for substitutes.

**Consequences:**  
- Never use `Assert.*` from xUnit directly — always use FluentAssertions.
- Never use Moq, FakeItEasy, or other mocking libraries.
- Test method naming: `MethodName_GivenContext_ShouldOutcome`.

---

## ADR-005: Solution file format (.slnx)

**Date:** 2026-07-06 (revised 2026-07-08)  
**Context:** MSBuild 17.11+ supports `.slnx` (XML solution format), but not all tooling does.  
**Original decision:** Use traditional `.sln` format for broadest toolchain compatibility.  
**Reversal (2025-06-14):** The repository uses `.slnx` in practice. The original compatibility
concern no longer applies — Visual Studio 2022 17.11+, Visual Studio 2026, and the .NET CLI
all support `.slnx` fully. The XML format is human-readable, produces clean diffs, and is the
preferred format for new projects going forward.

**Consequences:**  
- The solution file is `AzurePipelinesGuidelines.slnx` (XML format, not the legacy binary-text hybrid).
- New files and projects must be registered in the `.slnx` file following the rules in
  `solution-files.instructions.md`.

---

## ADR-006: `TreatWarningsAsErrors = true` everywhere

**Date:** 2026-07-06  
**Context:** Want zero-tolerance for code quality issues.  
**Decision:** Set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in root `Directory.Build.props`.  
**Rationale:**  
- Forces resolution of all warnings before commit.
- Prevents warning accumulation over time.
- `AnalysisLevel=latest-all` ensures all Roslyn analysers are active.

**Consequences:**  
- Every new warning is a build break.
- Use `#pragma warning disable` only with an inline comment explaining the permanent exception.
- Suppressions in `.editorconfig` or `.globalconfig` must be justified in this file or in code comments.

---

## ADR-007: Guidelines manifest lives in the companion repository

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

## ADR-008: All `src/` projects are NuGet packages

**Date:** 2026-07-06  
**Context:** Want reusable libraries that consumers can compose as needed.  
**Decision:** Every project under `src/` sets `<IsPackable>true</IsPackable>` and is configured
for future NuGet publication.  
**Rationale:**  
- Consumers may want only parsing, or only rules, or only the analysis engine.
- Separate packages enable independent versioning per component.
- Encourages clean boundaries (NuGet packages are a natural unit of deployment).

**Consequences:**  
- Every `public` API must be documented via XML comments.
- Breaking changes require a major version bump (SemVer 2.0 strict).
- `tools/` projects are not NuGet packages — they are executables only.
- NuGet publication is deferred until a future release decision.

---

## ADR-009: Parser uses YamlDotNet, not System.Text.Json or custom parser

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

## ADR-010: Agent behaviour governed by published human-AI collaboration frameworks

**Date:** 2026-07-06  
**Context:** AI agents working in this repository need clear, principled guardrails covering
destructive actions, human authority, epistemic honesty, and other safety-relevant behaviours.
Ad-hoc or informal safety notes drift and conflict across sessions.  
**Decision:** Adopt a coherent set of agent behaviour principles grounded in the following
publicly available frameworks. Record them as the canonical reference so any future change to
the guardrails must re-consult these sources and update this ADR.

| Source | Key principles used |
| --- | --- |
| [MCP spec — Security best practices](https://modelcontextprotocol.io/specification/2025-03-26/basic/security_best_practices) | User consent and control, minimal footprint, trust hierarchy, prompt injection resistance |
| [Anthropic — Building effective agents](https://www.anthropic.com/research/building-effective-agents) | Minimal agents, human-in-the-loop checkpoints, explicit uncertainty |
| [Google PAIR — People + AI Guidebook](https://pair.withgoogle.com/guidebook/) | Progressive disclosure of actions, reversibility preference, graceful degradation |
| [Microsoft Responsible AI principles](https://learn.microsoft.com/en-us/azure/machine-learning/concept-responsible-ai) | Accountability, transparency, harm avoidance |
| [OWASP LLM Top 10](https://owasp.org/www-project-top-10-for-large-language-model-applications/) | LLM06 Excessive Agency, LLM07 Overreliance, LLM01 Prompt Injection |
| [GitHub Copilot — Responsible use](https://docs.github.com/en/copilot/responsible-use-of-github-copilot-features/responsible-use-of-github-copilot-chat-in-your-ide) | Agent confirmation before destructive Git operations |

**Rationale:**  
- These are authoritative, maintained, public documents — not ad-hoc opinions.
- Grounding guardrails in named sources gives future agents (and humans) a way to resolve
  ambiguity: "what does the MCP spec say about this?" is answerable; "what did someone mean
  here?" is not.
- OWASP LLM06 (Excessive Agency) and LLM01 (Prompt Injection) are directly relevant because
  this repo reads and analyses untrusted YAML pipeline files and exposes MCP tools to AI clients.

**Consequences:**  
- The current behaviour principles are documented in
  `.github/instructions/agent-behaviour.instructions.md` with `applyTo: "**"`.
- Before changing any guardrail, re-read the relevant source(s) above and update this ADR.
- Before pushing changes, agents must verify that the solution builds successfully and the
  relevant unit test suite passes.
- The instruction file links back here so the rationale chain is always traceable.
- When a session becomes long or context-heavy, agents should create concise handoff summaries
  or use the available conversation-summary feature so later turns preserve the current state,
  constraints, and next steps without reloading the entire history.

---

## ADR-011: Human readability as a first-class requirement

**Date:** 2026-07-06  
**Context:** AI agents can generate large volumes of code quickly. Without explicit constraints,
this leads to oversized files, long methods, speculative scaffolding, and "clever" constructs
that are hard to follow without the original context. The codebase must remain maintainable
by humans who do not have access to an AI assistant, including contributors who are not native
English speakers.  
**Decision:** Adopt explicit, measurable maintainability rules for code and documentation,
grounded in the following public style guides. These apply to agent-generated and human-written
code equally.

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
- Code is read far more often than it is written (Fowler, *Refactoring*, 1999).
- Agents optimise for task completion, not for the next human who opens the file.
- Non-native English speakers are part of the target audience for both code comments and
  documentation; plain language reduces ambiguity for everyone.
- Measurable limits (line counts, parameter counts) are enforceable in reviews and by
  analysers, unlike vague guidance such as "keep it simple."

**Consequences:**  
- Maintainability rules are documented in
  `.github/instructions/maintainability.instructions.md` with `applyTo: "**/*.cs"`.
- Markdown writing rules are documented in
  `.github/instructions/markdown.instructions.md` with `applyTo: "**/*.md"`.
- Both instruction files link back here so the rationale chain is traceable.
- Before changing any limit or rule, re-read the relevant source(s) above and update this ADR.

---

## ADR-012: C# implementation patterns for `IGuidelineRule`, logging, and static sets

**Date:** 2026-07-07  
**Context:** Agents implementing new `IGuidelineRule` classes repeatedly introduced the same
quality issues: a redundant `await Task.CompletedTask` no-op, `HashSet<string>` used in place
of `FrozenSet<T>` for static lookup sets, `[LoggerMessage]` attribute on partial methods
(which requires the full `Microsoft.Extensions.Logging` package that `src/` libraries must not
reference), overly complex regex alternations, and diagnostic messages that repeated
information already available in structured fields. These issues were not covered by the
existing instruction files.  
**Decision:** Add `.github/instructions/csharp-patterns.instructions.md` that documents the
correct pattern for each concern, with before/after examples drawn from real code.  
**Rationale:**  
- Rules that are written down are repeatable. Rules that live only in a code-review comment
  are lost after the session ends.
- `FrozenSet<T>` is the BCL-recommended type for immutable, read-heavy lookup sets (.NET 8+).
  Using `HashSet<string>` signals mutability that does not exist.
- `[LoggerMessage]` source-generates partial methods and requires the full Logging package.
  `LoggerMessage.Define` achieves the same goal with the Abstractions package only.
- One large regex alternation combining two structurally different YAML patterns violates the
  "no clever code" rule (maintainability rule 7). Two focused patterns are independently
  testable and readable.
- Diagnostic messages that embed `line` or `column` values duplicate structured fields and
  bloat the message text.

**Consequences:**  
- All new `IGuidelineRule` implementations must follow the patterns in the instruction file.
- The instruction file is listed in `.github/copilot-instructions.md` under Active instruction
  files so agents read it before writing code.
- Before changing any pattern, re-read the relevant source(s) in ADR-011 and update this ADR.

---

## ADR-013: Heuristic detection rules are deferred to Phase 2

**Date:** 2026-07-07  
**Context:** The `docs/vision.md` Phase 1 scope says "Implement rules for all
`ADOG-{CATEGORY}-{NNN}` guidelines in the manifest." After auditing the live
`guidelines.json` manifest, 36 guideline IDs exist. 9 have `detection.kind` of
`regex` or `yamlPath` — these are statically detectable and were implemented in
Phase 1. The remaining 27 all have `detection.kind = heuristic`, meaning they
require reasoning about intent, architecture, or context that a regex or YAML
path expression cannot express. Phase 2 explicitly lists "LLM-assisted analysis
for `heuristic` detection rules" as a future enhancement.  
**Decision:** The Phase 1 "all rules implemented" criterion applies only to rules
with `detection.kind` of `regex` or `yamlPath`. Rules with `detection.kind =
heuristic` are deferred to Phase 2. This interpretation resolves the apparent
contradiction between "implement all rules" and "LLM-assisted heuristics are
Phase 2."  
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
- The `IGuidelineRepository` and `IGuidelineLoader` already load all 36 rules
  from the manifest at runtime, so heuristic rules are available for lookup via
  `list_guidelines`, `get_guideline`, and `adog rules show` even without a
  corresponding `IGuidelineRule` implementation.
- When Phase 2 begins, re-read this ADR to understand the design boundary.

---

## ADR-014: Debuggability as a first-class concern

**Date:** 2026-07-08  
**Context:** Domain model types (`PipelineDocument`, node records, `Diagnostic`, etc.) had no
`ToString()` overrides. The auto-generated positional record `ToString()` dumps every property
in declaration order, including large nested collections and multi-kilobyte YAML strings. This
made watch-window inspection slow, hover tooltips unreadable, and test failure messages hard to
diagnose without expanding each object manually.  
**Decision:** Treat debuggability as a first-class quality concern, on par with testability.
All domain types in `Core/` must implement `ToString()`, `[DebuggerDisplay]`, and
`[property: DebuggerBrowsable(Never)]` where applicable, following the four rules in
`csharp-patterns.instructions.md` section 9.

**Rationale:**

| Source | Key principle used |
| --- | --- |
| [Microsoft Framework Design Guidelines — Object.ToString](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/object-tostring) | "DO override `ToString` to return a human-readable, developer-oriented representation." Also: do not throw, do not return null. |
| [Visual Studio — Using the DebuggerDisplay attribute](https://learn.microsoft.com/en-us/visualstudio/debugger/using-the-debuggerdisplay-attribute) | Official guidance on `[DebuggerDisplay]`, `[DebuggerBrowsable]`, and `[DebuggerTypeProxy]`. |
| [.NET BCL source (dotnet/runtime)](https://github.com/dotnet/runtime) | `List<T>`, `Dictionary<TKey,TValue>`, `Span<T>`, `KeyValuePair<TK,TV>` and hundreds of other BCL types carry `[DebuggerDisplay]`. The runtime team applies it consistently — this is the strongest available signal that Microsoft engineers treat it as non-negotiable. |
| [CA1305 — Specify IFormatProvider](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1305) | Calling `int.ToString()` without a format provider is locale-sensitive. CA1305 is a build error in this project (ADR-006), so `CultureInfo.InvariantCulture` is required in every `ToString()` that formats a number. |

Additional reasoning:

- Poor debugger representations slow down development proportionally to the depth and size of
  the object graph. For a type like `PipelineDocument` that carries a full YAML string plus
  collections of stages, jobs, and steps, the default dump is unusable at a glance.
- Missing `ToString()` overrides produce unhelpful FluentAssertions failure messages such as
  `Expected "StageNode { Name = ..., Jobs = [...] }" but found ...` — the noise obscures the
  actual assertion.
- These are productivity and correctness costs comparable to missing unit test coverage;
  treating one as mandatory and the other as optional is inconsistent.

**Consequences:**

- Every new domain type in `Core/` must include `ToString()` and `[DebuggerDisplay]` from
  day one. A missing override is a code-review defect, not optional polish.
- `[property: DebuggerBrowsable(DebuggerBrowsableState.Never)]` must be applied to any
  property whose expanded view in the watch window would obscure more useful adjacent data
  (e.g. raw YAML strings, flattened projection properties).
- All numeric values in `ToString()` implementations must use `CultureInfo.InvariantCulture`
  and `using System.Globalization;`.
- Every `ToString()` override must have dedicated unit tests covering all logical branches,
  per the coverage rules in `testing.instructions.md`.
- The four concrete rules are documented in `csharp-patterns.instructions.md` section 9 with
  before/after examples. That file links back here so the rationale chain is traceable.

---

## ADR-015: Support both local and HTTP MCP transports

**Date:** 2026-07-29  
**Context:** The MCP server must run in more than one context. A local AI client can start the
server as a child process, while an IDE debugger or a separately hosted service needs an HTTP
endpoint. Earlier documentation called `stdio` the primary transport and described the HTTP
endpoint as SSE-only. That wording incorrectly treated an executable default as a product
preference and did not reflect the current MCP transport direction.  
**Decision:** Support both `stdio` and HTTP transports. Keep `stdio` as the executable's
default because it works with process-launching clients and the existing Docker command. Use the
HTTP transport when the client connects to an already-running server, including Visual Studio
debugging and future remote hosting. The host exposes the HTTP endpoint at `/mcp`. The existing
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
- Documentation must distinguish the current `stdio` default from a general recommendation.
- Documentation must call the `/mcp` endpoint HTTP or Streamable HTTP, not SSE-only, unless the
  implementation explicitly adds the legacy HTTP+SSE transport.
- The existing `SSE` launch-profile name remains for compatibility with the local debugging
  workflow. Its documentation must explain that it starts the host HTTP transport.
- Remote HTTP deployments require HTTPS and appropriate authentication and authorization. They
  must not expose the endpoint publicly without those controls.

---

## Template for new decisions

Copy this block when recording a new decision:

```markdown
## ADR-NNN: [Short title]

**Date:** YYYY-MM-DD  
**Context:** [What problem are we solving?]  
**Decision:** [What did we decide?]  
**Rationale:** [Why?]  
**Consequences:** [What does this imply?]
```

# Architecture Decision Records

Lightweight log of significant decisions and rationale. When an agent considers changing one of
these, it must re-read the rationale first; if the context has changed, document the reversal here.

---

## ADR-001: .NET 10 as target framework

**Date:** 2025-01-25  
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

**Date:** 2025-01-25  
**Context:** 12 projects, shared dependencies — want single source of truth for package versions.  
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

**Date:** 2025-01-25  
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

**Date:** 2025-01-25  
**Context:** Consistency in test style and readability across the codebase.  
**Decision:** Single stack for all tests; no mixing of assertion or mocking libraries.  
**Rationale:**  
- xUnit: industry-standard for .NET, VS/CLI/Rider integration excellent.
- FluentAssertions: readable, expressive assertions (`result.Should().NotBeNull()`).
- NSubstitute: minimal ceremony, natural syntax for substitutes.

**Consequences:**  
- Never use `Assert.*` from xUnit directly — always FluentAssertions.
- Never use Moq, FakeItEasy, or other mocking libraries.
- Test method naming: `MethodName_GivenContext_ShouldOutcome`.

---

## ADR-005: `.sln` format (not `.slnx`)

**Date:** 2025-01-25  
**Context:** MSBuild 17.11+ supports `.slnx` (XML solution format), but not all tooling does.  
**Decision:** Use traditional `.sln` format.  
**Rationale:**  
- Broadest toolchain compatibility (older VS versions, CI systems, build automation).
- `.slnx` offers no critical benefit for this repository's structure.
- Can migrate later if ecosystem adoption improves.

**Consequences:**  
- Solution file remains binary/text hybrid format.
- No impact on build performance or project management.

---

## ADR-006: `TreatWarningsAsErrors = true` everywhere

**Date:** 2025-01-25  
**Context:** Want zero-tolerance for code quality issues.  
**Decision:** Set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in root `Directory.Build.props`.  
**Rationale:**  
- Forces resolution of all warnings before commit.
- Prevents warning accumulation over time.
- `AnalysisLevel=latest-all` ensures all Roslyn analysers are active.

**Consequences:**  
- Every new warning is a build break.
- Use `#pragma warning disable` **only** with an inline comment explaining the permanent exception.
- Suppressions in `.editorconfig` or `.globalconfig` must be justified in this file or in code comments.

---

## ADR-007: Guidelines manifest lives in the companion repository

**Date:** 2025-01-25  
**Context:** Need a single source of truth for rule definitions.  
**Decision:** Consume `data/guidelines.json` from [azure-pipelines-guidelines](https://github.com/ruijarimba/azure-pipelines-guidelines).  
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

**Date:** 2025-01-25  
**Context:** Want reusable libraries that consumers can compose as needed.  
**Decision:** Every project under `src/` sets `<IsPackable>true</IsPackable>` and will be published to NuGet.  
**Rationale:**  
- Consumers may want only parsing, or only rules, or only the analysis engine.
- Separate packages enable independent versioning per component.
- Encourages clean boundaries (NuGet packages are a natural unit of deployment).

**Consequences:**  
- Every `public` API must be documented via XML comments.
- Breaking changes require a major version bump (SemVer 2.0 strict).
- `tools/` projects are **not** NuGet packages — they are executables only.

---

## ADR-009: Parser uses YamlDotNet, not System.Text.Json or custom parser

**Date:** 2025-01-25  
**Context:** Azure Pipelines YAML schema is complex and non-standard.  
**Decision:** Use YamlDotNet for parsing; keep it internal to `Parsing`.  
**Rationale:**  
- Battle-tested, actively maintained, supports YAML 1.1/1.2.
- Handles multi-document YAML, anchors, and Azure Pipelines quirks.
- Alternative (System.Text.Json) does not support YAML natively.

**Consequences:**  
- YamlDotNet types must **never** appear in public APIs — only `Core` domain models.
- `Parsing` is the only project that references YamlDotNet.

---

## ADR-010: Agent behaviour governed by published human-AI collaboration frameworks

**Date:** 2025-01-25
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
- The seven behaviour principles are documented in
  `.github/instructions/agent-behaviour.instructions.md` with `applyTo: "**"`.
- Before changing any guardrail, re-read the relevant source(s) above and update this ADR.
- The instruction file links back here so the rationale chain is always traceable.

---

## ADR-011: Human readability as a first-class requirement

**Date:** 2025-01-25
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

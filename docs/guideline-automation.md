# Guideline automation status

This document describes which Azure Pipelines guidelines the local analyzer can evaluate from one YAML document. It separates deterministic local checks from recommendations that require repository context or human judgment.

## Status definitions

| Status | Meaning |
| --- | --- |
| `enforceable` | The analyzer can identify the condition reliably from the current YAML document. |
| `heuristic` | The analyzer can identify evidence of a concern, but YAML alone cannot prove a violation. |
| `notAutomatable` | The guideline requires repository, deployment, task, or human context that is unavailable to the local analyzer. |

These statuses describe the current analyzer implementation. They are not the same as the manifest detection kinds documented in [How it works](how-it-works.md#detection-kinds).

## Enforceable guidelines

The following guidelines have deterministic local checks:

- `ADOG-GENERAL-001` — Template paths can be checked directly.
- `ADOG-GENERAL-004` — A first non-empty comment can be checked as a local documentation policy.
- `ADOG-JOBS-001` — Explicit checkout entries can be identified.
- `ADOG-JOBS-002` — Non-checkout job steps can be counted.
- `ADOG-JOBS-003` — Pipeline-root variables can be identified when jobs exist.
- `ADOG-JOBS-006` — Job timeout settings can be inspected.
- `ADOG-STEPS-001` — Macro syntax can be identified.
- `ADOG-STEPS-007` — Supported step controls and template parameters can be checked.
- `ADOG-STEPS-010` — Pipeline expressions in step content can be identified.
- `ADOG-STEPS-011` — AzureKeyVault task references can be identified.
- `ADOG-VARIABLES-005` — Pipeline- and stage-scope variables can be identified.

These checks inspect only the current YAML document. They do not resolve templates from other files or infer task behavior, repository intent, or deployment context.

## Heuristic guidelines

These guidelines may provide useful evidence, but their result depends on context:

- `ADOG-GENERAL-002`, `ADOG-GENERAL-003`, `ADOG-GENERAL-005`, and `ADOG-GENERAL-007`
- `ADOG-JOBS-005`
- `ADOG-STEPS-002`, `ADOG-STEPS-003`, `ADOG-STEPS-004`, and `ADOG-STEPS-006`
- `ADOG-VARIABLES-001`, `ADOG-VARIABLES-003`, and `ADOG-VARIABLES-006`

For example, a hard-coded value may be intentional, and a task timeout may be inappropriate for a particular task. Treat these results as guidance rather than proof.

## Guidelines that cannot be automated locally

The following guidelines require information unavailable in one YAML document:

- `ADOG-GENERAL-006`
- `ADOG-JOBS-004`, `ADOG-JOBS-007`, and `ADOG-JOBS-008`
- `ADOG-PARAMETERS-001` and `ADOG-PARAMETERS-002`
- `ADOG-PIPELINES-001`
- `ADOG-STAGES-001` and `ADOG-STAGES-002`
- `ADOG-STEPS-005`, `ADOG-STEPS-008`, and `ADOG-STEPS-009`
- `ADOG-VARIABLES-002` and `ADOG-VARIABLES-004`

These guidelines remain available through the guideline catalogue and can be reviewed with `adog rules show <rule-id>` or the corresponding MCP lookup tools.

## Limitations

The analyzer is a static analysis tool. It does not execute pipelines, resolve every referenced template, inspect repository history, infer deployment intent, or determine whether a task is safe for a particular environment. A clean result means that no implemented local check found a violation; it does not prove that the pipeline follows every guideline.

For the detection model used by the manifest, see [How it works](how-it-works.md#detection-kinds). For the architectural decision about heuristic analysis, see [ADR-013](decisions.md#adr-013-heuristic-detection-rules-are-deferred-to-phase-2).

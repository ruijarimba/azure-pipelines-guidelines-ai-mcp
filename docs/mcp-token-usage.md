# MCP token usage guide

This guide explains how each MCP capability affects the number of tokens an AI client spends when it uses the `azure-pipelines-guidelines` server. Use it to choose the operation that answers a question with the least amount of returned content.

For the full tool, resource, and prompt catalogue, see the [MCP server reference](mcp-reference.md). For the underlying analysis pipeline, see [the architecture guide](architecture.md#mcp-tool-surface).

## Why this matters

Every MCP tool, resource, or prompt result becomes part of the client's context. A response that returns the full guideline catalogue or every diagnostic from a large repository costs far more tokens than a response scoped to exactly what the client needs. The server is designed to return compact results by default and require an explicit opt-in for larger payloads, but the client still controls which operation it calls and with which parameters.

## Summaries by default, detail on request

- `get_guideline` returns a compact summary (`id`, `title`, `category`, `severity`) unless the
  caller passes `detail=full`. Only request `detail=full` when the description, detection hints,
  fix guidance, or reference links are actually needed.
- `list_guidelines` and `search_guidelines` always return compact summaries, never the full
  detail payload, so browsing and filtering stay lightweight even across the full catalogue.
- Prefer `get_guideline` with a known rule ID over browsing the full catalogue when the client
  already knows which guideline it needs (for example, from a diagnostic's `ruleId` field).
- `explain_diagnostic` is the most token-efficient way to explain a single diagnostic: it returns
  one guideline's full detail plus an optional echo of the diagnostic context, never the full
  catalogue. Prefer it over `get_guideline` with `detail=full` when you also want to pair the
  explanation with the original diagnostic's message, file path, line, and column.

## Server-side analysis summaries

- `analyze_template` returns a `summary` object
  (`filesAnalyzed`, `filesWithFindings`, `totalFindings`, and, when findings exist, `byRecommendation`,
  `byCategory`, and `byRule` count maps) alongside the detailed `diagnostics` array.
- A client can inspect the summary first to decide whether it needs the full diagnostics list at
  all, or whether it can answer the user's question (for example, "are there any DO violations?") from
  the summary alone.
- The analysis tool accepts an optional `guidelineIds` parameter (a comma-separated list such as
  `ADOG-STEPS-001,ADOG-JOBS-006`) and a `category` parameter. Passing either narrows the rules
  that run and shrinks the diagnostics returned, instead of analyzing against all rules and
  filtering client-side. By default only enforceable rules are checked; pass
  `includeNonEnforceable: true` to also include heuristic and non-automatable rules.
- Empty count maps are omitted from the summary, and count maps are sorted deterministically, so
  the response never carries redundant zero-valued fields.

## Cache-friendly resources

Resource endpoints are smaller and more predictable than repeatedly requesting the full catalogue:

- `adog://guidelines/version` returns only the current catalogue version. A client can cache the
  catalogue and skip re-fetching it entirely when the version is unchanged.
- `adog://capabilities` returns a compact, cacheable description of the server version, catalogue
  version, supported transports, and available tools, resources, and prompts. Use it once at
  startup instead of probing individual capabilities.
- `adog://guidelines/category/{category}` returns only the entries for one category, which is
  smaller than `adog://guidelines` (the full catalogue) when the client only needs guidelines
  from one category such as `steps` or `jobs`.
- `adog://guidelines/{id}` and `adog://guidelines/{id}/automation` return one guideline's full
  detail or automation status respectively, scoped to a single rule ID.

Prefer these narrower resources over `adog://guidelines` (the full catalogue) whenever the client only needs a version check, a capability check, one category, or one guideline.

## Prompts are procedural, not data-heavy

The predefined prompts (`review`, `review-category`, `review-guideline`, `explain-guideline`, `find-guidelines`, `list-guidelines`, `list-categories`) return instructions that tell the client which existing tool to call and how to present the result. They do not embed the full guideline catalogue or duplicate tool output, so invoking a prompt does not by itself add significant token cost beyond the tool call it triggers.

## Practical guidance for AI clients

- Use a focused lookup (`get_guideline` by ID) instead of the full catalogue (`list_guidelines` or
  `adog://guidelines`) whenever the target rule ID is already known.
- Pass `category` or `guidelineIds` to `analyze_template` when the user
  only cares about a subset of rules, instead of running every rule and filtering the result
  afterward.
- Read the `summary` object first and only request or process the detailed `diagnostics` array
  when the user needs individual findings.
- Cache `adog://guidelines/version` and `adog://capabilities` results across a session instead of
  re-fetching them for every request.
- Treat the full guideline catalogue (`list_guidelines` with no filter, `adog://guidelines`) and
  large multi-file `analyze_template` calls using `fileOrPath` as the main source of token cost. Scope them to
  the smallest set of files, categories, or rule IDs that answers the user's question.

## See also

- [MCP server reference](mcp-reference.md) — full tool, resource, and prompt catalogue
- [Architecture guide](architecture.md) — MCP tool surface and analysis pipeline
- [Glossary](glossary.md) — domain vocabulary used throughout this guide

# MCP Sample Prompts

These prompts are examples for users of the Azure Pipelines Guidelines MCP server. Replace the
placeholders with your own pipeline, template, file, directory, or rule IDs.

The prompts are ordered from the most basic review to more targeted workflows. MCP analysis is
advisory: preserve the original guideline wording (`do`, `don't`, `avoid`, or `consider`) and use
the results to guide review rather than treating them as a binary pass/fail decision.

## 1. Analyze a pipeline

> Analyze this Azure Pipelines YAML against the available guidelines. Identify any issues, explain
> what each issue means, and suggest a concise fix. Do not modify the file.

## 2. Analyze a reusable template

> Analyze this Azure Pipelines template against the available guidelines. Identify any issues,
> explain what each issue means, and suggest a concise fix. Do not modify the file.

## 3. Review a pipeline directory

> Analyze all Azure Pipelines YAML files under `<directory>`. Summarize the findings and point out
> repeated issues across files. Do not modify the files.

## 4. Review a pipeline and its templates

> Analyze the pipeline and reusable templates under `<directory>`. Consider how the templates are
> used by the pipeline, identify issues in both the callers and templates, and report the source
> file for each finding. Do not modify the files.

## 5. Focus on one guideline category

> Analyze this pipeline for `steps` guidelines only. Ignore findings from all other categories and
> suggest concise fixes for the findings you report.

## 6. Exclude selected rules

> Analyze this pipeline using all available guidelines except `ADOG-STEPS-001`, `ADOG-JOBS-006`,
> and `ADOG-VARIABLES-003`. Do not report the excluded rules.

## 7. Run a targeted review

> Review this pipeline for timeout, retry, authentication, and secret-handling concerns. Identify
> the relevant guideline rules, analyze the pipeline with those rules, and keep the response
> focused on those concerns.

## 8. Optimize the response for low token usage

> Analyze this pipeline using compact output. Return only the rule ID, advisory guidance, severity,
> message, file, and line for each finding. Omit repeated rule descriptions and remediation details
> unless they are needed to understand a finding.

## 9. Prioritize findings for remediation

> Analyze this pipeline and prioritize the findings for review. Preserve each guideline's original
> advisory label (`do`, `don't`, `avoid`, or `consider`), group repeated findings by rule, and
> suggest the smallest practical fixes first.

## 10. Review a proposed change

> Analyze this Azure Pipelines change for guideline regressions. Focus on the added or modified
> lines, compare the result with the previous version if provided, and report only actionable
> findings. Do not suggest unrelated improvements.

## Related documentation

- [MCP Server Reference](mcp-reference.md) — installation, configuration, tools, and response format
- [Architecture guide](architecture.md) — project boundaries and MCP extension points

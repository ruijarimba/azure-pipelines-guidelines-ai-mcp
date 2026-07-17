# Azure Pipelines YAML schema

This repository analyzes Azure Pipelines YAML in two layers:

1. **Schema validation** checks whether the document has a plausible Azure Pipelines structure.
2. **Guideline analysis** checks the document against advisory `ADOG-*` rules.

The guideline analyzer must not treat a recommendation such as a missing timeout as a schema error. For example, `jobs` containing a job with `steps` is structurally valid even when the job omits `timeoutInMinutes`; that omission is reported separately by `ADOG-JOBS-006`.

## Official schema reference

Use the [Azure Pipelines YAML schema reference for Azure DevOps Services](https://learn.microsoft.com/en-us/azure/devops/pipelines/yaml-schema) as the canonical, current documentation source.

The reference describes pipeline definitions and reusable structures including:

- Pipeline-level properties.
- Stages and stage templates.
- Jobs, deployment jobs, and job templates.
- Steps and step templates.
- Parameters, pools, resources, variables, triggers, and related properties.

The schema reference does not cover task-specific inputs. For task validation, use the [Azure Pipelines tasks reference](https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/reference).

## Local validation scope

The repository includes a deliberately limited local validator exposed through the parsing layer. It validates structure that can be checked without contacting Azure DevOps, including:

- YAML syntax and a mapping document root.
- Recognized pipeline-level sections.
- Sequence shapes for `stages`, `jobs`, and `steps`.
- Required identifiers such as `stage`, `job`, `deployment`, and `template`.
- Template usage contexts, such as stages, jobs, job, steps, and step templates.
- Basic scalar types and obvious mutually exclusive structures.

The validator remains permissive around Azure DevOps expressions and constructs that require template expansion or service context. It reports `SchemaDiagnostic` values separately from advisory guideline diagnostics.

A standalone YAML file does not always contain enough information to determine its template kind. Validation therefore accepts an explicit `PipelineSchemaContext` when needed, such as `Pipeline`, `Stages`, `Jobs`, `Job`, `Steps`, or `Step`.

This is not a complete Azure DevOps schema implementation. It does not expand templates, evaluate expressions, validate task-specific inputs, resolve referenced repositories, or contact Azure DevOps.

## Azure DevOps service validation

For authoritative validation, use Azure DevOps itself through a preview or validation workflow. Service validation can resolve organization-specific context that local analysis cannot, including referenced repositories, templates, parameters, compile-time expressions, tasks, and extensions.

Service validation should be optional. The CLI and MCP server should remain deterministic and useful offline, with local validation providing fast feedback and Azure DevOps providing an authoritative check when credentials and project context are available.

## Microsoft Learn MCP

A Microsoft Learn MCP server can complement local validation during interactive development. It can retrieve current schema documentation, explain template contexts, and locate the relevant reference pages. It should not be a runtime dependency of the analyzer because MCP access may require network connectivity and client configuration, and documentation lookup does not expand or execute a pipeline.

Recommended separation:

```text
Local YAML parser
    -> local structural validator
    -> advisory guideline rules

AI assistant
    -> Microsoft Learn MCP for current documentation and explanations
```

## PDF reference

The file `azure-devops-pipelines-yaml-schema-azure-pipelines.pdf` is an exported Microsoft Learn document. It can be useful as an offline human reference, but it is not a machine-readable schema and should not be used as a runtime dependency.

The live Microsoft Learn page is the canonical source because it can be updated independently of this repository. Do not commit the exported PDF unless the repository intentionally distributes a documentation snapshot and records its source URL, retrieval date, and redistribution terms. If a snapshot is added later, store it under `docs/reference/` and keep the live link above.

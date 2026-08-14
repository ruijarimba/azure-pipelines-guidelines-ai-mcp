# Guideline automation status

This document describes which Azure Pipelines guidelines the local analyzer can evaluate from one YAML document. It separates deterministic local checks from recommendations that require repository context or human judgment.

MCP clients can read this information from `adog://guidelines/{id}/automation`. Full guideline responses also include `automationStatus` and `automationReason`.

## Status definitions

| Status | Meaning |
| --- | --- |
| `enforceable` | The analyzer can identify the condition reliably from the current YAML document. |
| `heuristic` | The analyzer can identify evidence of a concern, but YAML alone cannot prove a violation. |
| `notAutomatable` | The guideline requires repository, deployment, task, or human context that is unavailable to the local analyzer. |

These statuses describe the current analyzer implementation. They are not the same as the manifest detection kinds documented in [How it works](how-it-works.md#detection-kinds).

## Enforceable guidelines

The following guidelines have deterministic local checks. They are sorted by category and rule ID.

| Guideline | Category | Summary |
| --- | --- | --- |
| [ADOG-GENERAL-001](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-absolute-paths.md) | General | CONSIDER: Use absolute paths to reference stages, jobs, steps and variables templates. |
| [ADOG-GENERAL-004](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-documentation.md) | General | DO: Document pipelines and templates. Add comments to the top of pipeline and template files. Describe the purpose, usage, and other relevant information clearly for both people and tools. |
| [ADOG-JOBS-001](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-explicit-checkout.md) | Jobs | CONSIDER: Explicitly set `checkout` in every job to make source code checkout behavior clear and stable. |
| [ADOG-JOBS-002](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-grouping-tasks.md) | Jobs | CONSIDER: Group job tasks into a single steps template, rather than using multiple steps templates in a job. |
| [ADOG-JOBS-003](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-job-variables.md) | Jobs | CONSIDER: Declare variables at the job level instead of the stage or root level. |
| [ADOG-JOBS-006](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-job-timeouts.md) | Jobs | DO: Set job timeouts or add parameters to shared job templates to let users configure them. |
| [ADOG-STEPS-001](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/avoid-pipeline-variables.md) | Steps | AVOID: Avoid pipeline variables in steps templates. Use parameters instead. |
| [ADOG-STEPS-007](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-extensible-steps.md) | Steps | DO: Add control parameters when building reusable templates. |
| [ADOG-STEPS-010](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-mix-syntax.md) | Steps | DO-NOT: Bind pipeline expressions at a script task boundary instead of embedding them throughout the script body. |
| [ADOG-STEPS-011](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-use-azurekeyvault-task.md) | Steps | DO-NOT: Do not run the `AzureKeyVault` task to pull secrets into pipeline variables. |
| [ADOG-VARIABLES-005](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-variable-scope.md) | Variables | DO: Restrict the scope of variables as much as possible. |

These checks inspect only the current YAML document. They do not resolve templates from other files or infer task behavior, repository intent, or deployment context.

## Heuristic guidelines

These guidelines may provide useful evidence, but their result depends on context. They are sorted by category and rule ID.

| Guideline | Category | Summary |
| --- | --- | --- |
| [ADOG-GENERAL-002](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-native-yaml-constructs.md) | General | CONSIDER: Prefer YAML-native constructs to express values, logic, and scripts in a clear, consistent, and platform-agnostic way. |
| [ADOG-GENERAL-003](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-schema-compatible-types.md) | General | CONSIDER: Use the same name, type, and schema-compatible default for parameters that map to YAML fields. |
| [ADOG-GENERAL-005](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-folder-structure.md) | General | DO: Organize pipelines and templates logically and consistently across projects and repositories. |
| [ADOG-GENERAL-007](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/donot-hard-code-values.md) | General | DO-NOT: Do not hard-code values in Azure DevOps pipelines and templates. |
| [ADOG-JOBS-005](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-extensible-jobs.md) | Jobs | DO: Add parameters when creating job templates for reuse by different teams, stages, or pipelines. |
| [ADOG-STEPS-002](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-environment-variables.md) | Steps | CONSIDER: Set environment variables at the task level. |
| [ADOG-STEPS-003](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-logging-diagnostics.md) | Steps | CONSIDER: Log enough diagnostic details to troubleshoot issues and failures. |
| [ADOG-STEPS-004](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-logging-diagnostics.md) | Steps | CONSIDER: Log enough diagnostic details to troubleshoot issues and failures. |
| [ADOG-STEPS-006](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-timeouts.md) | Steps | CONSIDER: Set task timeouts to avoid stalled pipeline runs. |
| [ADOG-VARIABLES-001](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/consider-read-only-variables.md) | Variables | CONSIDER: Mark variables as `readonly` when they should not change after initialization. |
| [ADOG-VARIABLES-003](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-sensitive-information.md) | Variables | DO: Store passwords, tokens, and keys in variable groups. |
| [ADOG-VARIABLES-006](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/donot-mix-environments.md) | Variables | DO-NOT: Do not define variables for multiple environments in one variable template. |

For example, a hard-coded value may be intentional, and a task timeout may be inappropriate for a particular task. Treat these results as guidance rather than proof.

## Guidelines that cannot be automated locally

The following guidelines require information unavailable in one YAML document. They are sorted by category and rule ID.

| Guideline | Category | Summary |
| --- | --- | --- |
| [ADOG-GENERAL-006](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-templates-everywhere.md) | General | DO: Create and reference templates instead of defining logic or configuration directly in pipelines or templates. |
| [ADOG-JOBS-004](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-validation-flag.md) | Jobs | CONSIDER: Add a `boolean` parameter to run a job in validation mode without changes. |
| [ADOG-JOBS-007](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-parameters-short.md) | Jobs | DO: Keep the number of environment-related parameters in job templates small. |
| [ADOG-JOBS-008](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-single-responsibility.md) | Jobs | DO: Focus each job on a single, well-defined responsibility. |
| [ADOG-PARAMETERS-001](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/consider-grouping.md) | Parameters | CONSIDER: Group related parameters, such as a username and password. |
| [ADOG-PARAMETERS-002](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/do-restrict-values.md) | Parameters | DO: Restrict parameter values when they have a well-defined set. |
| [ADOG-PIPELINES-001](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/pipelines/consider-validation.md) | Pipelines | CONSIDER: Add a parameter or condition to run the pipeline in validation mode. |
| [ADOG-STAGES-001](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/consider-grouping-jobs.md) | Stages | CONSIDER: Organize related jobs into stages. |
| [ADOG-STAGES-002](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/do-parallel-stages.md) | Stages | DO: Run independent stages in parallel. |
| [ADOG-STEPS-005](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-retries.md) | Steps | CONSIDER: Configure retries for tasks that face transient failures. |
| [ADOG-STEPS-008](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-use-service-connections.md) | Steps | DO: Use service connections to authenticate with external services. |
| [ADOG-STEPS-009](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-validate-parameters.md) | Steps | DO: Validate step parameters in templates and fail when a parameter is invalid. |
| [ADOG-VARIABLES-002](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-organize-variables.md) | Variables | DO: Organize variables by functionality, environment, or another logical partition. |
| [ADOG-VARIABLES-004](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-separate-configuration.md) | Variables | DO: Avoid hard-coding configuration values inside templates. |

These guidelines remain available through the guideline catalogue and the corresponding MCP lookup tools.

## Limitations

The analyzer is a static analysis tool. It does not execute pipelines, resolve every referenced template, inspect repository history, infer deployment intent, or determine whether a task is safe for a particular environment. A clean result means that no implemented local check found a violation; it does not prove that the pipeline follows every guideline.

For the detection model used by the manifest, see [How it works](how-it-works.md#detection-kinds). For the architectural decision about heuristic analysis, see [ADR-013](decisions.md#adr-013-heuristic-detection-rules-are-deferred-to-phase-2).

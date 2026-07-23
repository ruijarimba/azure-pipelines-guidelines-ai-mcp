# Guideline automation status

Not every Azure Pipelines guideline can be reliably enforced by a local tool. Some recommendations require repository-wide context, deployment intent, task semantics, or human judgment. Therefore, this document classifies each guideline according to what the analyzer can determine from the current YAML document alone.

This page gives people and AI agents the rule description, local automation status, and evaluation reason for every implemented guideline. It shows what the local analyzer can prove from one Azure Pipelines YAML document without external APIs or documentation lookup.

The companion [Azure Pipelines Guidelines repository](https://github.com/ruijarimba/azure-pipelines-guidelines) owns the machine-readable `data/guidelines.json` manifest. The descriptions and links in the tables below are copied from that manifest; when the companion repository changes, refresh this document from the current manifest before committing related changes.

## How analysis handles each status

| Status | Analyzer behavior | Use it for |
| --- | --- | --- | --- |
| `enforceable` | Runs by default | Deterministic YAML checks |
| `heuristic` | Runs only with `--include-heuristics` or MCP `includeHeuristics: true` | Optional advisory review |
| `notAutomatable` | Does not run | Human review with repository and deployment context |

For example, the analyzer cannot enforce [`ADOG-STEPS-008`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-use-service-connections.md) from YAML alone. A script might have no suitable task alternative, might not need a service connection, or might use a service connection through a template.

## General guidelines

| Guideline | Description | Status | Reason |
| --- | --- | --- | --- |
| [`ADOG-GENERAL-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-absolute-paths.md) | **CONSIDER:** Use absolute paths to reference stages, jobs, steps and variables templates. | `enforceable` | Template references can be checked for `/`- or `\`-prefixed paths directly in local YAML |
| [`ADOG-GENERAL-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-native-yaml-constructs.md) | **CONSIDER:** Prefer YAML‑native constructs to express values, logic, and scripts in a clear, consistent, and platform‑agnostic way. | `heuristic` | Quoted values can be valid YAML and valid task input |
| [`ADOG-GENERAL-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-schema-compatible-types.md) | **CONSIDER:** When adding parameters that map to Azure Pipelines YAML fields, use the same name, type, and a schema-compatible default value so templates work naturally without requiring every parameter to be explicitly set. | `heuristic` | Parameter names do not prove their intended schema field |
| [`ADOG-GENERAL-004`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-documentation.md) | **DO:** Document pipelines and templates. Add comments to the top of pipeline and template files. Describe the purpose, usage, and other relevant information clearly for both people and tools. | `enforceable` | A first non-empty comment is a deterministic local documentation policy |
| [`ADOG-GENERAL-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-folder-structure.md) | **DO:** Organize pipelines and templates logically and consistently across different projects and repositories. | `heuristic` | Shared template roots are repository conventions |
| [`ADOG-GENERAL-006`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-templates-everywhere.md) | **DO:** Create and reference templates instead of defining logic or configuration directly in your pipelines or templates. | `notAutomatable` | YAML alone cannot show whether inline logic should be reused |
| [`ADOG-GENERAL-007`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/donot-hard-code-values.md) | **DON'T:** Do not hard-code values in Azure DevOps pipelines and templates. | `heuristic` | Literal values can be intentional stable defaults |

## Job guidelines

| Guideline | Description | Status | Reason |
| --- | --- | --- | --- |
| [`ADOG-JOBS-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-explicit-checkout.md) | **CONSIDER:** Explicitly set `checkout` in every job to make source code checkout behavior clear and stable. | `enforceable` | Any non-empty checkout entry is directly identifiable in local YAML |
| [`ADOG-JOBS-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-grouping-tasks.md) | **CONSIDER:** Group job tasks into a single steps template, rather than using multiple steps templates in a job. | `enforceable` | All non-checkout steps count as job logic; only checkout steps are excluded |
| [`ADOG-JOBS-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-job-variables.md) | **CONSIDER:** Declare variables at the job level instead of the stage or root level. | `enforceable` | Pipeline- and stage-scope declarations are directly identifiable when jobs are present in local YAML |
| [`ADOG-JOBS-004`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-validation-flag.md) | **CONSIDER:** Add a `boolean` parameter to your job to run it in _validation mode_, without deploying or executing any changes. | `notAutomatable` | Only some jobs need a non-destructive validation mode |
| [`ADOG-JOBS-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-extensible-jobs.md) | **DO:** When creating job templates for reuse by different teams, stages, or pipelines, add parameters such as: | `heuristic` | YAML cannot prove that a job template is reused or needs each control |
| [`ADOG-JOBS-006`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-job-timeouts.md) | **DO:** Set job timeouts or add parameters to shared job templates to let users configure them. | `enforceable` | The parsed job node deterministically exposes `timeoutInMinutes` |
| [`ADOG-JOBS-007`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-parameters-short.md) | **DO:** When defining job templates in Azure DevOps pipelines, keep the number of **environment-related** parameters as short as possible. | `notAutomatable` | Environment parameter necessity depends on template consumers and policy |
| [`ADOG-JOBS-008`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-single-responsibility.md) | **DO:** Focus each job on a single, well-defined responsibility. | `notAutomatable` | Keyword matches cannot establish a job's responsibilities |

## Parameter guidelines

| Guideline | Description | Status | Reason |
| --- | --- | --- | --- |
| [`ADOG-PARAMETERS-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/consider-grouping.md) | **CONSIDER:** Group related parameters, such as username and password. | `notAutomatable` | Parameter grouping is an interface-design decision |
| [`ADOG-PARAMETERS-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/do-restrict-values.md) | **DO:** Restrict the values of parameters when they have a well-defined set. | `notAutomatable` | YAML cannot show whether a string has a finite valid value set |

## Pipeline guidelines

| Guideline | Description | Status | Reason |
| --- | --- | --- | --- |
| [`ADOG-PIPELINES-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/pipelines/consider-validation.md) | **CONSIDER:** Add a parameter or condition to run the pipeline in _validation mode_, skipping deployment or changes. | `notAutomatable` | Only deployment-capable pipelines need a validation mode |

## Stage guidelines

| Guideline | Description | Status | Reason |
| --- | --- | --- | --- |
| [`ADOG-STAGES-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/consider-grouping-jobs.md) | **CONSIDER:** Organize related jobs into stages to: | `notAutomatable` | YAML cannot show which top-level jobs are related |
| [`ADOG-STAGES-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/do-parallel-stages.md) | **DO:** Run independent stages in parallel. | `notAutomatable` | YAML cannot show whether stages are independent |

## Step guidelines

| Guideline | Description | Status | Reason |
| --- | --- | --- | --- |
| [`ADOG-STEPS-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/avoid-pipeline-variables.md) | **AVOID:** Avoid pipeline variables in steps templates. Use parameters instead. | `enforceable` | Macro syntax is directly identifiable in YAML |
| [`ADOG-STEPS-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-environment-variables.md) | **CONSIDER:** Set environment variables at the task level. | `heuristic` | Variable use does not prove a step-level environment mapping is appropriate |
| [`ADOG-STEPS-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-logging-diagnostics.md) | **CONSIDER:** Log enough diagnostic details required to troubleshoot issues and failures. | `heuristic` | Logging quality cannot be proven from selected command text |
| [`ADOG-STEPS-004`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-logging-diagnostics.md) | **CONSIDER:** Log enough diagnostic details required to troubleshoot issues and failures. | `heuristic` | Logging sufficiency is contextual and task-specific |
| [`ADOG-STEPS-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-retries.md) | **CONSIDER:** Configure the number of retries if a task faces transient failures. | `notAutomatable` | Retry suitability depends on operation idempotency and failure modes |
| [`ADOG-STEPS-006`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-timeouts.md) | **CONSIDER:** Set timeouts for tasks to avoid stalling pipeline runs. Provide reasonable values based on the expected execution time. | `heuristic` | Task-level timeouts are not appropriate for every task |
| [`ADOG-STEPS-007`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-extensible-steps.md) | **DO:** When building reusable templates, add these control parameters: | `enforceable` | Each supported control setting used by a local step-template reference can be checked against its parameters |
| [`ADOG-STEPS-008`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-use-service-connections.md) | **DO:** Use Service Connections to authenticate with external services (Azure, GitHub, Docker, Kubernetes, etc.). | `notAutomatable` | YAML cannot prove that a service-connection-capable task alternative exists or applies |
| [`ADOG-STEPS-009`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-validate-parameters.md) | **DO:** Validate step parameters in templates. Fail the pipeline if a parameter is invalid. | `notAutomatable` | Parameter validation needs depend on the template's accepted input domain |
| [`ADOG-STEPS-010`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-mix-syntax.md) | **DON'T:** Do not embed pipeline expressions (`$(...)` or `${{ ... }}`) throughout the body of a script task. Bind them at the boundary instead — either in the task-level `env:` block or as variable assignments at the very top of the script. | `enforceable` | Macro and template expression syntax is directly identifiable in local YAML step content |
| [`ADOG-STEPS-011`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-use-azurekeyvault-task.md) | **DON'T:** Do not run the `AzureKeyVault` task to pull secrets into pipeline variables. | `enforceable` | Azure Key Vault task references are directly identifiable in YAML |

## Variable guidelines

| Guideline | Description | Status | Reason |
| --- | --- | --- | --- |
| [`ADOG-VARIABLES-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/consider-read-only-variables.md) | **CONSIDER:** Mark variables as `readonly` when they shouldn't change after you initialize them. | `heuristic` | YAML cannot determine whether a variable should remain mutable |
| [`ADOG-VARIABLES-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-organize-variables.md) | **DO:** Organize your variables into folders by functionality, environment, or any logical partition that fits your project. | `notAutomatable` | Folder organization requires repository path context |
| [`ADOG-VARIABLES-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-sensitive-information.md) | **DO:** Store passwords, tokens, and keys inside [variable groups](https://learn.microsoft.com/en-us/azure/devops/pipelines/library/variable-groups?view=azure-devops&tabs=yaml). | `heuristic` | A secret-like name is not proof that an inline value is a secret |
| [`ADOG-VARIABLES-004`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-separate-configuration.md) | **DO:** Avoid hard-coding configuration values inside pipeline, step, job, or stage templates. | `notAutomatable` | Configuration ownership requires template and environment context |
| [`ADOG-VARIABLES-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-variable-scope.md) | **DO:** Restrict the scope of variables as much as possible. | `enforceable` | Pipeline- and stage-scope declarations are directly identifiable; job scope is allowed |
| [`ADOG-VARIABLES-006`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/donot-mix-environments.md) | **DON'T:** Do not define variables for multiple environments inside a single variable template. | `heuristic` | Environment naming and file purpose cannot be proven from one YAML file |

## Review skipped rules

Structured MCP responses include `skippedGuidelines` with each skipped rule ID, status, and reason. The `adog rules` commands also show the current local automation status. Use these details to decide whether a manual review or repository-specific policy is needed.

## Local structural policies

The following enforceable rules use explicit local policies:

- [`ADOG-JOBS-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-explicit-checkout.md) requires one or more checkout entries in every parsed job. Any non-empty checkout value is accepted.
- [`ADOG-JOBS-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-grouping-tasks.md) reports more than one non-checkout step in a job. Templates, tasks, scripts, and other non-checkout steps count as job logic; only checkout entries are excluded.
- [`ADOG-STEPS-007`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-extensible-steps.md) checks `condition`, `continueOnError`, `enabled`, `retryCountOnTaskFailure`, and `timeoutInMinutes`. Each setting used by a local step-template reference must be exposed through that template invocation's `parameters` block.
- [`ADOG-VARIABLES-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-variable-scope.md) reports pipeline- and stage-scope variables. Job-scope variables and variable-template documents are allowed.
- [`ADOG-GENERAL-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-absolute-paths.md) reports template references that do not start with `/` or `\`; `./` is treated as relative.
- [`ADOG-JOBS-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-job-variables.md) reports pipeline- or stage-scope variables when the document defines jobs. It remains separate from [`ADOG-VARIABLES-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-variable-scope.md) so category filtering can select either rule independently.
- [`ADOG-STEPS-010`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/donot-mix-syntax.md) reports `$(...)` and `${{ ... }}` expressions found in local step content. It detects syntax and does not infer whether every occurrence is harmful.

These checks inspect only the current YAML document. They do not resolve templates from other files or infer task behavior, repository intent, or deployment context. A variables-only document is treated as a variable template when its local file name identifies it as a variables template.

## Deferred classification review

The following rules remain deferred for a later analysis. Their current statuses are intentionally unchanged because local YAML alone does not yet provide a sufficiently reliable policy without additional repository conventions or semantic context:

- [`ADOG-GENERAL-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-native-yaml-constructs.md), [`ADOG-GENERAL-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/consider-schema-compatible-types.md), [`ADOG-GENERAL-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/do-folder-structure.md), and [`ADOG-GENERAL-007`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/general/donot-hard-code-values.md)
- [`ADOG-JOBS-004`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/consider-validation-flag.md), [`ADOG-JOBS-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-extensible-jobs.md), [`ADOG-JOBS-007`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-parameters-short.md), and [`ADOG-JOBS-008`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/jobs/do-single-responsibility.md)
- [`ADOG-PARAMETERS-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/consider-grouping.md), [`ADOG-PARAMETERS-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/parameters/do-restrict-values.md), and [`ADOG-PIPELINES-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/pipelines/consider-validation.md)
- [`ADOG-STAGES-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/consider-grouping-jobs.md) and [`ADOG-STAGES-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/stages/do-parallel-stages.md)
- [`ADOG-STEPS-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-environment-variables.md), [`ADOG-STEPS-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-logging-diagnostics.md), [`ADOG-STEPS-004`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-logging-diagnostics.md), [`ADOG-STEPS-005`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-retries.md), [`ADOG-STEPS-008`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-use-service-connections.md), and [`ADOG-STEPS-009`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/do-validate-parameters.md)
- [`ADOG-VARIABLES-001`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/consider-read-only-variables.md), [`ADOG-VARIABLES-002`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-organize-variables.md), [`ADOG-VARIABLES-003`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-sensitive-information.md), and [`ADOG-VARIABLES-004`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/do-separate-configuration.md)

[`ADOG-STEPS-006`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/steps/consider-timeouts.md) remains heuristic because making task timeouts mandatory would be too aggressive. [`ADOG-VARIABLES-006`](https://github.com/ruijarimba/azure-pipelines-guidelines/blob/main/guidelines/variables/donot-mix-environments.md) remains heuristic because environment detection depends on repository naming conventions.

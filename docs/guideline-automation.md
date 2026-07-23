# Guideline automation status

This page gives people and AI agents the rule description, local automation status, and evaluation reason for every implemented guideline. It shows what the local analyzer can prove from one Azure Pipelines YAML document without external APIs or documentation lookup.

## How analysis handles each status

| Status | Analyzer behavior | Use it for |
| --- | --- | --- | --- |
| `enforceable` | Runs by default | Deterministic YAML checks |
| `heuristic` | Runs only with `--include-heuristics` or MCP `includeHeuristics: true` | Optional advisory review |
| `notAutomatable` | Does not run | Human review with repository and deployment context |

For example, the analyzer cannot enforce `ADOG-STEPS-008` from YAML alone. A script might have no suitable task alternative, might not need a service connection, or might use a service connection through a template.

## General guidelines

| Rule | Description | Status | Reason |
| --- | --- | --- | --- |
| `ADOG-GENERAL-001` | Use absolute paths to reference stages, jobs, steps and variables templates. | `enforceable` | Template references can be checked for `/`- or `\`-prefixed paths directly in local YAML |
| `ADOG-GENERAL-002` | Prefer YAML-native constructs to express values, logic, and scripts in a clear, consistent, and platform-agnostic way. | `heuristic` | Quoted values can be valid YAML and valid task input |
| `ADOG-GENERAL-003` | When adding parameters that map to Azure Pipelines YAML fields, use the same name, type, and a schema-compatible default value so templates work naturally without requiring every parameter to be explicitly set. | `heuristic` | Parameter names do not prove their intended schema field |
| `ADOG-GENERAL-004` | Document pipelines and templates. Add comments to the top of pipeline and template files. Describe the purpose, usage, and other relevant information clearly for both people and tools. | `enforceable` | A first non-empty comment is a deterministic local documentation policy |
| `ADOG-GENERAL-005` | Organize pipelines and templates logically and consistently across different projects and repositories. | `heuristic` | Shared template roots are repository conventions |
| `ADOG-GENERAL-006` | Create and reference templates instead of defining logic or configuration directly in your pipelines or templates. | `notAutomatable` | YAML alone cannot show whether inline logic should be reused |
| `ADOG-GENERAL-007` | Do not hard-code values in Azure DevOps pipelines and templates. | `heuristic` | Literal values can be intentional stable defaults |

## Job guidelines

| Rule | Description | Status | Reason |
| --- | --- | --- | --- |
| `ADOG-JOBS-001` | Explicitly set `checkout` in every job to make source code checkout behavior clear and stable. | `enforceable` | Any non-empty checkout entry is directly identifiable in local YAML |
| `ADOG-JOBS-002` | Group job tasks into a single steps template, rather than using multiple steps templates in a job. | `enforceable` | All non-checkout steps count as job logic; only checkout steps are excluded |
| `ADOG-JOBS-003` | Declare variables at the job level instead of the stage or root level. | `enforceable` | Pipeline- and stage-scope declarations are directly identifiable when jobs are present in local YAML |
| `ADOG-JOBS-004` | Add a `boolean` parameter to your job to run it in _validation mode_, without deploying or executing any changes. | `notAutomatable` | Only some jobs need a non-destructive validation mode |
| `ADOG-JOBS-005` | When creating job templates for reuse by different teams, stages, or pipelines, add parameters such as: | `heuristic` | YAML cannot prove that a job template is reused or needs each control |
| `ADOG-JOBS-006` | Set job timeouts or add parameters to shared job templates to let users configure them. | `enforceable` | The parsed job node deterministically exposes `timeoutInMinutes` |
| `ADOG-JOBS-007` | When defining job templates in Azure DevOps pipelines, keep the number of **environment-related** parameters as short as possible. | `notAutomatable` | Environment parameter necessity depends on template consumers and policy |
| `ADOG-JOBS-008` | Focus each job on a single, well-defined responsibility. | `notAutomatable` | Keyword matches cannot establish a job's responsibilities |

## Parameter guidelines

| Rule | Description | Status | Reason |
| --- | --- | --- | --- |
| `ADOG-PARAMETERS-001` | Group related parameters, such as username and password. | `notAutomatable` | Parameter grouping is an interface-design decision |
| `ADOG-PARAMETERS-002` | Restrict the values of parameters when they have a well-defined set. | `notAutomatable` | YAML cannot show whether a string has a finite valid value set |

## Pipeline guidelines

| Rule | Description | Status | Reason |
| --- | --- | --- | --- |
| `ADOG-PIPELINES-001` | Add a parameter or condition to run the pipeline in _validation mode_, skipping deployment or changes. | `notAutomatable` | Only deployment-capable pipelines need a validation mode |

## Stage guidelines

| Rule | Description | Status | Reason |
| --- | --- | --- | --- |
| `ADOG-STAGES-001` | Organize related jobs into stages to: | `notAutomatable` | YAML cannot show which top-level jobs are related |
| `ADOG-STAGES-002` | Run independent stages in parallel. | `notAutomatable` | YAML cannot show whether stages are independent |

## Step guidelines

| Rule | Description | Status | Reason |
| --- | --- | --- | --- |
| `ADOG-STEPS-001` | Avoid pipeline variables in steps templates. Use parameters instead. | `enforceable` | Macro syntax is directly identifiable in YAML |
| `ADOG-STEPS-002` | Set environment variables at the task level. | `heuristic` | Variable use does not prove a step-level environment mapping is appropriate |
| `ADOG-STEPS-003` | Log enough diagnostic details required to troubleshoot issues and failures. | `heuristic` | Logging quality cannot be proven from selected command text |
| `ADOG-STEPS-004` | Log enough diagnostic details required to troubleshoot issues and failures. | `heuristic` | Logging sufficiency is contextual and task-specific |
| `ADOG-STEPS-005` | Configure the number of retries if a task faces transient failures. | `notAutomatable` | Retry suitability depends on operation idempotency and failure modes |
| `ADOG-STEPS-006` | Set timeouts for tasks to avoid stalling pipeline runs. Provide reasonable values based on the expected execution time. | `heuristic` | Task-level timeouts are not appropriate for every task |
| `ADOG-STEPS-007` | When building reusable templates, add these control parameters: | `enforceable` | Each supported control setting used by a local step-template reference can be checked against its parameters |
| `ADOG-STEPS-008` | Use Service Connections to authenticate with external services (Azure, GitHub, Docker, Kubernetes, etc.). | `notAutomatable` | YAML cannot prove that a service-connection-capable task alternative exists or applies |
| `ADOG-STEPS-009` | Validate step parameters in templates. Fail the pipeline if a parameter is invalid. | `notAutomatable` | Parameter validation needs depend on the template's accepted input domain |
| `ADOG-STEPS-010` | Do not embed pipeline expressions (`$(...)` or `${{ ... }}`) throughout the body of a script task. Bind them at the boundary instead—either in the task-level `env:` block or as variable assignments at the very top of the script. | `enforceable` | Macro and template expression syntax is directly identifiable in local YAML step content |
| `ADOG-STEPS-011` | Do not run the `AzureKeyVault` task to pull secrets into pipeline variables. | `enforceable` | Azure Key Vault task references are directly identifiable in YAML |

## Variable guidelines

| Rule | Description | Status | Reason |
| --- | --- | --- | --- |
| `ADOG-VARIABLES-001` | Mark variables as `readonly` when they shouldn't change after you initialize them. | `heuristic` | YAML cannot determine whether a variable should remain mutable |
| `ADOG-VARIABLES-002` | Organize your variables into folders by functionality, environment, or any logical partition that fits your project. | `notAutomatable` | Folder organization requires repository path context |
| `ADOG-VARIABLES-003` | Store passwords, tokens, and keys inside [variable groups](https://learn.microsoft.com/en-us/azure/devops/pipelines/library/variable-groups?view=azure-devops&tabs=yaml). | `heuristic` | A secret-like name is not proof that an inline value is a secret |
| `ADOG-VARIABLES-004` | Avoid hard-coding configuration values inside pipeline, step, job, or stage templates. | `notAutomatable` | Configuration ownership requires template and environment context |
| `ADOG-VARIABLES-005` | Restrict the scope of variables as much as possible. | `enforceable` | Pipeline- and stage-scope declarations are directly identifiable; job scope is allowed |
| `ADOG-VARIABLES-006` | Do not define variables for multiple environments inside a single variable template. | `heuristic` | Environment naming and file purpose cannot be proven from one YAML file |

## Review skipped rules

Structured MCP responses include `skippedGuidelines` with each skipped rule ID, status, and reason. The `adog rules` commands also show the current local automation status. Use these details to decide whether a manual review or repository-specific policy is needed.

## Local structural policies

The following enforceable rules use explicit local policies:

- `ADOG-JOBS-001` requires one or more checkout entries in every parsed job. Any non-empty checkout value is accepted.
- `ADOG-JOBS-002` reports more than one non-checkout step in a job. Templates, tasks, scripts, and other non-checkout steps count as job logic; only checkout entries are excluded.
- `ADOG-STEPS-007` checks `condition`, `continueOnError`, `enabled`, `retryCountOnTaskFailure`, and `timeoutInMinutes`. Each setting used by a local step-template reference must be exposed through that template invocation's `parameters` block.
- `ADOG-VARIABLES-005` reports pipeline- and stage-scope variables. Job-scope variables and variable-template documents are allowed.

These checks inspect only the current YAML document. They do not resolve templates from other files or infer task behavior, repository intent, or deployment context. A variables-only document is treated as a variable template when its local file name identifies it as a variables template.

## Deferred classification review

The following rules remain deferred for a later analysis. Their current statuses are intentionally unchanged because local YAML alone does not yet provide a sufficiently reliable policy without additional repository conventions or semantic context:

- `ADOG-GENERAL-002`, `ADOG-GENERAL-003`, `ADOG-GENERAL-005`, and `ADOG-GENERAL-007`
- `ADOG-JOBS-004`, `ADOG-JOBS-005`, `ADOG-JOBS-007`, and `ADOG-JOBS-008`
- `ADOG-PARAMETERS-001`, `ADOG-PARAMETERS-002`, and `ADOG-PIPELINES-001`
- `ADOG-STAGES-001` and `ADOG-STAGES-002`
- `ADOG-STEPS-002`, `ADOG-STEPS-003`, `ADOG-STEPS-004`, `ADOG-STEPS-005`, `ADOG-STEPS-008`, and `ADOG-STEPS-009`
- `ADOG-VARIABLES-001`, `ADOG-VARIABLES-002`, `ADOG-VARIABLES-003`, and `ADOG-VARIABLES-004`

`ADOG-STEPS-006` remains heuristic because making task timeouts mandatory would be too aggressive. `ADOG-VARIABLES-006` remains heuristic because environment detection depends on repository naming conventions.

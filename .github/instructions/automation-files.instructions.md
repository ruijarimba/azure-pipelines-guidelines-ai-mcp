---
applyTo: "{Dockerfile,compose.yaml,compose.yml,**/*.ps1,**/*.sh,**/*.cmd,**/*.bat,**/*.yml,**/*.yaml,**/*.json,**/*.toml,**/*.props,**/*.targets,**/*.config,**/docker-compose*.yml,**/docker-compose*.yaml}"
---

# Automation file documentation

These rules apply to imperative and declarative automation files that configure, package,
run, or validate the repository. They cover shell scripts, PowerShell scripts, Dockerfiles,
Compose files, build configuration files, and similar workflow definitions.

## Purpose

Automation files are often the first place a maintainer looks when something breaks. Good
comments make the intent obvious, reduce the chance of accidental edits, and help contributors
understand why a command, flag, or configuration exists.

## Required documentation

Every automation file must include enough comments to answer the following questions:

- What is this file for?
- What is the main execution path or deployment path?
- Which environment values, ports, or credentials are expected?
- Which assumptions or prerequisites matter for a human maintainer?

## Comment rules

- Add a short header comment near the top of the file that explains the file's purpose.
- Add comments for non-obvious behaviour, especially around:
  - Docker build and runtime defaults
  - environment variable handling
  - transport or port selection
  - conditional logic that depends on platform, Docker, or WSL
  - commands that have side effects such as build, push, run, or cleanup
- Prefer comments that explain the reason for the choice, not just the syntax.
- Keep comments concise and useful. Avoid repeating what the code already says clearly.
- If a file has multiple related sections, add short section comments to separate them.

## File-specific expectations

### Dockerfiles

- Explain the build stage purpose and the runtime behaviour of the final image.
- Note any non-obvious defaults such as transport, port, or user context.
- Explain why a command or environment variable is set.

### Docker Compose files

- Explain which service is defined and how it is intended to be used.
- Note the default transport, port mapping, and any runtime assumptions.
- Explain whether credentials or secrets come from the environment or are intentionally excluded.

### Scripts

- Document the script's primary goal and the expected environment.
- Add comments around prerequisites, Docker or platform checks, output, and cleanup steps.
- Document any destructive or state-changing actions such as stopping containers or deleting build artefacts.

## What to avoid

- Empty files with no explanation.
- Comments that merely restate the command in plain English.
- Placeholder comments that do not add meaning.
- Large comment blocks that duplicate the script contents.
- GitHub Actions workflow files, GitHub-specific CI/CD automation, or other GitHub-hosted pipeline definitions unless the human explicitly asks for them.

## When to update comments

Update comments whenever you change the behaviour, prerequisites, default values, transport mode,
ports, or automation flow of a file.

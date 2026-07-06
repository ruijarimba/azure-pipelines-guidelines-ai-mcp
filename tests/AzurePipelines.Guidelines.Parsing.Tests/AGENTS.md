# AGENTS.md — AzurePipelines.Guidelines.Parsing.Tests

## Purpose

Unit tests for `AzurePipelines.Guidelines.Parsing` — YAML → AST transformation.

## What gets tested here

- **Valid YAML** → correct `PipelineDocument` structure (triggers, stages, jobs, steps, variables).
- **Malformed YAML** → `PipelineParsingException` with a meaningful message.
- **Empty / minimal YAML** → valid parse result with no optional elements.
- **Edge cases**: null keys, duplicate keys, unrecognised top-level keys, deeply nested structures.

## Test naming

`Parse_GivenValidPipeline_ShouldReturnPipelineDocument`
`Parse_GivenMalformedYaml_ShouldThrowPipelineParsingException`
`Parse_GivenEmptyDocument_ShouldReturnEmptyPipeline`

## Coverage expectations

- All YAML structural paths: triggers, pool, stages, jobs, steps, variables, parameters.
- All error paths: syntax errors, schema violations, unrecognised keys.
- Boundary cases: empty strings, whitespace-only, single-line vs multi-line blocks.

## Test data

Store sample YAML strings inline or use embedded resources for complex fixtures.
Never load YAML from the file system at test runtime — tests must be hermetic.

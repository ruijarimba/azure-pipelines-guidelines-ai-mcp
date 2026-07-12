---
applyTo: "**"
---

# Solution file management

This file explains how to add new files to the Visual Studio solution so they appear in
Solution Explorer with the correct folder hierarchy.

---

## Rule: All files must be visible in Solution Explorer

**Every file that belongs to this repository must be visible in Visual Studio Solution Explorer.**

Non-code files (`.md`, `.yml`, `.json`, `.props`, configuration files, etc.) are not
automatically picked up by the .NET SDK build system. You must register them explicitly.

---

## Where to register files

The registration location depends on where the file lives in the filesystem:

| File location | Registration method | Example |
| --- | --- | --- |
| **Inside a project directory** | Add `<None Include="filename" />` in the project's `.csproj` | `src/Core/AGENTS.md` → edit `src/Core/Core.csproj` |
| **Anywhere else** (root, `docs/`, `.github/`, `tests/`, `src/`, `tools/`) | Add `<File Path="relative/path/to/file" />` in `AzurePipelinesGuidelines.slnx` | `.github/copilot-instructions.md` → edit `AzurePipelinesGuidelines.slnx` |

---

## Registration examples

### Example 1: Project-level file registration

If you create `src/Core/AGENTS.md`, add this to `src/Core/AzurePipelines.Guidelines.Core.csproj`:

```xml
<ItemGroup>
  <None Include="AGENTS.md" />
</ItemGroup>
```

### Example 2: Solution-level file registration

If you create `.github/instructions/new-instruction.instructions.md`, add this to
`AzurePipelinesGuidelines.slnx`:

```xml
<Folder Name="/.github/">
  <Folder Name="/.github/instructions/">
    <File Path=".github/instructions/agent-behaviour.instructions.md" />
    <File Path=".github/instructions/new-instruction.instructions.md" />  <!-- NEW -->
  </Folder>
</Folder>
```

---

## Folder hierarchy rules

### Rule 1: Mirror the filesystem exactly

The Solution Explorer hierarchy **must match Windows Explorer exactly**:

✅ **Correct:**
```
/.github/
  ├── copilot-instructions.md
  └── /instructions/
      ├── agent-behaviour.instructions.md
      └── testing.instructions.md
```

❌ **Incorrect (flattened nested directory):**
```
/.github/
  ├── copilot-instructions.md
  ├── agent-behaviour.instructions.md  ← should be inside /instructions/
  └── testing.instructions.md           ← should be inside /instructions/
```

### Rule 2: Create parent folders before adding children

If you create a new subdirectory in the filesystem, you must:
1. Create a matching `<Folder Name="/parent/child/">` entry in `.slnx` first
2. Add `<File Path="..." />` entries as children inside that folder

**Example:** Creating `.github/examples/example.md`:

```xml
<Folder Name="/.github/">
  <Folder Name="/.github/instructions/">
    <!-- existing instruction files -->
  </Folder>
  <Folder Name="/.github/examples/">  <!-- NEW FOLDER FIRST -->
    <File Path=".github/examples/example.md" />  <!-- THEN FILE -->
  </Folder>
</Folder>
```

### Rule 3: Never flatten nested directories

Even if it "looks cleaner" in Solution Explorer, always preserve the full nesting:

- Files in `docs/adr/` go under `/docs/` → `/docs/adr/` — not flat under `/docs/`
- Files in `.github/instructions/` go under `/.github/` → `/.github/instructions/` — not flat under `/.github/`

---

## Verification checklist

After creating or registering any file, run this mental check:

1. ✅ Does Solution Explorer show this file?
2. ✅ Is it in the same relative location as Windows Explorer?
3. ✅ Are all parent folders visible as nested solution folders?
4. ✅ Can a human reviewer find this file by navigating the Solution Explorer hierarchy?

If the answer to **any** question is "no", the registration is incorrect — fix it before committing.

---

## Why this matters

Files invisible to the IDE are invisible to human reviewers. This rule supports the
**Human authority** principle ([agent-behaviour.instructions.md](agent-behaviour.instructions.md),
Rule 2) by ensuring reviewers can see and oversee all agent-generated content.

---

## Tool

When you create a new file, the agent should:
1. Create the file using `create_file`
2. Immediately register it in the appropriate `.csproj` or `.slnx` file using `replace_string_in_file`
3. Verify visibility before moving to the next task

Never defer registration to a later commit or "cleanup" step.

---

## Reference

This is a summary of Rule 9 from
[`.github/instructions/agent-behaviour.instructions.md`](agent-behaviour.instructions.md).
If there is a conflict, the agent-behaviour file takes precedence.

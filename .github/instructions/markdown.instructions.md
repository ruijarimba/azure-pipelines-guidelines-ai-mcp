---
applyTo: "**/*.md"
---

# Markdown writing rules

These rules apply to all Markdown files in this repository — `AGENTS.md`, `docs/`, instruction
files, prompt files, and `README.md`. The target audience includes non-native English speakers,
so clarity always takes priority over elegance.

> **Before changing any rule in this file:** re-read the reference sources recorded in
> [`docs/decisions.md` — ADR-011](../../docs/decisions.md) and update that ADR if the
> rationale changes.

---

## 1. Plain English first

Write so that a non-native English speaker with intermediate proficiency can understand the
text without a dictionary.

- Use common, everyday words. Write "use" not "utilise". Write "show" not "demonstrate".
- Use short sentences. Aim for 25 words or fewer per sentence.
- Use active voice. Write "the agent creates a file" not "a file is created by the agent".
- Address the reader directly with "you" where it makes sense.
- One idea per sentence. If a sentence contains two ideas joined by "and", split it.
- Avoid idioms and culturally specific expressions. Write "this is difficult" not "this is
  a tall order".
- Common Latin abbreviations (`e.g.`, `i.e.`, `etc.`) are allowed — they are widely understood
  in technical writing. Prefer the plain-English form ("for example", "that is", "and so on")
  when it reads more clearly, but neither form is wrong.

*Inspiration: Microsoft Writing Style Guide, GOV.UK Content Design guide,
plainlanguage.gov guidelines.*

---

## 2. Acronyms and technical terms

- Spell out an acronym the first time you use it on a page, then use the short form.
  Example: `Model Context Protocol (MCP)` on first use, then `MCP`.
- Do not assume the reader knows domain-specific terms. Link to `docs/glossary.md` on
  first use of any term defined there.
- Do not invent new abbreviations. Use the terms defined in `docs/glossary.md`.

---

## 3. Headings

- Use sentence case: "Dependency graph" not "Dependency Graph".
- Do not skip heading levels. Follow `#` → `##` → `###` in order.
- Each heading must describe the content below it in plain terms.
- Do not use headings as emphasis for a single sentence — use bold text instead.
- Maximum heading depth: `###` (three levels). Deeper nesting is a signal to split the
  document.

---

## 4. Lists

- Use a bulleted list (`-`) for items with no natural order.
- Use a numbered list (`1.`) only when order matters (steps, sequences).
- Keep list items parallel: start each item with the same part of speech (all nouns, all
  verbs, all sentences).
- Do not nest lists more than two levels deep.
- Do not end list items with a semicolon. End with a period only if the item is a full
  sentence; otherwise leave it without punctuation.

---

## 5. Tables

- Use tables only for genuinely tabular data (two or more attributes per row).
- Every table must have a header row.
- Keep cell content short. If a cell needs more than ~15 words, consider using a list or
  prose instead.
- Align pipe characters consistently. Leave one space inside each cell delimiter.

---

## 6. Code blocks and inline code

- Use a fenced code block (triple backtick) for any code, command, or file content longer
  than one line. Always specify the language identifier: ` ```csharp `, ` ```yaml `,
  ` ```bash `, ` ```markdown `.
- Use inline backticks for: file names, paths, command names, type names, property names,
  and any literal value the reader might type or copy.
- Do not use inline code for emphasis. Use **bold** for emphasis.

---

## 7. Links

- Use descriptive link text. Write `[docs/glossary.md](../../docs/glossary.md)` or
  `[ADR-011](../../docs/decisions.md)` instead of generic text such as "click here".
- Prefer relative links for files within this repository so they work offline and in forks.
- Check that every link target exists before committing.

---

## 8. Document structure

- Start every document with a single `#` heading that matches the file's purpose.
- Follow the heading with one or two sentences that tell the reader what the document
  contains and who it is for.
- Place the most important information first. Do not bury the key point in the middle.
- End a document with a clear stopping point. Do not trail off with open questions or
  placeholder sections.

---

## 9. Tone and length

- Be direct. Say what you mean in the fewest words that are still clear.
- Do not pad text with phrases like "it is worth noting that", "as mentioned above", or
  "needless to say".
- Do not write more than is needed. An instruction file that covers one topic clearly in
  30 lines is better than one that covers it vaguely in 100 lines.
- Avoid hedging language ("might", "could", "perhaps") unless you are genuinely uncertain.
  If you are uncertain, say so explicitly: "I am not sure whether…".

---

## 10. What to avoid

- Passive voice when active voice is possible.
- Long paragraphs (more than 5 sentences). Break them into shorter paragraphs or a list.
- Synonyms used for variety ("agents", "bots", "assistants" meaning the same thing).
  Pick one term from `docs/glossary.md` and use it consistently.
- Rhetorical questions as headings or section openers.
- Emoji in technical documentation. They are not universally rendered and add no meaning.
- HTML inside Markdown files unless absolutely necessary for layout that Markdown cannot
  express.

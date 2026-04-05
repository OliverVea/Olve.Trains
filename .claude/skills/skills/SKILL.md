---
name: skills
description: Reference for writing and maintaining Claude Code skills — file structure, frontmatter fields, progressive disclosure, scripts, best practices, and the Agent Skills open standard. Use when creating new skills, updating existing skills, or understanding how skills work.
user-invocable: false
---

# Skills Reference

Skills are structured instructions that extend Claude Code's capabilities. They follow the [Agent Skills open standard](https://agentskills.io/specification) adopted by 30+ tools (Claude Code, Cursor, VS Code/Copilot, Gemini CLI, JetBrains Junie, etc.).

## File Structure

```
.claude/skills/<skill-name>/
├── SKILL.md          # Required: YAML frontmatter + markdown instructions
├── scripts/          # Optional: executable scripts (bash, python)
└── references/       # Optional: additional documentation files
```

| Scope     | Path                                   | Applies to        |
|-----------|----------------------------------------|--------------------|
| Project   | `.claude/skills/<name>/SKILL.md`       | This project only  |
| Personal  | `~/.claude/skills/<name>/SKILL.md`     | All your projects  |

## SKILL.md Format

```yaml
---
name: my-skill                # Required. Must match directory name.
description: >-               # Required. What it does AND when to use it.
  Description here. Use when...
user-invocable: false          # Optional. false = Claude-only (reference skills).
context: fork                  # Optional. fork = run in isolated subagent.
agent: Explore                 # Optional. Subagent type when context: fork.
---

# Skill Title

Markdown body with instructions.
```

### Frontmatter Fields

#### Required

| Field         | Constraints |
|---------------|-------------|
| `name`        | 1-64 chars. Lowercase `a-z`, digits `0-9`, hyphens only. No leading/trailing/consecutive hyphens. Must match parent directory name. |
| `description` | 1-1024 chars. Front-load the key use case (truncated at ~250 chars in listings). Include both what the skill does AND when to trigger it. Write in third person. |

#### Optional (Claude Code extensions)

| Field                      | Default | Description |
|----------------------------|---------|-------------|
| `user-invocable`           | `true`  | `false` hides from `/` menu; only Claude auto-loads it. Use for reference skills. |
| `disable-model-invocation` | `false` | `true` prevents Claude from auto-loading; user must invoke via `/name`. |
| `context`                  | —       | `fork` runs in isolated subagent. `conversation` runs in main context. |
| `agent`                    | —       | Subagent type when `context: fork` (e.g. `Explore`, `Plan`, `general-purpose`). |
| `argument-hint`            | —       | Hint shown during autocomplete, e.g. `[issue-number]`. |
| `model`                    | —       | Model override when skill is active. |
| `effort`                   | —       | `low`, `medium`, `high`, `max`. |
| `paths`                    | —       | Glob patterns limiting auto-activation to matching files. |
| `shell`                    | `bash`  | `bash` or `powershell`. |
| `allowed-tools`            | —       | Space-delimited list of pre-approved tools. |

### String Substitutions

| Variable               | Description |
|------------------------|-------------|
| `$ARGUMENTS`           | All arguments passed when invoking |
| `$ARGUMENTS[N]` / `$N`| Specific argument by 0-based index |
| `${CLAUDE_SKILL_DIR}`  | Directory containing the SKILL.md |
| `${CLAUDE_SESSION_ID}` | Current session ID |

### Dynamic Context

`` !`<command>` `` runs a shell command before sending content to Claude. Output replaces the placeholder.

Multi-line form:
````
```!
command here
```
````

## Progressive Disclosure

Skills load in three tiers to minimize token cost:

| Level | When Loaded           | Budget          | Content |
|-------|-----------------------|-----------------|---------|
| 1     | Always (startup)      | ~50-100 tokens  | `name` + `description` from frontmatter |
| 2     | When triggered        | <5000 tokens    | Full SKILL.md body |
| 3     | As needed             | Unlimited       | Bundled files (scripts/, references/) |

**Implication:** Many skills can be installed with minimal cost. Only triggered skills consume significant tokens. Keep SKILL.md body under 500 lines; move detailed reference to separate files.

## Scripts

Place executable scripts in `scripts/`. These are Level 3 content — only loaded when Claude reads them during execution.

```
.claude/skills/my-skill/scripts/
├── build.sh           # Bash scripts for automation
├── validate.py        # Python scripts for validation
└── setup.sh           # Setup/initialization scripts
```

Guidelines:
- Scripts should be self-contained with helpful error messages
- Handle errors explicitly (don't punt to Claude)
- Document magic numbers and non-obvious parameters
- Make clear in SKILL.md whether Claude should **execute** or **read** a script

## Skill Types

### Reference skills (Claude-only)

Background knowledge loaded automatically when relevant. Most skills in this project use this pattern.

```yaml
user-invocable: false
```

Claude reads these when the description matches the current task. Examples: `testing`, `events`, `scenes`.

### User-invocable skills

Triggered by user via `/skill-name` in the prompt. Default behavior.

```yaml
# user-invocable: true is the default, can omit
argument-hint: "[args]"       # optional autocomplete hint
```

Examples: `create-pr`, `pick-todos`.

### Forked skills

Run in an isolated subagent to protect main context from large outputs.

```yaml
context: fork
agent: Explore                # or Plan, general-purpose
```

## Best Practices

### Descriptions

- Write in third person: "Processes Excel files..." not "I can help you..."
- Include both **what** it does and **when** to use it
- Include specific keywords for matching (e.g. "pytest, screenshot comparison, game pool")
- Front-load the key use case — only first ~250 chars shown in listings

### Naming

- Use descriptive kebab-case: `asset-pipeline`, `run-game`, `pick-todos`
- Avoid vague names: `helper`, `utils`, `tools`

### Content

- Be concise. Only add context Claude doesn't already have.
- **Write current state, not change history.** Describe how things work *now*. Never leave behind references to how things used to work — just replace the old description with the new one.
  - Bad: "Bar used to be a child of Foo requiring a FooConfig, but it has been changed to require a BarConfig."
  - Good: "Bar requires a BarConfig."
- Use consistent terminology throughout
- Structure longer files with clear sections and tables
- Avoid time-sensitive information
- Always use forward slashes in paths, even on Windows

### Degrees of Freedom

| Level  | When to use | Format |
|--------|-------------|--------|
| High   | Multiple valid approaches | Text instructions |
| Medium | Preferred pattern exists | Pseudocode, parameterized scripts |
| Low    | Fragile/critical operations | Exact scripts |

### Keeping Skills Current

Skills must always reflect the current codebase. When making changes that affect a skill (renaming commands, changing APIs, modifying workflows), update the skill in the same commit. Stale skills cause incorrect code.

## References

- [Agent Skills Specification](https://agentskills.io/specification)
- [Anthropic Skills Repository](https://github.com/anthropics/skills)
- [Claude Code Skills Docs](https://platform.claude.com/docs/en/agents-and-tools/agent-skills/overview)
- [The Complete Guide to Building Skills for Claude (PDF)](https://resources.anthropic.com/hubfs/The-Complete-Guide-to-Building-Skill-for-Claude.pdf)

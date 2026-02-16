---
name: pick-todos
description: Pick simple tasks from README TODOs, investigate them, and present options. Use when user asks to "pick todos", "find simple tasks", "what should I work on", or "suggest tasks".
context: fork
agent: Explore
---

# Pick TODOs Task

Read the project README.md and find all uncompleted TODO items (lines starting with `- [ ]`).

## Select 3 Simple Tasks

Pick **3 tasks** that appear relatively simple:
- Clear, focused scope (single feature or fix)
- Not part of a large epic with many dependencies
- BUG fixes are often good candidates
- UI additions tend to be straightforward

Avoid:
- Tasks saying "basic support for X" (foundational work)
- Tasks involving multiple systems
- Procedural generation or complex algorithms

## Investigate Each Task

For each of the 3 tasks, briefly search the codebase:
- Find where related code exists
- Identify patterns already in place
- Assess how many files need modification
- Note any blockers or prerequisites

## Present Results

Output in this format:

## Task Options

### 1. [Task Name]
**From:** [Epic name if applicable]
**Complexity:** Low/Medium
**Summary:** [1-2 sentences]
**Key files:** [Main files to modify]
**Next steps:**
- Step 1
- Step 2

### 2. [Task Name]
...

### 3. [Task Name]
...

---

Be honest about complexity. If a task turns out harder than expected, swap it for another.

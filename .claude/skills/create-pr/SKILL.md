---
name: create-pr
description: Create a GitHub PR from uncommitted or unpushed changes. Use when user asks to "create a PR", "make a pull request", "open a PR", or "submit changes".
context: conversation
---

# Create Pull Request

## 1. Understand Changes

Run in parallel:
- `git fetch origin && git status`
- `git diff` and `git diff --cached`
- `git log origin/master..HEAD --oneline`

Review the changes. If they seem unrelated or include local preferences (window sizes, IDE settings), ask the user what to include.

## 2. Create Branch and Commit

```bash
git checkout -b feature/<description>
git add <specific-files>
git commit -m "$(cat <<'EOF'
<type>: <short description>

Generated with [Claude Code](https://claude.ai/code)
via [Happy](https://happy.engineering)

Co-Authored-By: Claude <noreply@anthropic.com>
Co-Authored-By: Happy <yesreply@happy.engineering>
EOF
)"
```

## 3. Push and Create PR

```bash
git push -u origin <branch-name>

gh pr create --title "<title>" --body "$(cat <<'EOF'
## Summary
- <changes>

## Test plan
- [ ] <test items>

🤖 Generated with [Claude Code](https://claude.ai/code)
EOF
)"
```

Return the PR URL when complete.

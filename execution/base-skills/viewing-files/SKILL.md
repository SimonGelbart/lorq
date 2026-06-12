---
name: viewing-files
description: View selected file sections with bat. Use for syntax-highlighted source reads, line-numbered ranges, Git-aware previews, or fzf previews while avoiding full-file context loads.
---

# View Files with bat

Read only the section needed:

```bash
bat --line-range START:END --style=numbers FILE
```

Use `sed -n 'START,ENDp' FILE` when plain output is smaller or `bat` is unavailable. Use `cat` only for tiny files or concatenation. For agent-visible output, avoid color and decorative headers unless they add value.

Do not read an entire source file over 200 lines unless the task explicitly requires whole-file review.
Prefer symbol/range discovery before reading implementation bodies.

Read [the bat reference](./reference/bat-guide.md) for Git, paging, and preview integration.

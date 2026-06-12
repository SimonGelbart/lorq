# Minimal-Context Workflow Recipes

Use the smallest workflow that answers the task. Each arrow means "feed the narrowed result into the next decision or command", not necessarily a literal shell pipe.

## Selection Rules

- Start with the user's question, not a preferred tool.
- Narrow paths before reading content.
- Use structure extraction before reading large unfamiliar source files.
- Preview broad replacements before applying them.
- Verify with a targeted query after edits.
- Keep agent workflows non-interactive. Use `fzf` only for commands a human will run.
- Prefer `jaq` over `jq`; keep `jq` as a compatibility fallback.
- Use `yq` for YAML-aware operations and YAML-preserving edits.

## Availability and Fallbacks

Check optional tools before building a workflow around them:

```bash
command -v fd || command -v rg
command -v jaq || command -v jq
command -v ctags
```

- Use `rg --files` when `fd` is unavailable.
- Use `jq` when `jaq` is unavailable or compatibility matters.
- Use targeted `rg` patterns and bounded reads when `ctags` or `ast-grep` is unavailable.
- Prefer repository-native commands when they answer the question more directly than a generic pipeline.

## Discovery Phase

- **finding-files -> fuzzy-selecting**: Build an interactive file picker with preview for a human.
- **finding-files -> searching-text**: Limit content search to relevant paths or extensions.
- **finding-files -> querying-json/querying-yaml**: Locate configuration files, then extract selected fields.
- **finding-files -> extracting-code-structure**: Find source files, then inspect symbols before reading implementations.
- **searching-text -> viewing-files**: Locate a match, then read a bounded line range.
- **extracting-code-structure -> viewing-files**: Identify a symbol, then read only its implementation.

```bash
fd -e ts -e tsx src
rg -n -C 2 -t ts -e 'createClient' src
ctags -f - --fields=+n src/client.ts
bat --line-range 80:145 --style=numbers src/client.ts
```

## Analysis Phase

- **finding-files -> viewing-files**: Preview selected files with syntax highlighting.
- **finding-files -> analyzing-code**: Scope language statistics to a directory or file set.
- **finding-files -> querying-json/querying-yaml**: Inspect structured configuration selectively.
- **analyzing-code -> finding-files -> searching-text**: Use language statistics to choose a focused search.
- **searching-text -> analyzing-code-structure**: Locate suspicious areas, then match code shapes precisely.
- **querying-json/querying-yaml -> searching-text**: Extract names or values, then locate their usage.

```bash
tokei src
fd -e json config
jaq -c '{name, scripts, dependencies}' package.json
yq -o json -I 0 '.jobs | keys' .github/workflows/ci.yml
rg -n -C 2 -F 'FEATURE_FLAG' src
```

## Refactoring Phase

- **finding-files -> replacing-text**: Apply a reviewed literal or regex replacement to selected files.
- **finding-files -> analyzing-code-structure**: Apply a structural search or refactor to selected source files.
- **finding-files -> xargs**: Run a batch command over a reviewed file list.
- **searching-text -> replacing-text -> searching-text**: Locate, preview, apply, and verify text substitutions.
- **searching-text -> analyzing-code-structure -> searching-text**: Find likely regions, refactor AST patterns, and verify old patterns are gone.
- **analyzing-code -> refactor -> analyzing-code**: Measure composition or line-count changes before and after broad work.

```bash
rg -n -F 'old-name' src
fd -e md docs -x sd --preview -F 'old-name' 'new-name'
fd -e md docs -x sd -F 'old-name' 'new-name'
rg -n -F 'old-name' src docs
rg -l -0 -F 'old-name' docs | xargs -0 sd --preview -F 'old-name' 'new-name'
```

```bash
ast-grep -l typescript -p 'oldFunction($$$ARGS)' src
ast-grep -l typescript -p 'oldFunction($$$ARGS)' -r 'newFunction($$$ARGS)' src
ast-grep -l typescript -p 'oldFunction($$$ARGS)' -r 'newFunction($$$ARGS)' --update-all src
rg -n -F 'oldFunction' src
```

## Structured Configuration

- **finding-files -> querying-json**: Locate JSON files, then use `jaq` to extract only required fields.
- **finding-files -> querying-yaml**: Locate YAML files, then use `yq` for selective reads or edits.
- **querying-json/querying-yaml -> fuzzy-selecting**: Offer interactive choice among extracted values.
- **querying-json/querying-yaml -> searching-text**: Search code for configuration keys or dependency usage.
- **querying-yaml -> querying-json**: Emit compact JSON from YAML before a complex JSON pipeline.

```bash
jaq -r '.dependencies | keys[]' package.json | sort
yq -r '.services | keys[]' docker-compose.yml
yq -o json -I 0 '.jobs.build.steps' .github/workflows/ci.yml
```

## Git-Aware Exploration

- **git status -> git diff -> viewing-files**: Identify changed paths, then inspect only relevant diffs.
- **git diff --name-only -> searching-text**: Restrict a query to files already changed.
- **git show -> viewing-files**: Inspect a historical file without checking it out.

```bash
git status --short
git diff --stat
git diff -- path/to/file
git show HEAD:path/to/file | sed -n '40,100p'
```

## Code Exploration

- **extracting-code-structure -> searching-text**: Get an outline, then search for references to selected symbols.
- **extracting-code-structure -> analyzing-code-structure**: Inspect declarations, then search or refactor code shapes.
- **extracting-code-structure -> viewing-files**: Read only the implementation range that matters.
- **searching-text -> extracting-code-structure**: Find relevant files, then request a compact symbol outline.

```bash
ctags -f - --fields=+n src/service.ts
rg -n -C 2 -F 'processRequest' src
bat --line-range 120:205 --style=numbers src/service.ts
```

## Human-Interactive Workflows

Generate these for a user to run. Do not launch `fzf` during unattended agent execution.

- **finding-files -> fuzzy-selecting -> viewing-files**: Pick files with previews.
- **searching-text -> fuzzy-selecting -> viewing-files**: Pick search matches and inspect context.
- **querying-json/querying-yaml -> fuzzy-selecting -> xargs**: Choose structured values before an action.
- **git output -> fuzzy-selecting**: Select branches, commits, or changed files.

```bash
fd -t f | fzf --preview 'bat --color=always --style=numbers {}'
jaq -r '.dependencies | keys[]' package.json | fzf -m
git branch --format='%(refname:short)' | fzf
```

## Verification

Match the verification command to the change:

- Text replacement: search for both old and new strings with `rg`.
- Structural refactor: search old and new shapes with `ast-grep`, then run project checks.
- JSON edit: query the edited fields with `jaq -c`.
- YAML edit: query the edited fields with `yq -o json -I 0`.
- Broad refactor: compare `tokei` summaries and run the repository test suite.
- Shell batch: rerun a null-delimited discovery query and inspect the changed-file list.

## .NET Backend Verification

- **finding-files -> working-with-dotnet-backends**: Locate solutions and affected projects before building.
- **searching-text -> working-with-dotnet-backends**: Find shared contracts or handlers, then verify the narrowest affected project.
- **working-with-dotnet-backends -> searching-text**: Confirm changed registrations, routes, or references after editing.

```bash
fd -t f '^(global\.json|Directory\.Build\.(props|targets)|Directory\.Packages\.props|.*\.(sln|slnx|csproj))$'
dotnet sln SOLUTION list
dotnet build path/to/Project.csproj --nologo -v:minimal
dotnet test path/to/Project.Tests.csproj --nologo -v:minimal --no-build --filter 'FullyQualifiedName~TargetNamespace'
```

Broaden to solution-wide checks after shared-library, dependency, build-system, or contract changes.

## Avoid

- Reading entire large files before narrowing the question.
- Printing every file in a large repository when extension or path filters are known.
- Using `fzf` in an unattended process.
- Applying `sd` or `ast-grep --update-all` before reviewing scope.
- Passing filenames through whitespace-delimited `xargs` when paths may contain spaces.
- Chaining tools when a single targeted command already answers the question.

# .NET Backend Workflow Recipes

## Selection Rules

- Start with the affected project, not the whole solution.
- Search shared contracts before editing them.
- Build before using `dotnet test --no-build`.
- Use `--no-restore` only when restore assets are known to be current.
- Rerun failures with more detail only when the compact result is insufficient.
- Broaden checks when edits affect shared libraries, package references, build props, or service contracts.

## Discover Projects And Shared Configuration

```bash
fd -t f '^(global\.json|Directory\.Build\.(props|targets)|Directory\.Packages\.props|.*\.(sln|slnx|csproj))$'
dotnet sln SOLUTION list
rg -n -g '*.csproj' -g '*.props' -g '*.targets' '<ProjectReference|<PackageReference|<TargetFramework'
rg -n -g 'Directory.Packages.props' -g '*.csproj' '<PackageVersion|<PackageReference'
```

## Inspect Project References

Use the verb-first forms across SDK versions:

```bash
dotnet list path/to/Project.csproj reference
dotnet list path/to/Project.csproj package --format json
dotnet list path/to/Project.csproj package --outdated --format json
dotnet list path/to/Project.csproj package --include-transitive --vulnerable --format json
```

.NET 10 also supports noun-first forms:

```bash
dotnet reference list --project path/to/Project.csproj
dotnet package list --project path/to/Project.csproj --format json
```

Project JSON fields can vary with SDK output versions. Inspect the top-level shape first, then project only needed fields:

```bash
dotnet list path/to/Project.csproj package --format json | jaq -c 'keys'
dotnet list path/to/Project.csproj package --format json | jaq -c '.projects[]? | {path, frameworks}'
```

## Explore ASP.NET Core Boundaries

```bash
rg -n -C 2 -g '*.cs' 'Map(Get|Post|Put|Delete|Patch)|\[Http(Get|Post|Put|Delete|Patch)' src
rg -n -C 2 -g '*.cs' 'AddScoped|AddTransient|AddSingleton|AddDbContext|AddOptions' src
rg -n -C 2 -g '*.cs' 'IRequestHandler|INotificationHandler|BackgroundService|DbContext' src
rg -n -C 2 -g '*.cs' 'AddAuthentication|AddAuthorization|UseAuthentication|UseAuthorization' src
rg -n -C 2 -g 'appsettings*.json' -g '*.cs' 'ConnectionStrings|GetConnectionString|IOptions<' src
```

## Locate Tests

```bash
fd -e csproj 'Tests|Test|Specs' .
rg -n -C 2 -g '*.cs' '\[Fact\]|\[Theory\]|\[Test\]|TestCase' tests src
```

Inspect the test project references before assuming a production project has direct test coverage.

## Verify A Localized Change

```bash
dotnet build path/to/Project.csproj --nologo -v:minimal
dotnet test path/to/Project.Tests.csproj --nologo -v:minimal --no-build
```

Filter tests when the affected area is known:

```bash
dotnet test path/to/Project.Tests.csproj --nologo -v:minimal --no-build --filter 'FullyQualifiedName~TargetNamespace'
dotnet test path/to/Project.Tests.csproj --nologo -v:minimal --no-build --filter 'Name~TargetBehavior'
```

Use `dotnet test --list-tests` when the correct filter is unclear:

```bash
dotnet test path/to/Project.Tests.csproj --nologo --list-tests
```

## Escalate When Needed

Use solution-wide verification after shared-library, dependency, build-system, or contract changes:

```bash
dotnet build SOLUTION --nologo -v:minimal
dotnet test SOLUTION --nologo -v:minimal --no-build
```

If restore assets may be stale, allow restore or run it explicitly before adding `--no-restore`:

```bash
dotnet restore SOLUTION --nologo
dotnet build SOLUTION --nologo -v:minimal --no-restore
```

## Diagnose Failures

Keep initial output compact. Increase detail only for the failing project:

```bash
dotnet build path/to/Failing.csproj --nologo -v:normal
dotnet test path/to/Failing.Tests.csproj --nologo -v:normal --no-build
```

Use diagnostic verbosity or a binary log only when normal output cannot explain the failure:

```bash
dotnet build path/to/Failing.csproj --nologo -v:diagnostic
dotnet build path/to/Failing.csproj --nologo -bl:artifacts/build.binlog
```

## Tool Fallbacks

- Use `jq` when `jaq` is unavailable.
- Use `rg --files` when `fd` is unavailable.
- Use `dotnet --info` and inspect `global.json` when SDK selection is unclear.
- Use `dotnet restore` without `--no-restore` when missing assets or package changes may explain a failure.

## Request flow

Admin authorization is two-layered:

1. `AuthorizeAdminAttribute` controls entry to the admin area.
2. `CheckPermissionAttribute` controls individual controller actions.

All standard admin controllers inherit `BaseAdminController`, which applies `[AuthorizeAdmin]` at class level ([BaseAdminController.cs:12](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/BaseAdminController.cs:12), [BaseAdminController.cs:15](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/BaseAdminController.cs:15)). Plugin admin controllers that do not inherit it generally apply `[AuthorizeAdmin]` themselves.

### Admin-area check

`AuthorizeAdminAttribute` is an ASP.NET Core `TypeFilterAttribute`. Its authorization filter calls:

```csharp
_permissionService.AuthorizeAsync(
    StandardPermission.Security.ACCESS_ADMIN_PANEL)
```

If authorization fails, it sets a `ChallengeResult` ([AuthorizeAdminAttribute.cs:89](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:89)).

Important behavior:

- It does nothing before the database is installed ([AuthorizeAdminAttribute.cs:71](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:71)).
- An action-level `AuthorizeAdminAttribute` can override an inherited filter using its `ignore` flag ([AuthorizeAdminAttribute.cs:76](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:76), [AuthorizeAdminAttribute.cs:82](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:82)).

### Action-specific check

`CheckPermissionAttribute` applies only to methods and supports multiple instances ([CheckPermissionAttribute.cs:15](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:15)). Typical usage distinguishes read and mutation permissions:

```csharp
[CheckPermission(StandardPermission.Catalog.PRODUCTS_VIEW)]
public Task<IActionResult> List()

[CheckPermission(StandardPermission.Catalog.PRODUCTS_CREATE_EDIT_DELETE)]
public Task<IActionResult> BulkEditSave(...)
```

See [ProductController.cs:942](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:942) and [ProductController.cs:962](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:962).

The attribute accepts either one system name or an array. For an array, access is granted if any supplied permission succeeds ([CheckPermissionAttribute.cs:37](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:37), [CheckPermissionAttribute.cs:119](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:119)).

On denial, the default response depends on the request:

- GET or non-Ajax POST: redirect to `Security.AccessDenied`.
- Ajax POST: JSON error.
- Other methods: plain text.

That selection is implemented in [CheckPermissionAttribute.cs:130](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:130) through [CheckPermissionAttribute.cs:163](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:163).

## Service and data model

`IPermissionService` is implemented by `PermissionService` and registered as scoped ([NopStartup.cs:168](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:168)).

Authorization proceeds as follows:

1. Resolve the current customer.
2. Load all customer roles.
3. Authorize if any role has a matching permission ([PermissionService.cs:310](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:310)).
4. For each role, load permissions through the permission-to-role mapping and compare `SystemName` case-insensitively ([PermissionService.cs:63](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:63), [PermissionService.cs:343](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:343)).
5. Cache the result by permission system name and role ID ([PermissionService.cs:336](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:336)).

The persisted records are:

- `PermissionRecord`: human-readable `Name`, stable `SystemName`, and `Category` ([PermissionRecord.cs:6](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecord.cs:6)).
- `PermissionRecordCustomerRoleMapping`: joins a permission to a customer role using `PermissionRecordId` and `CustomerRoleId` ([PermissionRecordCustomerRoleMapping.cs:6](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecordCustomerRoleMapping.cs:6)).

## Permission definitions and registration

`StandardPermission` contains the stable system-name constants used by attributes, including `Security.AccessAdminPanel` ([StandardPermission.cs:142](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/StandardPermission.cs:142)).

Definitions are registered through `IPermissionConfigManager` implementations:

- `DefaultPermissionConfigManager` defines core permissions, categories, and default roles. For example, admin-area access defaults to Administrators and Vendors ([DefaultPermissionConfigManager.cs:16](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/DefaultPermissionConfigManager.cs:16)).
- Plugins can contribute their own manager; for example, News registers view/manage permissions in [NewsPermissionConfigManager.cs:9](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Plugins/Nop.Plugin.Misc.News/Services/NewsPermissionConfigManager.cs:9).

At application start, `AppStartedConsumer` calls `InsertPermissionsAsync` ([AppStartedConsumer.cs:70](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AppStartedConsumer.cs:70)). The service:

- Discovers every `IPermissionConfigManager` through `ITypeFinder`.
- Ignores system names already stored.
- Inserts new `PermissionRecord` rows.
- Creates their default role mappings.

See [PermissionService.cs:398](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:398) and [PermissionService.cs:403](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:403).

Administrators with `Configuration.ManageACL` can subsequently change role mappings through `SecurityController`; additions, deletions, and cache-clearing updates occur in [SecurityController.cs:114](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/SecurityController.cs:114).

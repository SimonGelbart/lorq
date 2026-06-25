## Request flow

Admin actions use two permission layers:

1. **Admin-area access** — every controller inheriting `BaseAdminController` receives `[AuthorizeAdmin]`. The filter checks `Security.AccessAdminPanel`; failure returns an authentication challenge. [BaseAdminController.cs:15](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/BaseAdminController.cs:15), [AuthorizeAdminAttribute.cs:75](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:75)

2. **Action-specific access** — actions declare `[CheckPermission(StandardPermission.…)]`. For example, product listing requires `Catalog.ProductsView`, while product mutation requires `Catalog.ProductsCreateEditDelete`. [ProductController.cs:942](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:942), [ProductController.cs:1057](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1057)

Thus an action normally requires both admin-panel access and its specific permission. Actions without `[CheckPermission]` receive only the inherited admin-area check.

## Main attributes

- `AuthorizeAdminAttribute` is a `TypeFilterAttribute`. It supports an action-level `ignore` override and calls `IPermissionService.AuthorizeAsync(Security.AccessAdminPanel)`. [AuthorizeAdminAttribute.cs:11](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:11), [AuthorizeAdminAttribute.cs:82](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:82)

- `CheckPermissionAttribute` applies to methods and may be repeated. It accepts either one system name or an array. [CheckPermissionAttribute.cs:15](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:15)

- An array has **OR semantics**: the filter allows the request when any supplied permission succeeds. [CheckPermissionAttribute.cs:117](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:117)

- Fine-grained denial responses depend on the request: HTML redirects to `Security.AccessDenied`; AJAX POSTs return JSON; other methods can return plain text. [CheckPermissionAttribute.cs:130](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:130), [CheckPermissionAttribute.cs:152](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:152)

## Authorization service and records

`IPermissionService` is implemented by `PermissionService`. It:

- Gets the current customer from `IWorkContext`.
- Loads all customer roles.
- Grants access when **any role** has the requested permission. [PermissionService.cs:291](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:291), [PermissionService.cs:305](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:305)
- Joins role mappings to permission records and caches both role permission sets and individual authorization results. [PermissionService.cs:63](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:63), [PermissionService.cs:331](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:331)

The persistent model consists of:

- `PermissionRecord`: `Name`, unique functional `SystemName`, and `Category`. [PermissionRecord.cs:6](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecord.cs:6)
- `PermissionRecordCustomerRoleMapping`: links a permission ID to a customer-role ID. [PermissionRecordCustomerRoleMapping.cs:6](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecordCustomerRoleMapping.cs:6)

Permissions are therefore role-based, not based on a hard-coded “administrator” check. For example, `AccessAdminPanel` is seeded for both Administrators and Vendors, while individual action permissions determine what each can do. [DefaultPermissionConfigManager.cs:16](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/DefaultPermissionConfigManager.cs:16)

`StandardPermission` centralizes the system-name constants, such as `Catalog.ProductsView` and `Security.AccessAdminPanel`. [StandardPermission.cs:54](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/StandardPermission.cs:54), [StandardPermission.cs:145](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/StandardPermission.cs:145)

## Registration and provisioning

- DI registers `IPermissionService` as scoped. The attribute filters obtain it through `TypeFilterAttribute` dependency injection. [NopStartup.cs:168](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:168)

- `DefaultPermissionConfigManager` supplies built-in `PermissionConfig` definitions, including display name, system name, category, and default roles. Plugins can add their own `IPermissionConfigManager` implementations. [PermissionConfig.cs:9](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionConfig.cs:9), [IPermissionConfigManager.cs:6](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/IPermissionConfigManager.cs:6)

- At application start, `AppStartedConsumer` invokes `InsertPermissionsAsync`. [AppStartedConsumer.cs:19](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AppStartedConsumer.cs:19), [AppStartedConsumer.cs:70](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AppStartedConsumer.cs:70)

- `PermissionService` discovers every `IPermissionConfigManager` through `ITypeFinder`, selects system names not already stored, then creates the permission records and their default role mappings. [PermissionService.cs:398](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:398), [PermissionService.cs:104](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:104)

Administrators manage the resulting role mappings through `SecurityController`; those endpoints themselves require `Configuration.ManageAcl`. [SecurityController.cs:114](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/SecurityController.cs:114), [SecurityController.cs:124](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/SecurityController.cs:124)

## Request flow

Admin actions normally pass through two independent authorization layers:

1. `BaseAdminController` carries `[AuthorizeAdmin]`, so all derived admin controllers require `Security.AccessAdminPanel`. The filter calls `IPermissionService.AuthorizeAsync(...)`; failure returns `ChallengeResult`. An action can explicitly bypass this layer with `[AuthorizeAdmin(true)]`. [BaseAdminController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/BaseAdminController.cs:15) [AuthorizeAdminAttribute.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:71)

2. Individual actions use `[CheckPermission(...)]` for functional access, such as category viewing versus editing. [CategoryController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CategoryController.cs:143)

Both attributes are `TypeFilterAttribute`s backed by async authorization filters. ASP.NET creates those filters and injects their dependencies.

## Main attributes

`AuthorizeAdminAttribute`:

- Applies the broad `Security.AccessAdminPanel` permission.
- Supports an action-scoped `ignore` override.
- Skips checking before the database is installed.
- Produces an authentication challenge when denied. [AuthorizeAdminAttribute.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:11)

`CheckPermissionAttribute`:

- Is method-only and may appear multiple times.
- Accepts one permission name or an array.
- For the array overload, access succeeds when any supplied permission succeeds.
- On denial, returns an access-denied redirect, JSON, or plain text. The default depends on HTTP method and whether the request is AJAX. [CheckPermissionAttribute.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:15) [CheckPermissionAttribute.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:117)

## Authorization service and records

`IPermissionService` is registered as the scoped `PermissionService`. [NopStartup.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:168)

Authorization proceeds as follows:

- Resolve the current customer.
- Load all customer roles.
- Authorize if any role contains a permission whose `SystemName` matches case-insensitively.
- Cache both role permission lists and individual role/permission decisions. [PermissionService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:291) [PermissionService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:331)

The persisted model consists of:

- `PermissionRecord`: display `Name`, stable `SystemName`, and `Category`. [PermissionRecord.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecord.cs:6)
- `PermissionRecordCustomerRoleMapping`: links a permission to a customer role. [PermissionRecordCustomerRoleMapping.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecordCustomerRoleMapping.cs:6)
- The mapping table uses both IDs as a composite primary key and foreign keys. [PermissionRecordCustomerRoleMappingBuilder.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Data/Mapping/Builders/Security/PermissionRecordCustomerRoleMappingBuilder.cs:19)

## Permission registration points

Permission identities are constants such as `StandardPermission.Security.ACCESS_ADMIN_PANEL`, whose stored value is `Security.AccessAdminPanel`. [StandardPermission.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/StandardPermission.cs:145)

Definitions are supplied as `PermissionConfig` records containing name, system name, category, and initial roles. Implementations expose them through `IPermissionConfigManager.AllConfigs`. [PermissionConfig.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionConfig.cs:10) [IPermissionConfigManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/IPermissionConfigManager.cs:6)

Core registrations live in `DefaultPermissionConfigManager`; for example, admin-panel access initially maps to administrator and vendor roles. [DefaultPermissionConfigManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/DefaultPermissionConfigManager.cs:11)

At application startup:

- `AppStartedConsumer` calls `InsertPermissionsAsync()`.
- `PermissionService` discovers every `IPermissionConfigManager` through `ITypeFinder`.
- Missing system names are inserted as `PermissionRecord`s, assigned to configured default roles, and localized. [AppStartedConsumer.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AppStartedConsumer.cs:61) [PermissionService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:398)

Plugins extend the same mechanism by providing their own manager; Polls registers `Polls.View` and `Polls.Manage` this way. [PollPermissionConfigManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Plugins/Nop.Plugin.Misc.Polls/Services/PollPermissionConfigManager.cs:9)

Finally, administrators manage role mappings through `SecurityController`; those actions themselves require `Configuration.ManageACL`. [SecurityController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/SecurityController.cs:96)

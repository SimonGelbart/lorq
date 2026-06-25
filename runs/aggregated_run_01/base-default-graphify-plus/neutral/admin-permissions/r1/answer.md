Admin controller authorization uses two permission gates:

1. Access to the admin area.
2. Permission for the specific action.

### Request flow

```text
Admin action request
  → AuthorizeAdminAttribute
      → Security.AccessAdminPanel required
  → CheckPermissionAttribute
      → action-specific permission required
  → PermissionService
      → current customer roles
      → PermissionRecordCustomerRoleMapping
```

### Attributes

`BaseAdminController` carries `[AuthorizeAdmin]`, so all derived admin controllers inherit the admin-panel gate ([BaseAdminController.cs:15](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/BaseAdminController.cs:15)).

`AuthorizeAdminAttribute` is an asynchronous authorization filter. It checks `StandardPermission.Security.ACCESS_ADMIN_PANEL`; failure produces an MVC `ChallengeResult`. An action can override an inherited filter using `AuthorizeAdminAttribute(ignore: true)` ([AuthorizeAdminAttribute.cs:75](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:75), [AuthorizeAdminAttribute.cs:89](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/AuthorizeAdminAttribute.cs:89)).

`CheckPermissionAttribute` applies only to methods and accepts either one permission system name or an array ([CheckPermissionAttribute.cs:15](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:15)). For an array, authorization succeeds when any listed permission succeeds ([CheckPermissionAttribute.cs:117](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:117)).

On denial it returns:

- HTML redirect to `Security.AccessDenied`
- JSON for AJAX POSTs
- HTML for ordinary GET/POST requests
- Plain text for other HTTP methods

This selection is implemented at [CheckPermissionAttribute.cs:130](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Mvc/Filters/CheckPermissionAttribute.cs:130).

A concrete example separates read and write access: return-request list/edit GET actions require `RETURN_REQUESTS_VIEW`, while the edit POST requires `RETURN_REQUESTS_CREATE_EDIT_DELETE` ([ReturnRequestController.cs:94](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ReturnRequestController.cs:94), [ReturnRequestController.cs:129](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ReturnRequestController.cs:129)).

### Permission service and records

`IPermissionService` is implemented by `PermissionService` and registered as scoped ([NopStartup.cs:168](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:168)).

Authorization works as follows:

- Resolve the current customer.
- Load all their customer roles.
- Grant access if any role has a matching permission.
- Match `PermissionRecord.SystemName` case-insensitively.
- Cache results by permission system name and role ID.

See [PermissionService.cs:305](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:305) and [PermissionService.cs:331](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:331).

The persisted model consists of:

- `PermissionRecord`: display name, stable `SystemName`, and category ([PermissionRecord.cs:6](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecord.cs:6)).
- `PermissionRecordCustomerRoleMapping`: many-to-many association between permission records and customer roles ([PermissionRecordCustomerRoleMapping.cs:6](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Core/Domain/Security/PermissionRecordCustomerRoleMapping.cs:6)).

### Permission declarations and registration

`StandardPermission` centralizes stable system-name constants such as `Orders.ReturnRequestsView` and `Security.AccessAdminPanel` ([StandardPermission.cs:27](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/StandardPermission.cs:27), [StandardPermission.cs:145](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/StandardPermission.cs:145)).

`DefaultPermissionConfigManager` maps those constants to display names, categories, and default roles. For example, admin-panel access defaults to Administrators and Vendors, while return-request permissions default to Administrators ([DefaultPermissionConfigManager.cs:16](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/DefaultPermissionConfigManager.cs:16), [DefaultPermissionConfigManager.cs:45](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/DefaultPermissionConfigManager.cs:45)).

At application startup:

- `AppStartedConsumer` calls `InsertPermissionsAsync` ([AppStartedConsumer.cs:70](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web.Framework/Infrastructure/AppStartedConsumer.cs:70)).
- `PermissionService` discovers every `IPermissionConfigManager` via `ITypeFinder`.
- Missing system names are inserted as `PermissionRecord` rows and mapped to configured default roles ([PermissionService.cs:403](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Libraries/Nop.Services/Security/PermissionService.cs:403)).

Plugins extend permissions by supplying another `IPermissionConfigManager`; for example, the News plugin declares its view/manage permissions in [NewsPermissionConfigManager.cs:9](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Plugins/Nop.Plugin.Misc.News/Services/NewsPermissionConfigManager.cs:9).

Finally, administrators manage role mappings through `SecurityController`. Those actions themselves require `Configuration.ManageACL` ([SecurityController.cs:96](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-permissions__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/SecurityController.cs:96)).

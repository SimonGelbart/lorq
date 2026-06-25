## How it fits

The feature belongs in the existing admin customer-list flow:

1. `GET /Admin/Customer/List` prepares a `CustomerSearchModel`.
2. The list page posts that model either to the grid endpoint or an export action.
3. The controller queries customers through `ICustomerService`.
4. `IExportManager` formats them.
5. The controller returns a downloadable file.

The current flow is visible in [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:293) and [List.cshtml](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:14).

## Likely integration points

### Controller

Use the admin [CustomerController](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:35), not the public or online-customer controllers.

A CSV action would follow `ExportExcelAll` and `ExportXmlAll`:

- Accept `CustomerSearchModel`.
- Require both `CUSTOMERS_VIEW` and `CUSTOMERS_IMPORT_EXPORT`.
- Query matching customers.
- Call the export manager.
- Return a file.

Existing examples are at [CustomerController.cs:1524](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1524) and [CustomerController.cs:1581](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1581). The list-page export dropdown is at [List.cshtml:25](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:25).

### Search model and inactivity definition

The existing [CustomerSearchModel](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Customers/CustomerSearchModel.cs:11) already has:

- `SearchLastActivityFrom`
- `SearchLastActivityTo`
- `SearchIsActive`

“Inactive” should be defined carefully:

- `SearchIsActive == false` means a disabled customer account.
- `SearchLastActivityTo <= cutoff` means a customer who has not recently used the site.

The latter is normally the appropriate definition for an inactivity export. `Customer.LastActivityDateUtc` is the persisted activity timestamp ([Customer.cs:229](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Core/Domain/Customers/Customer.cs:229)).

The list factory already converts the administrator’s cutoff into UTC and passes it as `lastActivityToUtc` ([CustomerModelFactory.cs:556](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/CustomerModelFactory.cs:556)). `CustomerService` then applies:

```csharp
lastActivityToUtc.Value >= c.LastActivityDateUtc
```

at [CustomerService.cs:171](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:171).

Therefore, CSV export can reuse `SearchLastActivityTo`; a separate search model is unnecessary unless the UI needs a dedicated “inactive for N days” input.

### Export service

Customer export formatting belongs in:

- [IExportManager](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/IExportManager.cs:220)
- [ExportManager](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2527)

A likely addition is:

```csharp
Task<string> ExportCustomersToCsvAsync(IList<Customer> customers);
```

Its output should include `LastActivityDateUtc`, since that is the reason each row qualifies. The current XLSX export includes identity, roles, active state, and creation time, but does not expose the last-activity timestamp ([ExportManager.cs:2562](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2562)).

The existing newsletter CSV formatter demonstrates the repository’s string-returning export convention and activity logging at [ExportManager.cs:2739](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2739). Customer values require proper CSV quoting for commas, quotes, and newlines; the simple newsletter implementation is not sufficient for all customer fields.

### Response format

The established CSV response is:

```csharp
return File(
    Encoding.UTF8.GetBytes(result),
    MimeTypes.TextCsv,
    fileName);
```

See [NewsLetterSubscriptionController.cs:236](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/NewsLetterSubscriptionController.cs:236). `MimeTypes.TextCsv` is `"text/csv"` ([MimeTypes.cs:117](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Core/MimeTypes.cs:117)).

A suitable name would be `inactive_customers_<timestamp>.csv`.

## Important consistency issue

The customer grid passes registration dates, last-activity dates, IP address, and active status into `GetAllCustomersAsync` ([CustomerModelFactory.cs:584](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/CustomerModelFactory.cs:584)). Existing Excel/XML “all” exports omit several of those filters ([CustomerController.cs:1531](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1531)).

The CSV action should not copy that omission. Ideally, date conversion and customer-query construction should be shared so grid and export results cannot diverge.

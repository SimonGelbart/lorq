The feature fits naturally into the existing admin customer export pipeline:

`Customer/List` → `CustomerSearchModel` → `CustomerController` → `ICustomerService.GetAllCustomersAsync` → `IExportManager` → CSV `FileResult`

### Likely integration points

| Concern | Existing point | Recommended change |
|---|---|---|
| UI | [Customer/List.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:25) contains the XML/XLSX export dropdown | Add an “Export inactive customers to CSV” submit button |
| Controller | [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1526) owns all customer export actions | Add `ExportInactiveCustomersCsv`, protected by customer view and import/export permissions |
| Search model | [CustomerSearchModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Customers/CustomerSearchModel.cs:49) | Reuse `SearchLastActivityTo` or `SearchIsActive`; no new model is necessarily required |
| Query | [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:155) | Call `GetAllCustomersAsync(lastActivityToUtc: cutoff)` |
| Serialization | [IExportManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/IExportManager.cs:224) and [ExportManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2531) | Add `ExportCustomersToCsvAsync(IList<Customer>)`; selection should remain outside the exporter |
| HTTP response | [MimeTypes.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Core/MimeTypes.cs:117) | Return UTF-8 bytes as `text/csv` with a `.csv` filename |

### Meaning of “inactive”

The repository has two different concepts:

- Disabled account: `Customer.Active == false`, exposed as `CustomerSearchModel.SearchIsActive` at [CustomerSearchModel.cs:94](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Customers/CustomerSearchModel.cs:94).
- No recent activity: `LastActivityDateUtc <= cutoff`. This is what the existing GDPR task calls “inactive” at [DeleteInactiveCustomersTask.cs:43](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Gdpr/DeleteInactiveCustomersTask.cs:43).

For consistency with repository terminology, an “inactive customers export” should probably use the last-activity cutoff. If it supports pre-deletion review, it should also exclude system accounts, matching the task’s `!c.IsSystemAccount` check.

The cutoff could come from:

- `SearchLastActivityTo`, for a filter-driven export; or
- `GdprSettings.DeleteInactiveCustomersAfterMonths`, for a dedicated GDPR-oriented export. `CustomerController` already receives `GdprSettings`.

### Important implementation detail

The grid factory converts activity dates to UTC and passes them into the customer query at [CustomerModelFactory.cs:558](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/CustomerModelFactory.cs:558). Existing XLSX/XML “all” exports do not pass registration dates, activity dates, or IP filters at [CustomerController.cs:1531](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1531).

Therefore, the new action should not copy those export methods blindly. It should reproduce or share the factory’s UTC conversion and complete filtering logic.

For the response, the established CSV pattern is:

```csharp
return File(Encoding.UTF8.GetBytes(result), MimeTypes.TextCsv, fileName);
```

This is used by [NewsLetterSubscriptionController.cs:238](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/NewsLetterSubscriptionController.cs:238). The CSV should at minimum include customer ID, email, active state, creation date, and `LastActivityDateUtc`, with proper quoting for commas, quotes, and newlines.

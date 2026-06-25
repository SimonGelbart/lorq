## How it fits

An inactive-customer CSV export belongs in the existing admin customer list flow:

`Customer/List` search form → `CustomerController` export action → `ICustomerService` query → `IExportManager` serialization → UTF-8 CSV `FileResult`.

The customer grid itself uses JSON through `CustomerList(CustomerSearchModel)` and `PrepareCustomerListModelAsync`; exports should remain separate file-producing actions ([CustomerController.cs:294](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:294)).

## Likely integration points

- Controller: [`CustomerController`](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:35).

  It already injects `ICustomerService`, `IExportManager`, date conversion, notifications, and permissions. Existing Excel/XML actions demonstrate the pattern at [CustomerController.cs:1526](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1526). A CSV action should likewise be an HTTP POST protected by both `CUSTOMERS_VIEW` and `CUSTOMERS_IMPORT_EXPORT`.

- Search model: [`CustomerSearchModel`](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Customers/CustomerSearchModel.cs:11).

  It already exposes `SearchLastActivityFrom`, `SearchLastActivityTo`, and `SearchIsActive` ([CustomerSearchModel.cs:48](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Customers/CustomerSearchModel.cs:48)). Therefore, a last-activity-based export needs no new search model.

- Query translation: [`CustomerModelFactory.PrepareCustomerListModelAsync`](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/CustomerModelFactory.cs:547).

  This is the authoritative example for converting entered dates to UTC, including the end-date `AddDays(1)`, and passing all filters to `GetAllCustomersAsync` ([CustomerModelFactory.cs:557](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/CustomerModelFactory.cs:557)). The new action should reuse equivalent logic or extract it into a shared query helper.

  This matters because the current Excel/XML actions omit registration dates, last-activity dates, and IP filtering, despite receiving the same search model ([CustomerController.cs:1529](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1529)).

- Customer service: [`ICustomerService.GetAllCustomersAsync`](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Customers/ICustomerService.cs:44).

  `CustomerService` applies `lastActivityToUtc` directly to `LastActivityDateUtc`, excludes deleted customers, and optionally filters `Active` status ([CustomerService.cs:155](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:155)).

- Export service: [`IExportManager`](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/IExportManager.cs:224) and [`ExportManager`](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2531).

  Add a customer CSV contract here, likely returning `Task<string>`, following `ExportNewsLetterSubscribersToTxtAsync`. Its implementation builds comma-separated text and records an export activity ([ExportManager.cs:2739](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2739)). A customer-specific method should implement proper CSV escaping for commas, quotes, and line breaks.

- View: [`Areas/Admin/Views/Customer/List.cshtml`](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:21).

  Add the CSV submit button to the existing export dropdown. Because it is inside the customer search form, the current search fields—including last activity—will bind to the action. `Admin.Common.ExportToCsv` already exists and is used elsewhere ([NewsLetterSubscription/List.cshtml:29](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/NewsLetterSubscription/List.cshtml:29)).

- Response format: return UTF-8 bytes with `MimeTypes.TextCsv` and a `.csv` filename:

  ```csharp
  return File(Encoding.UTF8.GetBytes(csv), MimeTypes.TextCsv, fileName);
  ```

  This is the established admin CSV response pattern ([NewsLetterSubscriptionController.cs:234](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/NewsLetterSubscriptionController.cs:234)); `MimeTypes.TextCsv` resolves to `text/csv` ([MimeTypes.cs:117](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Core/MimeTypes.cs:117)).

## Meaning of “inactive”

The repository’s GDPR task defines an inactive customer as one whose last activity predates a cutoff, not necessarily an account with `Active == false` ([DeleteInactiveCustomersTask.cs:43](/home/simon/repos/lorq-worktrees/base-only__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Gdpr/DeleteInactiveCustomersTask.cs:43)).

Therefore:

- Dormant customer export: filter by `SearchLastActivityTo`/`lastActivityToUtc`.
- Disabled account export: filter by `SearchIsActive = false`.
- Avoid conflating the two; `SearchIsActive` currently defaults to `true`, so a dormant export from the existing screen naturally means active accounts with old activity unless the administrator changes that filter.

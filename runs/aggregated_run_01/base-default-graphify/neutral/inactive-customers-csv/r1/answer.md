An inactive customers export should fit as another admin customer-list export, not as a separate admin flow.

The likely integration points are:

- **Controller:** [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1529) under `#region Export / Import`.
  Existing `ExportExcelAll(CustomerSearchModel model)` and `ExportXmlAll(CustomerSearchModel model)` already export filtered customer sets via `_customerService.GetAllCustomersAsync(...)`, then return `File(...)`.

- **Search model:** [CustomerSearchModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Customers/CustomerSearchModel.cs:49).
  The model already has `SearchLastActivityFrom` and `SearchLastActivityTo`, plus `SearchIsActive`. Important distinction: `SearchIsActive` is account active/inactive status, while “inactive customers” in this codebase appears to mean stale `LastActivityDateUtc`.

- **List/search flow:** [CustomerModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/CustomerModelFactory.cs:558).
  The grid converts `SearchLastActivityFrom/To` into UTC and passes them to `GetAllCustomersAsync(...)`. The customer list view includes those filters and sends them to the grid at [List.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:180) and [List.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:296).

- **Customer query service:** [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:155).
  `GetAllCustomersAsync` already accepts `lastActivityFromUtc` and `lastActivityToUtc`, and applies them to `Customer.LastActivityDateUtc`.

- **Inactive definition evidence:** [DeleteInactiveCustomersTask.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Gdpr/DeleteInactiveCustomersTask.cs:43).
  The GDPR task treats inactive customers as those with `lastActivityToUtc = DateTime.UtcNow.AddMonths(-DeleteInactiveCustomersAfterMonths)` and calls `GetAllCustomersAsync(lastActivityToUtc: lastActivityToUtc)`.

- **Export service:** [IExportManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/IExportManager.cs:224) and [ExportManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2531).
  Existing customer exports support XLSX and XML only: `ExportCustomersToXlsxAsync` and `ExportCustomersToXmlAsync`. A CSV-specific inactive export would likely require a new `ExportCustomersToCsvAsync`/`ExportInactiveCustomersToCsvAsync` method, unless the feature is allowed to be XLSX/XML.

- **Response format:** existing customer exports return `File(bytes, MimeTypes.TextXlsx, "customers.xlsx")` or XML bytes. CSV response precedent exists in [NewsLetterSubscriptionController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/NewsLetterSubscriptionController.cs:238): `File(Encoding.UTF8.GetBytes(result), MimeTypes.TextCsv, fileName)`. `MimeTypes.TextCsv` is defined at [MimeTypes.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Core/MimeTypes.cs:117).

One implementation wrinkle: current `ExportExcelAll`/`ExportXmlAll` do **not** pass `createdFromUtc`, `createdToUtc`, `lastActivityFromUtc`, `lastActivityToUtc`, or `ipAddress`, even though the grid search does. So an inactive CSV export should either share the grid’s UTC conversion logic or add equivalent conversion in the controller before calling `GetAllCustomersAsync(lastActivityToUtc: cutoff)`.

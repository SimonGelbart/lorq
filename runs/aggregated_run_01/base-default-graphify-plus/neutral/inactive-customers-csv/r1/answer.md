## Fit in the admin flow

The feature belongs on the existing Admin → Customers list page:

1. `CustomerController.List` prepares `CustomerSearchModel`.
2. `CustomerController.CustomerList` passes it to `CustomerModelFactory`.
3. The factory converts date filters to UTC and calls `ICustomerService.GetAllCustomersAsync`.
4. An export action retrieves the unpaged matching customers, calls `IExportManager`, and returns a file.

This is the same structure used by the current XML/XLSX exports.

### 1. Controller

The likely entry point is [CustomerController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:35). It already injects both `ICustomerService` and `IExportManager`, and its existing export actions:

- Accept `CustomerSearchModel`.
- Require customer-view and customer-import/export permissions.
- Load unpaged customers.
- Call `ExportCustomersToXlsxAsync` or `ExportCustomersToXmlAsync`.
- Return an ASP.NET `File` result.

See [ExportExcelAll](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1529) and [ExportXmlAll](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/CustomerController.cs:1586).

A likely new action would therefore be `ExportInactiveCustomersCsv(CustomerSearchModel model)`, using:

```csharp
[HttpPost]
[CheckPermission(StandardPermission.Customers.CUSTOMERS_VIEW)]
[CheckPermission(StandardPermission.Customers.CUSTOMERS_IMPORT_EXPORT)]
```

The button would fit in the export dropdown in [List.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:20).

### 2. Search model and inactivity semantics

No new search model is necessarily required. `CustomerSearchModel` already exposes:

- `SearchLastActivityFrom`
- `SearchLastActivityTo`
- `SearchIsActive`

See [CustomerSearchModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Customers/CustomerSearchModel.cs:48).

The distinction matters:

- `SearchIsActive == false` means the account’s `Active` flag is disabled.
- “Inactive since a cutoff date” should use `LastActivityDateUtc <= cutoff`.

The list UI already renders both last-activity date fields and submits them with grid searches: [List.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:177) and [List.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Customer/List.cshtml:285).

`CustomerModelFactory` provides the correct timezone and inclusive-date behavior, converting `SearchLastActivityTo` to UTC and adding one day: [CustomerModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/CustomerModelFactory.cs:558).

### 3. Customer search service

`ICustomerService.GetAllCustomersAsync` already accepts `lastActivityFromUtc` and `lastActivityToUtc`: [ICustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Customers/ICustomerService.cs:45).

Its implementation applies:

```csharp
query = query.Where(c => lastActivityToUtc.Value >= c.LastActivityDateUtc);
```

See [CustomerService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/Customers/CustomerService.cs:169). The underlying field is [Customer.LastActivityDateUtc](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Core/Domain/Customers/Customer.cs:229).

One existing gap should not be copied: current `ExportExcelAll` and `ExportXmlAll` omit registration-date, last-activity, and IP filters even though the grid uses them. The inactive CSV action must explicitly pass `lastActivityToUtc`, or the downloaded data will not match the displayed results. Ideally, filter construction should be factored so the grid and exports cannot drift.

### 4. Export service

The likely service extension points are:

- [IExportManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/IExportManager.cs:220)
- [ExportManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Services/ExportImport/ExportManager.cs:2531)

A suitable method would be either:

```csharp
Task<string> ExportCustomersToCsvAsync(IList<Customer> customers);
```

or a specialized `ExportInactiveCustomersToCsvAsync` if the output has a smaller reporting schema.

The CSV should include `LastActivityDateUtc`; the current customer XLSX/XML serializers include `CreatedOnUtc` but omit last activity. Customer data can contain commas, quotes, and line breaks, so a CSV implementation should escape fields rather than directly concatenate them.

### 5. Response format

The repository’s established CSV response pattern is:

```csharp
return File(
    Encoding.UTF8.GetBytes(csv),
    MimeTypes.TextCsv,
    fileName);
```

This is used by [NewsLetterSubscriptionController.ExportCsv](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/NewsLetterSubscriptionController.cs:215). `MimeTypes.TextCsv` is defined as `text/csv` in [MimeTypes.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__inactive-customers-csv__r1/src/Libraries/Nop.Core/MimeTypes.cs:117).

A timestamped filename such as `inactive_customers_2026-06-24-14-30-00.csv` would follow existing conventions.

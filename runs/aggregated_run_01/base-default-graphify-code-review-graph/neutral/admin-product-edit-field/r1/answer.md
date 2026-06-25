The likely change path is:

- [ProductController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1037) — admin create/edit actions. Create prepares `ProductModel`, maps it with `model.ToEntity<Product>()`, then calls `InsertProductAsync`; edit maps onto the existing entity and calls `UpdateProductAsync` (lines 1051, 1082–1085, 1149, 1225–1247).

- [ProductModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Catalog/ProductModel.cs:14) — admin form model. Existing editable fields use properties decorated with `NopResourceDisplayName`, such as `Name`, `ShortDescription`, and `AdminComment` around lines 105–115.

- [ProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/ProductModelFactory.cs:886) — prepares create/edit models. For an existing product it calls `product.ToModel<ProductModel>()` at line 895, then populates non-directly-mapped fields and selections.

- [AdminMapperConfiguration.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs:506) — defines both `Product → ProductModel` and `ProductModel → Product` mappings. A same-named field will generally flow through these mappings unless explicitly ignored.

- [Edit.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Edit.cshtml:12) and [Create.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Create.cshtml:12) — post to the corresponding controller actions and both render the shared `_CreateOrUpdate` partial.

- [_CreateOrUpdate.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.cshtml:86) — organizes the product editor cards. It delegates the main product fields to `_CreateOrUpdate.Info`.

- [_CreateOrUpdate.Info.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.Info.cshtml:208) — most likely place for the actual label/editor/validation markup. Existing fields use `nop-label`, `nop-editor`/`nop-textarea`, and `asp-validation-for`.

Persistence path:

- [Product.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Core/Domain/Catalog/Product.cs:13) — persisted domain entity; the new database-backed property belongs here.

- [ProductService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Services/Catalog/ProductService.cs:578) — `InsertProductAsync` and `UpdateProductAsync` delegate to `IRepository<Product>`.

- [EntityRepository.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/EntityRepository.cs:340) — generic repository sends inserts/updates to the configured data provider.

- [ProductBuilder.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Mapping/Builders/Catalog/ProductBuilder.cs:17) — add explicit column configuration here when length/nullability differs from conventions.

- A new schema migration under `src/Libraries/Nop.Data/Migrations/UpgradeTo500/` or the applicable target version. [UpgradeTo490/SchemaMigration.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Migrations/UpgradeTo490/SchemaMigration.cs:18) demonstrates adding product columns with `AddOrAlterColumnFor<Product>`.

Also likely:

- [defaultResources.nopres.xml](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/App_Data/Localization/defaultResources.nopres.xml:3207) — label and hint resources used by `NopResourceDisplayName`.
- [ProductValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Validators/Catalog/ProductValidator.cs:10) — only if the new field needs validation beyond database-derived rules.

No files were changed.

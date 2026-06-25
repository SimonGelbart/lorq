Likely change surface:

- [ProductController.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1037) — admin create/edit actions. GET actions prepare `ProductModel`; POST create maps with `model.ToEntity<Product>()` and inserts at line 1082, while edit maps onto the existing entity and updates at lines 1225–1247.

- [ProductModel.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Catalog/ProductModel.cs:14) — admin form model. Existing editable properties such as `Name` and `AdminComment` carry `NopResourceDisplayName` attributes.

- [ProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/ProductModelFactory.cs:886) — prepares both create and edit models. For edits, `PrepareProductModelAsync` maps `Product` to `ProductModel` at line 895 and prepares additional form data.

- [IProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/IProductModelFactory.cs:41) — factory contract. Its signature probably does not need changing for an ordinary scalar field.

- [AdminMapperConfiguration.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs:507) — defines both `Product → ProductModel` and `ProductModel → Product` mappings. A same-named scalar property should map conventionally unless explicitly ignored or requiring custom conversion.

Views:

- [Create.cshtml](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Create.cshtml:33) and [Edit.cshtml](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Edit.cshtml:46) both render the shared `_CreateOrUpdate` partial.
- [_CreateOrUpdate.cshtml](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.cshtml:87) composes the product editor cards.
- [_CreateOrUpdate.Info.cshtml](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.Info.cshtml:664) is the likely location for a general product field. Existing fields use `nop-label`, an editor/textarea, and `asp-validation-for`.

Persistence:

- [Product.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Core/Domain/Catalog/Product.cs:13) — persisted domain entity; the new database-backed property belongs here.
- [ProductService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Services/Catalog/ProductService.cs:578) — insert/update methods delegate directly to `_productRepository.InsertAsync` and `UpdateAsync`.
- [ProductBuilder.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Mapping/Builders/Catalog/ProductBuilder.cs:11) — product schema configuration, especially relevant when the field needs a non-default string length or nullability.
- [Migrations](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Migrations) — an existing installation needs a schema migration. [UpgradeTo500/SchemaMigration.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Migrations/UpgradeTo500/SchemaMigration.cs:20) demonstrates guarded `Alter.Table(...).AddColumn(...)` migrations.

For the display label/help text, add matching resources to [defaultResources.nopres.xml](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/App_Data/Localization/defaultResources.nopres.xml:3207).

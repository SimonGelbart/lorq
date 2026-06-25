Likely change path:

`ProductModel` ⇄ AutoMapper ⇄ `Product` → `ProductService` → repository/database

UI rendering and model preparation wrap that core path.

- [Areas/Admin/Controllers/ProductController.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1037) — Admin create/edit actions. GET actions call `PrepareProductModelAsync`; POST create maps with `model.ToEntity<Product>()` and inserts at line 1082, while POST edit maps onto the existing entity and updates at lines 1225–1247.

- [Areas/Admin/Models/Catalog/ProductModel.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Catalog/ProductModel.cs:13) — Admin form model. Existing fields such as `Name` and `Sku` use `NopResourceDisplayName` at lines 105–106 and 140–141. Add the form-facing property here.

- [Areas/Admin/Factories/ProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/ProductModelFactory.cs:886) — Prepares both create and edit models. For edits, line 895 initializes the model through `product.ToModel<ProductModel>()`; this file is needed if the field requires defaults, options, permissions, or nontrivial preparation.

- [Areas/Admin/Factories/IProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/IProductModelFactory.cs:41) — Factory contract for `PrepareProductModelAsync`; probably unchanged unless preparation APIs change.

- [Areas/Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs:507) — Declares `Product → ProductModel` and, at line 560, `ProductModel → Product`. A same-named compatible property should map by convention; inspect this file if the field needs ignoring or custom mapping.

- [Areas/Admin/Views/Product/Create.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Create.cshtml:12) and [Edit.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Edit.cshtml:12) — Form endpoints. Both render `_CreateOrUpdate`, at create line 33 and edit line 46; they usually need no field-specific change.

- [Areas/Admin/Views/Product/_CreateOrUpdate.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.cshtml:86) — Composes the editor cards. The general product-information card loads `_CreateOrUpdate.Info` at line 87.

- [Areas/Admin/Views/Product/_CreateOrUpdate.Info.cshtml](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.Info.cshtml:208) — Most likely field UI location. Existing controls use `nop-label`, `nop-editor`, and validation bindings—for example `Name` at lines 208–215 and `Sku` at 244–250. Another specialized partial may be more appropriate for price, inventory, shipping, etc.

- [Nop.Core/Domain/Catalog/Product.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Core/Domain/Catalog/Product.cs:13) — Persistent domain entity. Existing persisted properties include `Name` at line 35 and `Sku` at line 120.

- [Nop.Services/Catalog/ProductService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Services/Catalog/ProductService.cs:555) — Persistence service. It retrieves via `_productRepository.GetByIdAsync`; inserts and updates delegate to the repository at lines 578–600. A plain new entity column normally requires no service change.

- [Nop.Data/Mapping/Builders/Catalog/ProductBuilder.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Mapping/Builders/Catalog/ProductBuilder.cs:9) — Installation-schema customization. Lines 20–27 define explicit product column constraints. Add configuration here when the new property needs specific length/nullability beyond conventions.

- Add an upgrade migration under [Nop.Data/Migrations](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Migrations) for existing databases. [UpgradeTo490/SchemaMigration.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Migrations/UpgradeTo490/SchemaMigration.cs:18) demonstrates product columns added with `AddOrAlterColumnFor<Product>()`.

- [defaultResources.nopres.xml](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/App_Data/Localization/defaultResources.nopres.xml:3591) — Add the display/help localization keys referenced by `NopResourceDisplayName`.

- [Areas/Admin/Validators/Catalog/ProductValidator.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Validators/Catalog/ProductValidator.cs:10) — Conditional candidate if the new field needs validation; the existing required-name rule starts at line 14.

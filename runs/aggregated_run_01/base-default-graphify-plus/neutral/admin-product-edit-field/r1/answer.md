The likely end-to-end path is:

`ProductModel` → controller model binding → AutoMapper → `Product` → `ProductService` → `IRepository<Product>`.

Core candidate files:

- [ProductController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1037)  
  Owns GET/POST Create and Edit. Create maps with `model.ToEntity<Product>()` and inserts at lines 1082–1085; Edit maps onto the existing entity and updates it around lines 1230–1247.

- [ProductModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Catalog/ProductModel.cs:14)  
  Admin form model. Existing fields such as `Name`, `AdminComment`, and `ShowOnHomepage` demonstrate the expected property and `NopResourceDisplayName` pattern around lines 105–118.

- [ProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/ProductModelFactory.cs:886)  
  `PrepareProductModelAsync` populates the create/edit model. For existing products it calls `product.ToModel<ProductModel>()` at line 896, then prepares related form data.

- [IProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/IProductModelFactory.cs:41)  
  Declares the preparation contract used by the controller.

- [Edit.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Edit.cshtml:46) and [Create.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Create.cshtml:33)  
  Both render the shared `_CreateOrUpdate` form.

- [_CreateOrUpdate.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.cshtml:87)  
  Composes the editor cards. The general product-information card delegates to `_CreateOrUpdate.Info`.

- [_CreateOrUpdate.Info.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.Info.cshtml:210)  
  Likely field placement for general product data. Existing controls use `nop-label`, `nop-editor`/`nop-textarea`, and validation; `AdminComment` is another example at lines 666–670.

Persistence candidates:

- [Product.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Core/Domain/Catalog/Product.cs:13)  
  Persistent domain entity. The new database-backed property belongs here.

- [AdminMapperConfiguration.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs:507)  
  Defines both `Product → ProductModel` and `ProductModel → Product` maps. Same-named properties map conventionally unless explicitly ignored.

- [ProductService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Services/Catalog/ProductService.cs:575)  
  `InsertProductAsync` and `UpdateProductAsync` delegate directly to `_productRepository` at lines 580 and 600.

- [ProductBuilder.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Mapping/Builders/Catalog/ProductBuilder.cs:9)  
  Product schema customization, especially string length and nullability. Relevant when the new field needs explicit database characteristics.

- [UpgradeTo490/SchemaMigration.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Migrations/UpgradeTo490/SchemaMigration.cs:10)  
  Source example of adding product columns with `AddOrAlterColumnFor<Product>`. A new migration should be created for the current target version rather than modifying this historical migration.

Also likely if applicable:

- [ProductValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Validators/Catalog/ProductValidator.cs:10) for required/range validation.
- [defaultResources.nopres.xml](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/App_Data/Localization/defaultResources.nopres.xml:3207) for the field label and hint resources.

No files were changed.

Used `graphify query` first, then verified the important claims against source.

**Core Admin Flow**

- [ProductController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/ProductController.cs:1037)
  Admin create/edit controller. Evidence: GET `Create` prepares `ProductModel` through `_productModelFactory.PrepareProductModelAsync(...)` at lines 1037-1051; POST `Create` maps `model.ToEntity<Product>()` and calls `_productService.InsertProductAsync(product)` at lines 1058-1085. Edit mirrors this: GET loads product and prepares model at lines 1136-1149; POST maps `model.ToEntity(product)` and calls `_productService.UpdateProductAsync(product)` at lines 1183-1247.

- [ProductModel.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Models/Catalog/ProductModel.cs:14)
  Admin product form model. Evidence: `ProductModel` is the admin model at line 14; existing persisted product fields are defined here with `NopResourceDisplayName`, e.g. `Name` line 106, `AdminComment` line 115, `Sku` line 141, `Price` line 329, `Published` line 401.

- [IProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/IProductModelFactory.cs:41)
  Factory contract for preparing product admin models. Evidence: declares `PrepareProductModelAsync(ProductModel model, Product product, bool excludeProperties = false)`.

- [ProductModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Factories/ProductModelFactory.cs:886)
  Main model preparation implementation. Evidence: `PrepareProductModelAsync` starts at line 886; existing product entity values are copied with `product.ToModel<ProductModel>()` around line 895; defaults for new products are set when `product == null` around line 960.

**Views**

- [Create.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Create.cshtml:9)
  Create form entry point. Evidence: posts to `Product/Create` and renders `@await Html.PartialAsync("_CreateOrUpdate", Model)`.

- [Edit.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/Edit.cshtml:10)
  Edit form entry point. Evidence: posts to `Product/Edit` and also renders `_CreateOrUpdate`.

- [_CreateOrUpdate.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.cshtml:89)
  Shared create/edit card composition. Evidence: renders product cards; the main info card renders `_CreateOrUpdate.Info`, price card renders `_CreateOrUpdate.Prices`, etc.

- [_CreateOrUpdate.Info.cshtml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Views/Product/_CreateOrUpdate.Info.cshtml:155)
  Likely place for a general product field in the “Info” card. Evidence: existing inputs use `nop-label`, `nop-editor`, validation spans for fields like `Name`, `ShortDescription`, `FullDescription`, and `Sku`.

**Mapping and Persistence**

- [AdminMapperConfiguration.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Infrastructure/Mapper/AdminMapperConfiguration.cs:507)
  AutoMapper config for Product/ProductModel. Evidence: `CreateMap<Product, ProductModel>()` at line 507 and `CreateMap<ProductModel, Product>()` at line 560. Controller persistence depends on this through `ToEntity`.

- [MappingExtensions.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Infrastructure/Mapper/Extensions/MappingExtensions.cs:85)
  `ToEntity` implementation used by controller. Evidence: maps model to new entity at line 85 and model into existing entity at line 100.

- [Product.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Core/Domain/Catalog/Product.cs:13)
  Domain entity to persist a real product field. Evidence: `Product` entity starts at line 13; persisted properties include `Name`, `ShortDescription`, `FullDescription`, `AdminComment`, `Sku`, `Price`, `Published`, etc.

- [ProductBuilder.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Mapping/Builders/Catalog/ProductBuilder.cs:9)
  Product table mapping for install/schema generation. Evidence: `ProductBuilder : NopEntityBuilder<Product>` and `MapEntity` defines product columns like `Name`, `Sku`, `Gtin`, `RequiredProductIds`.

- [ProductService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Services/Catalog/ProductService.cs:578)
  Repository persistence path. Evidence: `InsertProductAsync` calls `_productRepository.InsertAsync(product)` at line 578; `UpdateProductAsync` calls `_productRepository.UpdateAsync(product)` at line 598.

**Likely Supporting Files**

- [ProductValidator.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/Areas/Admin/Validators/Catalog/ProductValidator.cs:9)
  Add validation here if the new admin field has required/range rules. Existing evidence: validates `Name`, `SeName`, `RentalPriceLength`, `MinimumAgeToPurchase`, then calls `SetDatabaseValidationRules<Product>()`.

- [defaultResources.nopres.xml](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Presentation/Nop.Web/App_Data/Localization/defaultResources.nopres.xml:3207)
  Localization for labels/hints. Existing field evidence: `Admin.Catalog.Products.Fields.AdminComment` and `.Hint` are defined here.

- [src/Libraries/Nop.Data/Migrations/UpgradeTo500/](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__admin-product-edit-field__r1/src/Libraries/Nop.Data/Migrations/UpgradeTo500/SchemaMigration.cs:10)
  Likely migration location/pattern for adding a column to existing installs. Evidence: schema migrations use `[NopSchemaMigration(...)]`; examples add/alter columns with `Alter.Table(...).AddColumn(...)` or `this.AddOrAlterColumnFor<TEntity>(...)`.

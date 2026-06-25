I identified `IOrderTotalCalculationService` as the contract. A signature or return-shape change will affect its implementation, DI registrations, all tuple-deconstruction callers, and plugins that inject it.

**Core Contract**
- [IOrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/IOrderTotalCalculationService.cs:10): defines subtotal, shipping, tax, total, payment fee, update totals, reward point methods.
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:24): sole implementation.
- DI registrations: [NopStartup.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:201), [BaseNopTest.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Tests/Nop.Tests/BaseNopTest.cs:374).

**High-Impact Services**
- [OrderProcessingService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:62): injects the contract.
- Uses it for tax and final order placement totals at lines 382 and 393.
- Uses it when admin/order-edit totals are recalculated at line 1735.
- Uses subtotal/total for minimum order validation and payment workflow decisions at lines 3169, 3192, and 3216.

**Checkout / Cart UI**
- [ShoppingCartModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:68): injects the contract.
- Calls subtotal, shipping, tax, and total methods for mini-cart/order-total models at lines 1079, 1194, 1213, 1251, and 1280.
- [CheckoutModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:43): injects it and checks reward-point totals at line 514.
- [OrderController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/OrderController.cs:1250): admin order editing calls `IOrderProcessingService.UpdateOrderTotalsAsync`, which delegates to this contract.

**Payment Code / Plugins**
- [PayPalCommerceServiceManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:73): direct dependency. Uses subtotal/total/tax/shipping for button amounts, PayPal order items, and approved amount validation at lines 231, 239, 552, 565, 569, and 2146.
- [CheckMoneyOrderPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder/CheckMoneyOrderPaymentProcessor.cs:22): direct dependency; calls `CalculatePaymentAdditionalFeeAsync` at line 104.
- [ManualPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.Manual/ManualPaymentProcessor.cs:24): constructor dependency, likely compile-affected by contract/type changes.

**Discount / Tax**
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:32): depends on `IDiscountService`; subtotal/shipping/order total discounts are calculated in helpers such as lines 115 and 154, and final order total discount is applied around line 1377.
- [TaxService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Tax/TaxService.cs:731): order-total service delegates tax total calculation to active tax providers.
- [FixedOrByCountryStateZipTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip/FixedOrByCountryStateZipTaxProvider.cs:31): injects the contract and calls subtotal for tax total at line 168.
- [AvalaraTaxManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/Services/AvalaraTaxManager.cs:46): injects it and uses subtotal discount data at line 1343.

**Other Plugins**
- [MarketingAutomationManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Brevo/Services/MarketingAutomationManager.cs:38): uses subtotal, tax, shipping, total at lines 139-143.
- [OmnisendService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendService.cs:31): cart total at line 458.
- [OmnisendEventsService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendEventsService.cs:45): cart total at line 237.
- [FacebookPixelService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Widgets.FacebookPixel/Services/FacebookPixelService.cs:54): cart total at line 818.
- [UPSService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Shipping.UPS/Services/UPSService.cs:57): subtotal for insurance values at lines 568, 600, and 692.

**Tests**
- [OrderTotalCalculationServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderTotalCalculationServiceTests.cs:26): main coverage for subtotal, tax, total, discounts, payment fees, reward points.
- [FixedRateTestTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Tax/FixedRateTestTaxProvider.cs:14): test tax provider injects the contract and calls subtotal at line 65.

Graph evidence: `graphify query` found extracted `IOrderTotalCalculationService` field-reference edges to `IOrderProcessingService`, `CheckMoneyOrderPaymentProcessor.cs`, and `OrderTotalCalculationServiceTests.cs`. I verified the broader affected set against source with direct `rg` and line-numbered reads.

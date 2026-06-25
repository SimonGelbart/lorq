The blast radius is broad: 14 production files directly call `IOrderTotalCalculationService`, plus registration, tests, and indirect payment/tax/discount paths. The exact subset depends on which method changes; tuple-return changes are especially disruptive because callers destructure by position.

## Core services

- Contract: [IOrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/IOrderTotalCalculationService.cs:10)
- Implementation and dependencies: [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:24)
- DI registration: [NopStartup.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:201)
- Order placement and recalculation: [OrderProcessingService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:339)

`OrderProcessingService.PrepareAndValidateTotalsAsync` consumes subtotal, shipping, tax, total, discounts, gift cards, and reward-point tuple fields, then assigns the result to `ProcessPaymentRequest.OrderTotal`. This is the highest-risk downstream consumer.

The implementation directly coordinates:

- `IDiscountService`
- `IPaymentService`
- `ITaxService`
- shipping services/plugins
- shopping-cart and price-calculation services
- gift cards and reward points

## Checkout and storefront

- [CheckoutModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:404): adjusts displayed shipping rates; calculates reward-point payment state at line 514.
- [ShoppingCartModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1183): builds the order-total display from subtotal, shipping, fees, tax rates, discounts, gift cards, rewards, and final total.
- [OrderModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/OrderModelFactory.cs:783): consumes reward-point conversion methods.
- [CheckoutController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1272): prepares confirmation; at line 1332 it enters `OrderProcessingService.PlaceOrderAsync`.
- [OrderTotalsViewComponent.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Components/OrderTotalsViewComponent.cs:33): indirect route into `ShoppingCartModelFactory`.

Relationship:

```text
CheckoutController
  → CheckoutModelFactory / ShoppingCartModelFactory
  → IOrderTotalCalculationService

CheckoutController.ConfirmOrder
  → OrderProcessingService.PlaceOrderAsync
  → PrepareAndValidateTotalsAsync
  → subtotal + shipping + tax + final total
  → ProcessPaymentRequest.OrderTotal
```

## Plugins

Direct callers that must be checked for signature or tuple changes:

- [PayPalCommerceServiceManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:542): reconstructs PayPal subtotal, shipping, tax, discount, and final amount.
- [CheckMoneyOrderPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder/CheckMoneyOrderPaymentProcessor.cs:102)
- [ManualPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.Manual/ManualPaymentProcessor.cs:117)
- [FixedOrByCountryStateZipTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip/FixedOrByCountryStateZipTaxProvider.cs:152)
- [AvalaraTaxManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/Services/AvalaraTaxManager.cs:1331)
- [UPSService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Shipping.UPS/Services/UPSService.cs:568)
- [MarketingAutomationManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Brevo/Services/MarketingAutomationManager.cs:139)
- [OmnisendService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendService.cs:458)
- [OmnisendEventsService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendEventsService.cs:237)
- [FacebookPixelService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Widgets.FacebookPixel/Services/FacebookPixelService.cs:818)

## Payment relationship

[OrderTotalCalculationService.GetShoppingCartTotalAsync](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:1327) calls `IPaymentService.GetAdditionalHandlingFeeAsync`, then taxes the fee.

Percentage-fee plugins call back in the opposite direction:

```text
Order total
  → PaymentService.GetAdditionalHandlingFeeAsync
  → payment plugin.GetAdditionalHandlingFeeAsync
  → CalculatePaymentAdditionalFeeAsync
  → GetShoppingCartTotalAsync(usePaymentMethodAdditionalFee: false)
```

Relevant abstractions are [IPaymentService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/IPaymentService.cs:46), [PaymentService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/PaymentService.cs:140), and [IPaymentMethod.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/IPaymentMethod.cs:49).

Preserving the `usePaymentMethodAdditionalFee: false` recursion guard is important.

## Discount relationship

[OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:115) independently applies:

- order-subtotal discounts
- shipping discounts
- order-total discounts

Each path loads discounts by `DiscountType`, validates coupon requirements, selects the preferred discount, and returns applied `Discount` objects. `OrderProcessingService` subsequently persists those applied discounts. Changes to whether totals are pre/post-discount or tax-inclusive therefore affect both amounts and discount history.

## Tax relationship

- [TaxService.GetTaxTotalAsync](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Tax/TaxService.cs:731) loads the primary `ITaxProvider`, calls it, then publishes `TaxTotalCalculatedEvent`.
- The fixed tax provider calls order-total subtotal and shipping methods to assemble tax rates.
- Avalara constructs a dummy order using calculated shipping, payment fees, and subtotal discount.

The fixed provider contains an explicit circular-reference short circuit around payment-fee tax. Changing fee flags or call ordering can reintroduce recursion.

## Tests

Primary, directly affected coverage:

- [OrderTotalCalculationServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderTotalCalculationServiceTests.cs:176): subtotal, checkout attributes, discounts, shipping, tax, final totals, rounding, and reward points.
- [FixedRateTestTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Tax/FixedRateTestTaxProvider.cs:48): directly destructures subtotal and shipping results.
- [BaseNopTest.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/BaseNopTest.cs:374): test DI registration.

Likely regression suites:

- [ShoppingCartModelFactoryTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/ShoppingCartModelFactoryTests.cs:139)
- [CheckoutModelFactoryTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/CheckoutModelFactoryTests.cs:141)
- [DiscountServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Discounts/DiscountServiceTests.cs:46)
- [TaxServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Tax/TaxServiceTests.cs:102)
- [OrderProcessingServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderProcessingServiceTests.cs:15)

There is limited direct test coverage for full order placement, payment-fee recursion, and plugin amount breakdowns. Those deserve targeted tests if calculation semantics—not just signatures—change.

Graphify located the interface, implementation, order-processing, PayPal, UPS, tax-provider, and test communities within two hops. All relationships above were then verified against the cited source files.

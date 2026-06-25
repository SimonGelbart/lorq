A change to `IOrderTotalCalculationService` has a broad compile-time and behavioral blast radius. Its methods return large positional tuples, so changing tuple arity, ordering, or types breaks many destructuring call sites; changing semantics affects checkout, payment reconciliation, discounts, tax providers, shipping, rewards, and order persistence.

## Contract and implementation

- [IOrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/IOrderTotalCalculationService.cs:10) — public contract. It covers subtotal, shipping, tax, final total, payment fees, order updates, and reward points.
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:24) — sole implementation found.
- [NopStartup.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:201) — production DI registration.
- [BaseNopTest.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Tests/Nop.Tests/BaseNopTest.cs:374) — test DI registration.
- [UpdateOrderParameters.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/UpdateOrderParameters.cs:10) — request model coupled to `UpdateOrderTotalsAsync`.

## Direct consumer map

| Contract area | Principal consumers and evidence |
|---|---|
| Subtotals | [OrderProcessingService.cs:343](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:343) persists inclusive/exclusive totals and discounts. [ShoppingCartModelFactory.cs:1194](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1194) renders them. PayPal, UPS, Brevo, Avalara, and the fixed tax provider also consume them. |
| Shipping totals/rate adjustment | [CheckoutModelFactory.cs:404](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:404) adjusts displayed shipping methods. [OrderProcessingService.cs:361](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:361) persists shipping totals. Tax providers use inclusive/exclusive shipping to derive shipping tax. |
| Tax total | [OrderProcessingService.cs:382](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:382) stores tax and serialized rates. [ShoppingCartModelFactory.cs:1251](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1251) renders tax totals/rates. PayPal and Brevo also consume it. |
| Final cart total | [OrderProcessingService.cs:393](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:393) turns its tuple into persisted order total, discounts, gift cards, rewards, and payment amount. [ShoppingCartModelFactory.cs:1280](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1280) renders the same fields. |
| Payment fee | [CheckMoneyOrderPaymentProcessor.cs:104](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder/CheckMoneyOrderPaymentProcessor.cs:104) and [ManualPaymentProcessor.cs:119](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.Manual/ManualPaymentProcessor.cs:119) delegate percentage-fee calculation to the contract. |
| Rewards | [CheckoutModelFactory.cs:514](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:514) determines whether points cover the order. [ShoppingCartModelFactory.cs:1327](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1327) calculates points to earn. [OrderProcessingService.cs:875](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:875) awards points. |
| Existing-order updates | [OrderProcessingService.cs:1735](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1735) delegates recalculation. Admin order edits enter through [OrderController.cs:1250](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Areas/Admin/Controllers/OrderController.cs:1250). |

## Checkout and order services

Key files:

- [OrderProcessingService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:330) — most important consumer. `PrepareAndValidateTotalsAsync` calls subtotal, shipping, tax, and total methods, aggregates applied discounts, and copies the final value into `ProcessPaymentRequest.OrderTotal`.
- [CheckoutModelFactory.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:370) — shipping choices, pickup fees, reward-point payment state.
- [ShoppingCartModelFactory.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1183) — complete customer-facing order summary.
- [OrderModelFactory.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/OrderModelFactory.cs:783) — reward-point value conversion.
- [CheckoutController.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:949) — indirect entry point through checkout factories.

Minimum-order validation also depends on the contract at [OrderProcessingService.cs:3169](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:3169).

## Payment code and plugins

There is a significant recursion-sensitive relationship:

1. `GetShoppingCartTotalAsync` asks [PaymentService.cs:140](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/PaymentService.cs:140) for the selected plugin’s handling fee.
2. Percentage-fee plugins call `CalculatePaymentAdditionalFeeAsync`.
3. That method recalculates the order with `usePaymentMethodAdditionalFee: false` at [OrderTotalCalculationService.cs:1428](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:1428).

Directly affected payment files:

- [IPaymentService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/IPaymentService.cs:46)
- [PaymentService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/PaymentService.cs:140)
- [CheckMoneyOrderPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder/CheckMoneyOrderPaymentProcessor.cs:104)
- [ManualPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.Manual/ManualPaymentProcessor.cs:119)
- [PayPalCommerceServiceManager.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:545) — builds PayPal subtotal/shipping/tax/discount breakdowns.
- [PayPalCommerceServiceManager.cs:2147](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:2147) — explicitly rejects approved PayPal orders when their amount differs from the recalculated cart total.

## Discount code

`OrderTotalCalculationService` is the orchestration point for three discount types:

- Subtotal discounts: [OrderTotalCalculationService.cs:115](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:115)
- Shipping discounts: [OrderTotalCalculationService.cs:154](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:154)
- Order-total discounts: [OrderTotalCalculationService.cs:195](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:195)

Each calls:

- [IDiscountService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/IDiscountService.cs:46)
- [DiscountService.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/DiscountService.cs:187) for loading discounts
- [DiscountService.cs:328](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/DiscountService.cs:328) for choosing the preferred discount
- [DiscountService.cs:492](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/DiscountService.cs:492) for validation

The returned `List<Discount>` is consumed by `OrderProcessingService` to build applied-discount history, so changing that part of the tuple affects order persistence, not only display.

## Tax code

Tax has another callback cycle:

```text
OrderTotalCalculationService
  → TaxService.GetTaxTotalAsync
    → active ITaxProvider
      → IOrderTotalCalculationService subtotal/shipping methods
```

Evidence:

- [OrderTotalCalculationService.cs:1303](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:1303) delegates tax totals.
- [TaxService.cs:727](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Tax/TaxService.cs:727) resolves the primary tax plugin and publishes `TaxTotalCalculatedEvent`.
- [ITaxProvider.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Tax/ITaxProvider.cs:28) defines the provider boundary.
- [FixedOrByCountryStateZipTaxProvider.cs:152](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip/FixedOrByCountryStateZipTaxProvider.cs:152) calls subtotal and shipping calculations and contains explicit circular-reference protection.
- [AvalaraTaxProvider.cs:93](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/AvalaraTaxProvider.cs:93) delegates to Avalara transaction creation.
- [AvalaraTaxManager.cs:1331](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/Services/AvalaraTaxManager.cs:1331) uses shipping and subtotal discount values to construct its dummy tax order.

Changes to the `usePaymentMethodAdditionalFee` behavior are particularly risky because both tax and payment code use it to break recursion.

## Other directly affected plugins

- [UPSService.cs:568](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Shipping.UPS/Services/UPSService.cs:568) — uses undiscounted subtotal as package insurance value.
- [MarketingAutomationManager.cs:139](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Brevo/Services/MarketingAutomationManager.cs:139) — exports subtotal, discount, tax, shipping, and total.
- [OmnisendEventsService.cs:237](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendEventsService.cs:237)
- [OmnisendService.cs:458](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendService.cs:458)
- [FacebookPixelService.cs:818](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Widgets.FacebookPixel/Services/FacebookPixelService.cs:818) — checkout conversion value.

## Tests to update or run

Highest-value direct suite:

- [OrderTotalCalculationServiceTests.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderTotalCalculationServiceTests.cs:176) — subtotal with/without tax, checkout attributes, all discount levels, free shipping, shipping tax, tax totals, final totals, and rewards.
- [FixedRateTestTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Tax/FixedRateTestTaxProvider.cs:48) — test provider calls back into the order-total contract.

Higher-level regression suites:

- [ShoppingCartModelFactoryTests.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/ShoppingCartModelFactoryTests.cs:139) — order-total presentation.
- [CheckoutModelFactoryTests.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/CheckoutModelFactoryTests.cs:141) — shipping and payment checkout models.
- [OrderProcessingServiceTests.cs](/home/simon/repos/lorq-worktrees/base-only__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderProcessingServiceTests.cs:198) — payment/refund behavior conditional on order total.

No dedicated tests for the directly affected PayPal, Manual, CheckMoneyOrder, UPS, Avalara, Brevo, Omnisend, or Facebook Pixel consumers were found. Those are important coverage gaps for a contract change.

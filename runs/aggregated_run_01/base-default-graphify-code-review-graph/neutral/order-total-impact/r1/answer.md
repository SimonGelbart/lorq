Assumption: “order total calculation contract” means `IOrderTotalCalculationService`. Its tuple return types are destructured throughout the codebase, so changing parameter lists, tuple shape/order, nullability, or semantics has a broad impact.

```text
Checkout/cart/payment plugins
          │
          ▼
IOrderTotalCalculationService ──► OrderTotalCalculationService
          │                              │
          │                              ├─► IDiscountService
          │                              ├─► IPaymentService ──► payment plugins
          │                              ├─► ITaxService ──► tax provider plugins
          │                              └─► shipping, pricing, gift cards, rewards
          ▼
OrderProcessingService ──► persisted Order totals
```

## Core contract and services

- [IOrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/IOrderTotalCalculationService.cs:10) — contract defining subtotal, shipping, tax, total, payment-fee, reward-point, and update-total operations.
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:24) — sole production implementation.
- [NopStartup.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:201) — production DI registration.
- [OrderProcessingService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:339) — principal business consumer. `PrepareAndValidateTotalsAsync` destructures:
  - subtotals at line 343,
  - shipping totals at line 361,
  - tax at line 382,
  - final total, discounts, gift cards, and reward points at line 393.
- [OrderProcessingService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1567) — `PlaceOrderAsync` ultimately persists those calculated values.
- [OrderProcessingService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1735) — order-edit/update path calls `UpdateOrderTotalsAsync`.

## Checkout and storefront

- [CheckoutModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:500) — payment-method preparation calls `GetShoppingCartTotalAsync` to decide reward-point display and whether points cover the order.
- [ShoppingCartModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1079) — consumes subtotal calculations.
- [ShoppingCartModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1213) — consumes shipping totals.
- [ShoppingCartModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1251) — consumes tax totals/rates.
- [ShoppingCartModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1280) — maps the final tuple into order-total, discount, gift-card, and reward-point UI.
- [OrderModelFactory.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/OrderModelFactory.cs:783) — reward-point amount conversion.
- [CheckoutController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1332) — normal checkout reaches the contract indirectly through `PlaceOrderAsync`.
- [CheckoutController.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2072) — one-page checkout has the same path.

## Payment code and plugins

- [PaymentService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/PaymentService.cs:140) — loads the selected payment plugin and asks it for an additional handling fee.
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:1355) — incorporates that fee and its tax into the final total.
- [CheckMoneyOrderPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder/CheckMoneyOrderPaymentProcessor.cs:100) and [ManualPaymentProcessor.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.Manual/ManualPaymentProcessor.cs:115) — directly call `CalculatePaymentAdditionalFeeAsync`.
- [PayPalCommerceServiceManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:231) — directly consumes subtotal, shipping, tax, and final-total tuples when creating/updating PayPal orders. Further total calls occur around lines 553–569 and 2147.

## Discount code

- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:115) — subtotal discount selection.
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:154) — shipping discount selection.
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:195) — order-total discount selection.
- These methods call `IDiscountService.GetAllDiscountsAsync`, `ValidateDiscountAsync`, `ContainsDiscount`, and `GetPreferredDiscount`; therefore [IDiscountService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/IDiscountService.cs) and [DiscountService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/DiscountService.cs) are upstream behavioral dependencies.
- `OrderProcessingService` also consumes the returned applied-discount collections at lines 348–354 and 370–376. Changing those collections affects discount history persistence.

## Tax code and tax plugins

The tax relationship is bidirectional:

1. [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:1303) delegates tax-total calculation to `ITaxService`.
2. [TaxService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Tax/TaxService.cs:731) loads the primary `ITaxProvider` and calls it at line 747.
3. Tax providers call back into the order-total contract for taxable subtotal and shipping components.

Key providers:

- [FixedOrByCountryStateZipTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip/FixedOrByCountryStateZipTaxProvider.cs:152) — subtotal callback at line 169 and shipping callbacks at 192–194. It explicitly guards the payment-fee recursion.
- [AvalaraTaxManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/Services/AvalaraTaxManager.cs:1331) — obtains shipping and subtotal/discount values while constructing the Avalara order.
- [AvalaraTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/AvalaraTaxProvider.cs:93) — provider entry point.

Changes involving `usePaymentMethodAdditionalFee`, tax-rate dictionaries, or subtotal/shipping semantics are particularly risky because they can break the recursion-avoidance protocol.

## Other directly coupled plugins

These inject and invoke `IOrderTotalCalculationService`, so signature or tuple changes are compile-time impacts:

- [UPSService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Shipping.UPS/Services/UPSService.cs:568) — subtotal for rate requests.
- [MarketingAutomationManager.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Brevo/Services/MarketingAutomationManager.cs:139) — subtotal, tax, shipping, and total.
- [OmnisendService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendService.cs:458) and [OmnisendEventsService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendEventsService.cs:237) — cart total.
- [FacebookPixelService.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Widgets.FacebookPixel/Services/FacebookPixelService.cs:818) — cart total used as pixel value.

## Tests

Highest-priority tests:

- [OrderTotalCalculationServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderTotalCalculationServiceTests.cs:24) — direct contract suite covering subtotal, shipping, tax, total, discounts, gift cards, and rewards.
- [FixedRateTestTaxProvider.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Tax/FixedRateTestTaxProvider.cs:48) — test tax provider that calls the contract for subtotal and shipping at lines 65 and 85–86.
- [ServiceTest.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/ServiceTest.cs:45) — activates that test provider.
- [BaseNopTest.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/BaseNopTest.cs:374) — test DI registration.
- [ShoppingCartModelFactoryTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/ShoppingCartModelFactoryTests.cs:139) — verifies displayed order totals.
- [CheckoutModelFactoryTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/CheckoutModelFactoryTests.cs:153) — verifies payment/reward-point checkout behavior.

Secondary regression candidates are [OrderProcessingServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderProcessingServiceTests.cs), [PaymentServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Payments/PaymentServiceTests.cs), and [TaxServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Tax/TaxServiceTests.cs), depending on whether the change is semantic rather than signature-only.

Graphify independently located the PayPal, UPS, Avalara, fixed-tax-test-provider, and direct service-test relationships; the relationships above were then verified at their source call sites.

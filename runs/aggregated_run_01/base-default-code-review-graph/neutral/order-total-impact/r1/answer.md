A change to `IOrderTotalCalculationService` has a broad checkout blast radius. Existing method signature or tuple-return changes will cause compile-time failures in direct consumers; calculation-semantic changes additionally affect order placement, displayed totals, payment reconciliation, discounts, and tax-provider behavior.

## Contract and implementation

- [IOrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/IOrderTotalCalculationService.cs:10) — contract for subtotal, shipping, tax, total, payment fees, reward points, and order-total updates.
- [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:24) — sole implementation found.
- [NopStartup.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web.Framework/Infrastructure/NopStartup.cs:201) — production DI registration.
- [BaseNopTest.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/BaseNopTest.cs:374) — test DI registration.

The implementation directly depends on discount, payment, price, shipping, tax, gift-card, reward-point, and shopping-cart services at [OrderTotalCalculationService.cs:28](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:28).

## Core services and checkout

- [OrderProcessingService.cs:339](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:339) is the highest-impact consumer. During order placement it calculates:
  - subtotal and subtotal discounts at line 343;
  - shipping totals at line 361;
  - tax at line 382;
  - final total, order discount, gift cards, and reward points at line 393;
  - then assigns the result to the payment request at line 410.
- [CheckoutModelFactory.cs:203](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:203) adjusts pickup/shipping rates; [line 514](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/CheckoutModelFactory.cs:514) calculates totals for reward-point/payment selection.
- [ShoppingCartModelFactory.cs:1183](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/ShoppingCartModelFactory.cs:1183) builds displayed subtotal, shipping, tax, total, gift-card, discount, and reward-point values.
- [OrderModelFactory.cs:783](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Factories/OrderModelFactory.cs:783) consumes reward-point conversion methods.
- [CheckoutController.cs:949](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:949) is transitively affected through the checkout factory; order placement reaches the calculation through `OrderProcessingService` at [line 1332](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1332).

## Payment code and plugins

There is an important bidirectional relationship:

`OrderTotalCalculationService → PaymentService → active payment plugin`

- Final totals request the selected method’s handling fee at [OrderTotalCalculationService.cs:1355](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:1355).
- [PaymentService.cs:140](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Payments/PaymentService.cs:140) loads the payment plugin and calls `IPaymentMethod.GetAdditionalHandlingFeeAsync`.
- These plugins call back into the contract to calculate their fee:
  - [CheckMoneyOrderPaymentProcessor.cs:104](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.CheckMoneyOrder/CheckMoneyOrderPaymentProcessor.cs:104)
  - [ManualPaymentProcessor.cs:119](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.Manual/ManualPaymentProcessor.cs:119)
- [PayPalCommerceServiceManager.cs:231](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:231) directly consumes subtotal, total, shipping, tax, and adjusted shipping rates. Its total-matching check at [line 2146](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Payments.PayPalCommerce/Services/PayPalCommerceServiceManager.cs:2146) is especially sensitive to rounding or inclusion-rule changes.

## Discount code

`OrderTotalCalculationService → DiscountService → discount requirement plugins`

- Subtotal, shipping, and order-total discounts call `GetAllDiscountsAsync`, `ValidateDiscountAsync`, and `GetPreferredDiscount` at [OrderTotalCalculationService.cs:115](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:115), [line 154](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:154), and [line 195](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:195).
- [DiscountService.cs:492](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/DiscountService.cs:492) validates discounts.
- Requirement validation dynamically loads plugins at [DiscountService.cs:102](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Discounts/DiscountService.cs:102), including [CustomerRoleDiscountRequirementRule.cs:53](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.DiscountRules.CustomerRoles/CustomerRoleDiscountRequirementRule.cs:53).

Requirement plugins are behavioral rather than compile-time dependents unless the discount-facing contract also changes.

## Tax code and plugins

Another important cycle exists:

`OrderTotalCalculationService → TaxService → tax provider → IOrderTotalCalculationService`

- Product, checkout-attribute, shipping, payment-fee, and aggregate tax calculations occur throughout [OrderTotalCalculationService.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Orders/OrderTotalCalculationService.cs:866).
- [TaxService.cs:731](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Libraries/Nop.Services/Tax/TaxService.cs:731) loads the primary `ITaxProvider` and delegates the tax-total calculation.
- [FixedOrByCountryStateZipTaxProvider.cs:168](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.FixedOrByCountryStateZip/FixedOrByCountryStateZipTaxProvider.cs:168) calls back for subtotal tax-rate buckets; lines 191–194 call back for shipping totals.
- [AvalaraTaxManager.cs:1331](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/Services/AvalaraTaxManager.cs:1331) consumes shipping totals and [line 1343](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Tax.Avalara/Services/AvalaraTaxManager.cs:1343) consumes subtotal discount data.

Tuple ordering, tax-inclusion flags, or payment-fee behavior must remain consistent across this cycle.

## Other directly coupled plugins

- [UPSService.cs:568](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Shipping.UPS/Services/UPSService.cs:568) — subtotal used in rate requests.
- [MarketingAutomationManager.cs:139](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Brevo/Services/MarketingAutomationManager.cs:139) — subtotal, tax, shipping, total.
- [OmnisendEventsService.cs:237](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendEventsService.cs:237) and [OmnisendService.cs:458](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Misc.Omnisend/Services/OmnisendService.cs:458) — cart totals.
- [FacebookPixelService.cs:818](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Plugins/Nop.Plugin.Widgets.FacebookPixel/Services/FacebookPixelService.cs:818) — purchase/cart value.

## Tests to update or run

Highest priority:

- [OrderTotalCalculationServiceTests.cs:176](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderTotalCalculationServiceTests.cs:176) — direct coverage for subtotals, checkout attributes, discounts, free shipping, shipping tax, total tax, final total, reward points, and rounding.
- [FixedRateTestTaxProvider.cs:65](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Tax/FixedRateTestTaxProvider.cs:65) — test tax provider directly consumes subtotal and shipping tuple results.
- [ShoppingCartModelFactoryTests.cs:139](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/ShoppingCartModelFactoryTests.cs:139) — verifies rendered total and earned reward points.
- [CheckoutModelFactoryTests.cs:141](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Web.Tests/Public/Factories/CheckoutModelFactoryTests.cs:141) — shipping/payment/reward-point checkout behavior.
- [OrderProcessingServiceTests.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__order-total-impact__r1/src/Tests/Nop.Tests/Nop.Services.Tests/Orders/OrderProcessingServiceTests.cs:198) — behavioral regression suite for order/payment processing.

The most critical verification path is: cart display → shipping selection → payment fee → tax provider → discount/gift-card/reward-point application → `PlaceOrderAsync` → payment-provider total reconciliation.

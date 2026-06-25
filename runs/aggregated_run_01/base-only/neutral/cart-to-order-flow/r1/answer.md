## End-to-end call chain

```text
Add-to-cart HTTP endpoint
  → ShoppingCartController
  → ShoppingCartService.AddToCartAsync
  → ShoppingCartItem repository insert/update
  → CheckoutController
  → OrderProcessingService.PlaceOrderAsync
  → OrderService.InsertOrderAsync
  → one OrderItem per ShoppingCartItem
  → OrderService.InsertOrderItemAsync
  → ShoppingCartService.ClearShoppingCartAsync
```

### 1. Cart mutation

The AJAX routes map catalog and product-detail requests to `ShoppingCartController`:

- [RouteProvider.cs:202](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:202)
- [ShoppingCartController.cs:593](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:593) — catalog endpoint.
- [ShoppingCartController.cs:791](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:791) — product-details endpoint, including selected attributes, entered price, quantity, and rental dates.

Catalog products call `_shoppingCartService.AddToCartAsync(...)` directly at [ShoppingCartController.cs:688](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:688). Product-detail requests parse the form and pass it through `SaveItemAsync`, which calls the same service at [ShoppingCartController.cs:304](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:304).

`ShoppingCartService.AddToCartAsync` is the persistence boundary:

- [ShoppingCartService.cs:1549](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1549) — validates permissions and quantity, resets stale checkout data, and loads the customer cart.
- [ShoppingCartService.cs:1587](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1587) — finds an equivalent existing item.
- [ShoppingCartService.cs:1615](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1615) — updates its quantity when found.
- [ShoppingCartService.cs:1658](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1658) — constructs a new `ShoppingCartItem` containing customer, product, store, attributes, quantity, and rental information.
- [ShoppingCartService.cs:1673](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1673) — inserts it through `_sciRepository`.

Subsequent cart reads query these persisted items by `CustomerId`, cart type, store, product, and dates at [ShoppingCartService.cs:753](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:753).

### 2. Checkout and placement

Checkout validates both the complete cart and every item, then routes to one-page checkout or billing-address checkout at [CheckoutController.cs:380](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:380).

Both checkout modes converge on placement:

- Multi-page `ConfirmOrder`: [CheckoutController.cs:1278](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1278), calling `PlaceOrderAsync` at line 1332.
- One-page `OpcConfirmOrder`: [CheckoutController.cs:2018](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2018), calling it at line 2072.

Both populate `ProcessPaymentRequest` with customer, store, payment method, and generated order identity before the call.

### 3. Order header creation

`PlaceOrderAsync` starts at [OrderProcessingService.cs:1567](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1567).

It first calls `PreparePlaceOrderDetailsAsync`, which validates customer, billing/shipping data, totals, and cart at [OrderProcessingService.cs:282](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:282). The cart is reloaded and revalidated immediately before placement at [OrderProcessingService.cs:483](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:483).

After successful payment processing, placement calls:

1. `SaveOrderDetailsAsync`
2. `MoveShoppingCartItemsToOrderItemsAsync`
3. discount/gift-card history and notifications
4. checkout-data reset and `OrderPlacedEvent`

This sequence is explicit at [OrderProcessingService.cs:1580](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1580).

`SaveOrderDetailsAsync` builds the `Order` header from calculated totals, addresses, currency, payment, shipping, and customer state at [OrderProcessingService.cs:717](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:717), then persists it at line 802.

`OrderService` delegates that operation to `_orderRepository.InsertAsync(order)` at [OrderService.cs:357](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:357).

### 4. Order-item persistence

`MoveShoppingCartItemsToOrderItemsAsync` loops over the validated `details.Cart` at [OrderProcessingService.cs:1276](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1276).

For each `ShoppingCartItem`, it:

- Recalculates unit price, subtotal, tax, discounts, attributes, and weight.
- Creates an `OrderItem` with `OrderId = order.Id`, `ProductId`, prices, attributes, quantity, weight, and rental dates.
- Persists it with `_orderService.InsertOrderItemAsync(orderItem)` at [OrderProcessingService.cs:1327](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1327).
- Adjusts inventory and publishes `ShoppingCartItemMovedToOrderItemEvent`.

`OrderService.InsertOrderItemAsync` delegates to `_orderItemRepository.InsertAsync(orderItem)` at [OrderService.cs:748](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:748). The database mapping confirms `OrderItem.OrderId` and `ProductId` are foreign keys at [OrderItemBuilder.cs:17](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Data/Mapping/Builders/Orders/OrderItemBuilder.cs:17).

### 5. Cart cleanup

Only after every cart item has been converted and persisted, `MoveShoppingCartItemsToOrderItemsAsync` calls:

```csharp
await _shoppingCartService.ClearShoppingCartAsync(details.Customer, order.StoreId);
```

at [OrderProcessingService.cs:1339](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1339).

`ClearShoppingCartAsync` reloads that customer/store shopping cart and bulk-deletes the `ShoppingCartItem` records at [ShoppingCartService.cs:656](/home/simon/repos/lorq-worktrees/base-only__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:656). It then publishes `ClearShoppingCartEvent` and recalculates the customer’s `HasShoppingCartItems` optimization flag.

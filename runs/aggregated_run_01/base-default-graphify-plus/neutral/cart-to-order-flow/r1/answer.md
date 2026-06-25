## End-to-end flow

```text
Add-product HTTP action
  → ShoppingCartService.AddToCartAsync
  → ShoppingCartItem repository insert/update
  → ShoppingCartController.StartCheckout
  → CheckoutController.ConfirmOrder / OpcConfirmOrder
  → OrderProcessingService.PlaceOrderAsync
  → SaveOrderDetailsAsync → Order persistence
  → MoveShoppingCartItemsToOrderItemsAsync
      → OrderItem persistence
      → inventory adjustment
      → cart cleanup
  → checkout-data reset and OrderPlacedEvent
  → payment post-processing / completed page
```

### 1. Cart mutation

AJAX routes map catalog and product-detail requests to `ShoppingCartController`:

- [`RouteProvider.cs:202`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:202)
- [`RouteProvider.cs:207`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:207)

The catalog action validates the product, loads the customer’s cart, and calls `IShoppingCartService.AddToCartAsync`:

- `AddProductToCart_Catalog`: [`ShoppingCartController.cs:593`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:593)
- Service call: [`ShoppingCartController.cs:688`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:688)

The product-details action parses attributes, rental dates, price and quantity, then delegates through `SaveItemAsync`:

- `AddProductToCart_Details`: [`ShoppingCartController.cs:791`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:791)
- `SaveItemAsync` invocation: [`ShoppingCartController.cs:870`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:870)
- `SaveItemAsync` calls `AddToCartAsync` for new items: [`ShoppingCartController.cs:305`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:305)

`ShoppingCartService.AddToCartAsync` resets stale checkout data, validates the item, and either:

- updates an equivalent existing `ShoppingCartItem` via `_sciRepository.UpdateAsync`: [`ShoppingCartService.cs:1549`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1549), [`ShoppingCartService.cs:1615`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1615)
- or constructs and inserts a new `ShoppingCartItem` via `_sciRepository.InsertAsync`: [`ShoppingCartService.cs:1648`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1648), [`ShoppingCartService.cs:1673`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1673)

The persisted cart entity is defined in [`ShoppingCartItem.cs:6`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Core/Domain/Orders/ShoppingCartItem.cs:6).

### 2. Entering checkout

Posting the cart with the `checkout` form value invokes `StartCheckout`:

- [`ShoppingCartController.cs:1354`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:1354)

It reloads the customer/store cart, persists checkout attributes, validates the entire cart, and then redirects either to normal checkout or guest authentication.

Normal checkout’s confirm route maps to `CheckoutController.Confirm`; one-page checkout maps to `OnePageCheckout`:

- [`RouteProvider.cs:284`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:284)
- [`RouteProvider.cs:316`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Infrastructure/RouteProvider.cs:316)

### 3. Order placement entry points

Both checkout modes converge on `IOrderProcessingService.PlaceOrderAsync`:

- Multi-step `ConfirmOrder`: [`CheckoutController.cs:1278`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1278), call at [`CheckoutController.cs:1332`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1332)
- One-page `OpcConfirmOrder`: [`CheckoutController.cs:2019`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2019), call at [`CheckoutController.cs:2072`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2072)

Before that call, the controller retrieves the payment request, assigns customer, store and payment-method data, and persists it as a customer attribute. The storage implementation is at [`OrderProcessingService.cs:3289`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:3289).

### 4. Order creation

`PlaceOrderAsync` is the central orchestrator:

- [`OrderProcessingService.cs:1567`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1567)

`PreparePlaceOrderDetailsAsync` reloads and validates customer, addresses, shipping and totals. Critically, it reloads the authoritative cart from persistence rather than relying on the controller’s copy:

- [`OrderProcessingService.cs:282`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:282)
- Cart reload and validation: [`OrderProcessingService.cs:483`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:483), [`OrderProcessingService.cs:490`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:490)

After successful payment processing, `PlaceOrderAsync` calls `SaveOrderDetailsAsync`, then moves cart items:

- [`OrderProcessingService.cs:1589`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1589)
- [`OrderProcessingService.cs:1594`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1594)

`SaveOrderDetailsAsync` constructs the `Order`, snapshots totals, tax, payment, shipping and checkout data, persists address snapshots, and inserts the order:

- [`OrderProcessingService.cs:717`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:717)
- `_orderService.InsertOrderAsync(order)`: [`OrderProcessingService.cs:802`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:802)
- Repository delegation: [`OrderService.cs:357`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:357)

### 5. Order-item persistence

`MoveShoppingCartItemsToOrderItemsAsync` iterates the prepared cart. For every `ShoppingCartItem`, it recalculates prices and tax, formats attributes, and creates an `OrderItem` snapshot containing product, quantity, prices, discounts, attributes, weight and rental dates:

- [`OrderProcessingService.cs:1276`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1276)
- Order-item construction: [`OrderProcessingService.cs:1301`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1301)
- Persistence call: [`OrderProcessingService.cs:1327`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1327)
- Repository delegation: [`OrderService.cs:748`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:748)

After insertion it creates gift cards when applicable, adjusts inventory, and publishes `ShoppingCartItemMovedToOrderItemEvent` at [`OrderProcessingService.cs:1336`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1336).

### 6. Cart and checkout cleanup

Only after all order items have been processed does the method clear the cart:

- Call: [`OrderProcessingService.cs:1339`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1339)
- Implementation: [`ShoppingCartService.cs:656`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:656)

`ClearShoppingCartAsync` reloads the customer’s shopping cart for that store, bulk-deletes it through `_sciRepository`, publishes `ClearShoppingCartEvent`, and updates `Customer.HasShoppingCartItems`:

- Delete: [`ShoppingCartService.cs:663`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:663)
- Event: [`ShoppingCartService.cs:664`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:664)

Finally, `PlaceOrderAsync` clears coupons and checkout attributes, records customer activity, publishes `OrderPlacedEvent`, and checks the resulting order/payment status at [`OrderProcessingService.cs:1608`](/home/simon/repos/lorq-worktrees/base-default-graphify-plus__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1608). The controller then performs payment post-processing and redirects to checkout completion.

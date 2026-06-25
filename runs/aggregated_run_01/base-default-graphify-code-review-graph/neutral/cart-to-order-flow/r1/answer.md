## End-to-end call flow

```text
ShoppingCartController
  → ShoppingCartService.AddToCartAsync
  → ShoppingCartItem repository

ShoppingCartController.StartCheckout
  → CheckoutController
  → ConfirmOrder / OpcConfirmOrder
  → OrderProcessingService.PlaceOrderAsync
      → SaveOrderDetailsAsync
      → MoveShoppingCartItemsToOrderItemsAsync
          → OrderService.InsertOrderItemAsync
      → ShoppingCartService.ClearShoppingCartAsync
```

### 1. Cart mutation

Products enter through:

- Catalog AJAX: [`AddProductToCart_Catalog`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:593), which calls `AddToCartAsync` at [line 688](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:688).
- Product details: [`AddProductToCart_Details`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:791) → `SaveItemAsync` at line 870 → [`ShoppingCartService.AddToCartAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1549).

`AddToCartAsync` resets checkout state, validates the item, and either:

- Updates a matching `ShoppingCartItem` through `_sciRepository.UpdateAsync` at [line 1615](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1615).
- Constructs and inserts a new `ShoppingCartItem` through `_sciRepository.InsertAsync` at [line 1673](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1673).

The persisted cart row carries `CustomerId`, `ProductId`, cart type, quantity, attributes, price, rental dates, and store ID.

### 2. Checkout entry and confirmation

[`StartCheckout`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:1352) saves checkout attributes, validates the cart, and redirects to the checkout route at line 1379.

[`CheckoutController.Index`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:342) reloads and validates the cart, then selects:

- One-page checkout at [line 409](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:409).
- Standard billing/shipping/payment checkout at line 411.

The standard flow stores payment information and redirects to confirmation at [lines 1242–1243](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1242).

Order placement has two terminal actions:

- Standard [`ConfirmOrder`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1278) calls `PlaceOrderAsync` at [line 1332](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1332).
- One-page [`OpcConfirmOrder`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2019) makes the same call at [line 2072](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2072).

### 3. Order and order-item persistence

[`OrderProcessingService.PlaceOrderAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1567):

1. `PreparePlaceOrderDetailsAsync` validates the customer, addresses, totals and reloads the current cart through [`PrepareAndValidateShoppingCartAndCheckoutAttributesAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:483).
2. After successful payment processing, calls `SaveOrderDetailsAsync` at line 1589.
3. Calls `MoveShoppingCartItemsToOrderItemsAsync` at line 1594.

[`SaveOrderDetailsAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:717) constructs the `Order` and calls `_orderService.InsertOrderAsync` at [line 802](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:802). [`OrderService.InsertOrderAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:357) delegates to `_orderRepository.InsertAsync`.

[`MoveShoppingCartItemsToOrderItemsAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1276) loops over the captured cart. Each `OrderItem` receives:

- `OrderId = order.Id`
- The cart item’s product, quantity, attributes and rental dates
- Calculated unit prices, subtotals, discounts, tax and weight

It calls `_orderService.InsertOrderItemAsync` at [line 1327](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1327). [`OrderService.InsertOrderItemAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:748) persists through `_orderItemRepository.InsertAsync`.

### 4. Cart cleanup

After every cart item has been converted and persisted, `MoveShoppingCartItemsToOrderItemsAsync` calls `ClearShoppingCartAsync` at [line 1339](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1339).

[`ShoppingCartService.ClearShoppingCartAsync`](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:656):

- Reloads the customer’s shopping cart for the order’s store.
- Bulk-deletes its rows at [line 663](/home/simon/repos/lorq-worktrees/base-default-graphify-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:663).
- Publishes `ClearShoppingCartEvent`.
- Recalculates `Customer.HasShoppingCartItems`.

Thus cleanup is a bulk `ClearShoppingCartAsync`, not repeated `DeleteShoppingCartItemAsync` calls. Checkout attributes and coupon state are then reset by `PlaceOrderAsync`.

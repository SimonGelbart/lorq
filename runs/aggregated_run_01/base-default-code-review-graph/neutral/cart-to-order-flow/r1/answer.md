## End-to-end flow

```text
ShoppingCartController
  → ShoppingCartService.AddToCartAsync
  → ShoppingCartItem repository

ShoppingCartController.StartCheckout
  → CheckoutController.Index
  → billing / shipping / payment
  → ConfirmOrder or OpcConfirmOrder

CheckoutController
  → OrderProcessingService.PlaceOrderAsync
      → PreparePlaceOrderDetailsAsync
      → SaveOrderDetailsAsync
          → OrderService.InsertOrderAsync
      → MoveShoppingCartItemsToOrderItemsAsync
          → OrderService.InsertOrderItemAsync (one per cart row)
          → ShoppingCartService.ClearShoppingCartAsync
      → ResetCheckoutDataAsync
```

### 1. Cart mutation

The public entry points are in [ShoppingCartController.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:593):

- `AddProductToCart_Catalog` validates the catalog request and calls `_shoppingCartService.AddToCartAsync` at line 688.
- `AddProductToCart_Details` parses product attributes, quantity, entered price and rental dates, then calls `SaveItemAsync` at line 870.
- `SaveItemAsync` delegates new items to `AddToCartAsync` and existing items to `UpdateShoppingCartItemAsync` at lines 305–329.

[ShoppingCartService.AddToCartAsync](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:1549):

1. Resets prior checkout state.
2. Loads the customer/store cart.
3. Searches for an equivalent cart row.
4. Updates its quantity through `_sciRepository.UpdateAsync`, or constructs a `ShoppingCartItem` at line 1657 and persists it through `_sciRepository.InsertAsync` at line 1673.
5. Updates `Customer.HasShoppingCartItems`.

The persisted cart entity is [ShoppingCartItem.cs](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Core/Domain/Orders/ShoppingCartItem.cs).

### 2. Entering checkout

[ShoppingCartController.StartCheckout](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/ShoppingCartController.cs:1354) reloads the cart, saves and validates checkout attributes, handles guest/login requirements, and redirects to the checkout route.

[CheckoutController.Index](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:341) validates the cart and each cart item, resets stale checkout state, then selects:

- one-page checkout, or
- standard checkout beginning with billing address.

The standard actions subsequently collect billing, shipping, shipping method, payment method, and payment information in the same file. `EnterPaymentInfo` saves the payment plugin’s `ProcessPaymentRequest` and redirects to confirmation at lines 1203–1243.

### 3. Confirmation and order placement

Both checkout variants converge on the same service call:

- [ConfirmOrder](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:1278) calls `PlaceOrderAsync` at line 1332.
- [OpcConfirmOrder](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Presentation/Nop.Web/Controllers/CheckoutController.cs:2019) calls it at line 2072.

Before calling it, the controller restores or creates the `ProcessPaymentRequest`, assigns the store, customer and selected payment method, and persists it with `SetProcessPaymentRequestAsync`.

[OrderProcessingService.PlaceOrderAsync](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1567) performs this sequence:

1. `PreparePlaceOrderDetailsAsync`.
2. Process payment.
3. `SaveOrderDetailsAsync`.
4. `MoveShoppingCartItemsToOrderItemsAsync`.
5. Save discount/gift-card usage and recurring-payment data.
6. Send notifications.
7. Reset checkout data and publish `OrderPlacedEvent`.

`PreparePlaceOrderDetailsAsync` starts at line 282. Its cart preparation reloads the customer’s `ShoppingCart` rows, rejects an empty cart, revalidates every item, and calculates subtotal, shipping, tax, discounts, rewards and total.

### 4. Order persistence

[SaveOrderDetailsAsync](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:717) maps the prepared customer, totals, payment, shipping, tax and checkout data into an `Order`.

After cloning and inserting the relevant addresses, it calls `_orderService.InsertOrderAsync(order)` at line 802 and subsequently updates the generated custom order number.

[OrderService.InsertOrderAsync](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:357) directly persists it through `_orderRepository.InsertAsync`.

### 5. Order-item persistence and cart cleanup

[MoveShoppingCartItemsToOrderItemsAsync](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderProcessingService.cs:1276) iterates over the previously prepared cart:

- Recalculates unit price, subtotal, tax and discounts.
- Copies product ID, quantity, attributes, rental dates and weight into an `OrderItem`.
- Associates it using `OrderId = order.Id`.
- Calls `_orderService.InsertOrderItemAsync(orderItem)` at line 1327.
- Adjusts inventory and publishes `ShoppingCartItemMovedToOrderItemEvent`.

[OrderService.InsertOrderItemAsync](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/OrderService.cs:748) persists each item through `_orderItemRepository.InsertAsync`.

Only after every cart row has been converted does `MoveShoppingCartItemsToOrderItemsAsync` call `ClearShoppingCartAsync`.

[ShoppingCartService.ClearShoppingCartAsync](/home/simon/repos/lorq-worktrees/base-default-code-review-graph__neutral__cart-to-order-flow__r1/src/Libraries/Nop.Services/Orders/ShoppingCartService.cs:656):

- Reloads the customer/store shopping cart.
- Bulk-deletes its rows through `_sciRepository.DeleteAsync`.
- Publishes `ClearShoppingCartEvent`.
- Recalculates `Customer.HasShoppingCartItems`.

Thus the cart is cleared after order and order-item persistence, while broader checkout state—payment request, coupons, checkout attributes, selected shipping/payment data—is reset afterward by `PlaceOrderAsync`.

The graph MCP entry point was unavailable due repeated cancellation, so the relationships above were traced and verified directly against source.

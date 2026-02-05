# Guest Checkout and Order History Implementation Plan

This document outlines the plan to implement a guest checkout feature, allowing unauthenticated users to place orders and view their order history.

## 1. Backend Modifications

### 1.1. Allow Order Creation for Guests

The current `Order` entity already supports a nullable `UserId`, which is perfect for this use case. We will leverage the `GuestEmail` field for communication.

- **`OrdersController.cs`**:
    - The existing `POST /api/orders` endpoint will be used.
    - No changes are needed in the controller itself, as the logic will be handled by the `OrderService`.

- **`OrderService.cs`**:
    - The `CreateOrderAsync` method will be updated.
    - If the `ICurrentUserService.UserId` is `null`, the service will treat it as a guest order.
    - It will validate that the `GuestEmail` is provided in the `OrderCreateDto`.
    - The `Order` will be saved with a `null` `UserId` and the provided `GuestEmail`.

### 1.2. Retrieve Guest Order History

A new endpoint will be created to allow guests to retrieve their order history using the order IDs stored in their browser.

- **`OrdersController.cs`**:
    - Create a new `[HttpPost("guest")]` endpoint. We use `POST` to allow sending a list of IDs in the request body, which is cleaner than a long query string.
    - This endpoint will not require authentication.
    - It will accept a DTO containing a list of `Guid`s (the order IDs).
    - It will call a new service method, `GetGuestOrdersAsync`.

- **`OrderService.cs`**:
    - Implement a new `GetGuestOrdersAsync(IEnumerable<Guid> orderIds)` method.
    - This method will query the database for orders that match the provided IDs.
    - **Security**: It will also verify that the `UserId` for these orders is `null` to ensure registered users' orders are not accidentally exposed.
    - It will return a list of `OrderDto`s.

### 1.3. Claim Guest Orders on Login/Registration

When a guest user logs in or creates an account, we need to associate their past guest orders with their new user account.

- **`OrdersController.cs`**:
    - Create a new `[HttpPost("claim")]` endpoint.
    - This endpoint will require authentication.
    - It will accept a DTO containing a list of `Guid`s (the order IDs from the browser).
    - It will call a new service method, `ClaimOrdersAsync`.

- **`OrderService.cs`**:
    - Implement `ClaimOrdersAsync(Guid userId, IEnumerable<Guid> orderIds)`.
    - The method will iterate through the provided order IDs.
    - For each order, it will verify that the order exists and that its `UserId` is currently `null`.
    - It will then update the `UserId` of the order to the `userId` of the currently authenticated user.
    - The `GuestEmail` on the claimed orders can optionally be cleared.

## 2. Frontend Modifications (Conceptual)

The frontend will need to be updated to handle the client-side logic.

### 2.1. Storing Order IDs

- When a guest user successfully places an order, the frontend application must get the `orderId` from the API response.
- The `orderId` should be stored in the browser's `localStorage`. A good approach is to maintain a JSON array of order IDs.
    - **Example `localStorage` item**: `guestOrderIds: '["guid-1", "guid-2"]'`

### 2.2. Displaying Guest Order History

- A dedicated "Order History" or "Track Order" page should check if a user is authenticated.
- If the user is a guest, the page should:
    1.  Read the order IDs from `localStorage`.
    2.  If IDs exist, make a `POST` request to the `/api/orders/guest` endpoint with the IDs in the request body.
    3.  Display the returned order information.

### 2.3. Claiming Orders After Login

- After a user successfully logs in or registers, the application should immediately check `localStorage` for `guestOrderIds`.
- If any IDs are found:
    1.  Make a `POST` request to the `/api/orders/claim` endpoint with the IDs in the request body.
    2.  On a successful response from the API, the application should clear the `guestOrderIds` from `localStorage` to prevent the same orders from being claimed again.
    3.  The user can then be redirected to their main account's order history page, which will now include their newly claimed orders.

This plan provides a complete roadmap for implementing the guest checkout feature in a secure and robust way.

# Controller Rules

## Scope

Applies to: API controllers, action methods, validation pipeline, authorization attributes, request/response mapping.

---

## Hard Rules

1. **Controllers are thin.** A controller action does three things: validate input shape (via `[ValidationFilter]`), dispatch the command or query through MediatR, and map the result to `ApiResponse<T>`. No business logic, no repository calls, no conditional branching beyond result mapping.

2. **All write endpoints use `[ValidationFilter]`.** Every action that accepts a DTO for write operations must have `[ValidationFilter]` applied. This runs FluentValidation before the handler is invoked.

3. **Every write command has a FluentValidation validator.** `{CommandName}CommandValidator : AbstractValidator<{CommandName}Command>` in the same folder as the command. Validates input shape: not-empty strings, positive numbers, valid GUIDs. Commands that accept user-typed strings or numbers always have a validator.

4. **ValidationBehavior returns `Result.Fail`, it does not throw.** The pipeline behavior returns `Result.Fail(new DomainError("VALIDATION_FAILED", message))` on failure. It never throws `ValidationException`. Exceptions are reserved for infrastructure failures.

5. **Three distinct validation layers — each does one job.**
   - `[FromBody]` binding: null / wrong types → 400 (framework, automatic)
   - `ValidationBehavior`: empty strings, ranges, required fields → 400 (FluentValidation)
   - Handler existence checks: does the referenced entity exist? → 404
   - Aggregate domain methods: business invariants, state machine rules → `Result.Fail` → 422 / 409

6. **Controller guards authentication; handler guards ownership.**
   - `[Authorize(Roles = "Admin")]` on the action → 401 / 403 for all-or-nothing role access.
   - `ICurrentUserService` in the handler → ownership checks (user can only act on their own data).
   - Do not duplicate: if the controller already guards a role, the handler does not also check that role.

7. **Controllers return `ApiResponse<T>`.** All responses are wrapped in the project's standard `ApiResponse<T>` envelope. No raw `Ok(data)` or `BadRequest(message)` without the envelope.

---

## Allowed Patterns

- Reading route or query parameters in the controller and passing them into the command/query record.
- Mapping HTTP status codes from `Result` error codes in a shared result-mapping helper.
- Thin `[FromQuery]` model binding for list/filter queries.

---

## Forbidden Patterns

- Business logic or conditional branching in a controller action.
- Direct repository or DbContext access from a controller.
- `throw` for expected failures — use `Result.Fail` and let the pipeline translate.
- Returning raw data without `ApiResponse<T>`.
- Skipping `[ValidationFilter]` on a write endpoint that accepts user input.
- Duplicating role checks in both the controller attribute and the handler.

---

## Required Tests

- Integration test per endpoint: valid input → correct response shape and status code.
- Integration test: invalid input → 400 with validation message.
- Integration test: unauthorized caller → 401 or 403.
- Integration test: not-found resource → 404.

---

## Required Evidence (before merge)

- `[ValidationFilter]` present on all write endpoints.
- FluentValidation validator present for all commands with user-typed input.
- No business logic visible in controller action bodies.

---

## Definition of Done

All hard rules pass. Required tests are green. Required evidence is attached in the PR.

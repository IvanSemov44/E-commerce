# Code Style Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Define cross-project style conventions that improve consistency and readability.

## General Rules
1. Follow established project formatter/linter output.
2. Use clear names over abbreviations.
3. Keep methods/functions focused and intention-revealing.
4. Prefer explicit types/contracts at module boundaries.

## Backend Notes
- Keep async methods cancellation-aware.
- Keep controllers thin; business logic in services.

## Frontend Notes
- Type props/hook outputs explicitly.
- Keep server state in RTK Query, UI state in slices/local state.

## References
- `.ai/backend/services.md`
- `.ai/frontend/type-safety.md`
- `.ai/reference/common-mistakes.md`

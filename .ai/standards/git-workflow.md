# Git Workflow Standard

Updated: 2026-03-08
Owner: @ivans

## Purpose
Keep commits reviewable and documentation synchronized with pattern changes.

## Core Rules
1. Prefer small, coherent commits by concern.
2. Keep commit messages clear and actionable.
3. If behavior/pattern changes, update `.ai` docs in same PR.
4. Avoid mixing unrelated refactors with feature/bugfix logic.

## PR Checklist Additions
- [ ] Code follows established architecture patterns.
- [ ] Tests added/updated where behavior changed.
- [ ] Relevant `.ai` docs updated when rules/patterns changed.

## References
- `CONTRIBUTING.md`
- `.ai/standards/documentation.md`

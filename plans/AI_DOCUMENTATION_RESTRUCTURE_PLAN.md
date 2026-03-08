# AI Documentation Architecture Restructure Plan

**Created**: March 8, 2026  
**Status**: Planning Phase  
**Estimated Effort**: 14-18 hours (parallelizable)  
**Goal**: Transform scattered documentation into enterprise-level AI knowledge base

---

## Executive Summary

**Problem**: 32+ markdown files scattered across 4+ locations with significant duplication, no clear navigation, and no single entry point for AI assistants.

**Solution**: Create modular `.ai/` directory with hierarchical organization (6 thematic subdirectories), split monolithic guides into focused topic modules, establish navigable README hub, and add tool-specific adapter files so assistants reliably discover the library.

**Benefits**: 
- Single source of truth for AI assistants
- Zero duplication through cross-referencing
- Easy maintenance (update one file per topic)
- Scalable structure for future growth
- Tool-aware by design (explicit adapters for Copilot, Claude, Cursor, and Cline)

---

## Current State Analysis

### Documentation Inventory (32 Files)

**Root-level (8 files):**
- `.github/copilot-instructions.md` — Main AI instructions (200+ lines)
- `BACKEND_CODING_GUIDE.md` — Detailed backend rules (1200+ lines)
- `DEPLOYMENT.md` — Deployment guide
- `CONTRIBUTING.md` — Contribution guidelines
- `CODE_OF_CONDUCT.md` — Code of conduct
- `DOTNET_ERROR_CHECKING_GUIDE.md` — Error checking
- `plan.md` — Old/outdated plan
- `README.md` — Project overview

**docs/ folder (12 files):**
- Active: `API_DOCUMENTATION_DEBT.md`, `ARCHITECTURE_COLOCATION_PLAN.md`, `CODE_REVIEW.md`, `I18N_REVIEW.md`, `MODERN_DESIGN_SYSTEM.md`
- archives/ subfolder: 6 historical documents

**plans/ folder (2 files):**
- `TEST_COVERAGE_PLAN.md`
- `COMPREHENSIVE_CODE_REVIEW.md`

**Frontend embedded (10+ files in src/frontend/storefront/):**
- `FRONTEND_CODING_GUIDE.md`
- `COLOCATION_*.md` (4 files)
- `COMPREHENSIVE_FRONTEND_IMPLEMENTATION_PLAN.md`
- `TEST_ERRORS_ANALYSIS.md`
- And more

### Critical Problems Identified

1. **No Clear Hierarchy** — Documentation scattered across 4+ locations (root, .github, docs, plans, src/frontend)
2. **Overlapping Content** — copilot-instructions.md duplicates BACKEND_CODING_GUIDE.md concepts
3. **Buried Guides** — Frontend coding guide hidden in `src/` folder (not accessible to AI)
4. **Mixed Concerns** — Active guides mixed with plans, debt tracking, and historical archives
5. **No Navigation** — No index or table of contents linking all documentation
6. **Inconsistent Structure** — Each document has different format, depth, and organization
7. **No Versioning** — Can't tell which docs are current vs outdated
8. **Monolithic Files** — BACKEND_CODING_GUIDE.md is 1200+ lines (impossible to scan efficiently)
9. **No Decision Log** — Architectural decisions not documented
10. **Duplication Tax** — Changes must be made in multiple places

---

## Proposed Solution: `.ai/` Directory Structure

### Directory Layout

```
.ai/
├── README.md                          # Entry point - navigation hub
├── quick-start.md                     # 5-minute onboarding for AI
│
├── architecture/
│   ├── overview.md                    # System architecture summary
│   ├── clean-architecture.md          # Layer rules & dependencies
│   ├── patterns.md                    # Design patterns (Repository, UoW, Result<T>)
│   └── decisions/                     # Architecture Decision Records (ADRs)
│       ├── 001-clean-architecture.md
│       ├── 002-result-pattern.md
│       ├── 003-jwt-authentication.md
│       └── template.md
│
├── backend/
│   ├── overview.md                    # Backend tech stack summary
│   ├── entities.md                    # Entity rules (BaseEntity, null!, virtual props)
│   ├── dtos.md                        # DTO patterns & organization
│   ├── repositories.md                # Repository patterns (specialized vs generic)
│   ├── services.md                    # Service layer rules (UoW, Result<T>, logging)
│   ├── controllers.md                 # API layer conventions
│   ├── validation.md                  # 3-layer validation strategy
│   ├── error-handling.md              # Result<T> pattern, exceptions, error codes
│   ├── database.md                    # Migrations, seeding, EF Core patterns
│   ├── testing.md                     # Test patterns, helpers, integration tests
│   └── api-contracts.md               # Response shapes, pagination, status codes
│
├── frontend/
│   ├── overview.md                    # Frontend architecture overview
│   ├── redux.md                       # Redux + RTK Query patterns
│   ├── components.md                  # Component structure, CSS modules
│   ├── hooks.md                       # Custom hooks patterns
│   ├── routing.md                     # React Router, protected routes
│   ├── api-integration.md             # baseApi pattern, error handling
│   ├── type-safety.md                 # TypeScript conventions
│   ├── testing.md                     # Vitest, RTL, Playwright patterns
│   └── styling.md                     # CSS modules, design tokens
│
├── workflows/
│   ├── adding-feature.md              # Step-by-step feature development
│   ├── database-migrations.md         # How to create/apply migrations
│   ├── testing-strategy.md            # How to write tests
│   ├── deployment.md                  # Deployment procedures
│   └── troubleshooting.md             # Common issues & solutions
│
├── standards/
│   ├── code-style.md                  # Naming, formatting, file organization
│   ├── git-workflow.md                # Branching, commits, PR process
│   ├── documentation.md               # How to document code
│   └── security.md                    # Security practices
│
├── reference/
│   ├── file-structure.md              # Project directory tree explained
│   ├── technologies.md                # Tech stack with versions
│   ├── common-mistakes.md             # Anti-patterns to avoid
│   ├── code-examples.md               # Reference implementations
│   └── glossary.md                    # Terms & acronyms
│
└── tracking/
    ├── technical-debt.md              # Current debt items
    ├── improvements.md                # Future enhancements
    └── archived/                      # Historical documents

# Keep existing:
docs/                                  # Human-facing documentation
plans/                                 # Planning documents (can reference .ai/)
README.md                             # Project README (links to .ai/README.md)
CONTRIBUTING.md                       # Contributor guide (links to .ai/)
```

### Tool Discovery (Critical)

The `.ai/` folder is canonical, but each assistant must have its native entry file that points to it:

- `CLAUDE.md` for Claude Code
- `.github/copilot-instructions.md` for GitHub Copilot
- `.cursorrules` or `.cursor/rules/*` for Cursor (if used)
- `.clinerules` for Cline (if used)

Without these adapters, many assistants will not reliably discover `.ai/README.md`.

### Design Principles

1. **Modular** — One topic per file; keep concise, but do not force artificial line limits
2. **Hierarchical** — Clear folder structure by domain
3. **Scannable** — Use tables, bullet lists, code snippets
4. **Actionable** — Concrete examples, not theory
5. **Consistent** — Same structure across all files
6. **Discoverable** — README.md with full navigation
7. **Versioned** — Last-updated dates on every file
8. **Minimal Duplication** — DRY principle, link instead of repeat
9. **Task-Oriented** — Organized by "what developer needs to do"
10. **Tool-Aware** — Canonical docs in `.ai/` plus native adapter files for each assistant

---

## Standard Templates

### Document Template

Every documentation file should follow this structure:

```markdown
---
last_updated: YYYY-MM-DD
status: active | deprecated | draft
category: backend | frontend | architecture | workflow
related:
  - path/to/related-doc.md
  - path/to/another-doc.md
---

# [Topic Name]

> **TL;DR**: [One-sentence summary of what this doc covers]

## Overview

[2-3 paragraph overview of the topic]

## Rules

### Rule 1: [Concise Rule Name]
**Why**: [Rationale]  
**How**: [Implementation]

```[language]
// Code example
```

**Anti-pattern**:
```[language]
// What NOT to do
```

### Rule 2: [Next Rule]
...

## Examples

### Example 1: [Scenario]
[Step-by-step example with code]

## Verification

- [ ] Checklist item 1
- [ ] Checklist item 2

## See Also
- [Related Topic](../path/to/doc.md)
- [External Resource](https://example.com)
```

### ADR Template

Architecture Decision Records document key architectural choices:

```markdown
# ADR-XXX: [Decision Title]

**Date**: YYYY-MM-DD  
**Status**: proposed | accepted | deprecated | superseded  
**Deciders**: [List of people involved]  
**Supersedes**: [ADR-YYY if applicable]

## Context

[What is the issue we're seeing that is motivating this decision or change?]

## Decision

[What is the change that we're proposing and/or doing?]

## Consequences

### Positive
- [Benefit 1]
- [Benefit 2]

### Negative
- [Drawback 1]
- [Drawback 2]

### Neutral
- [Impact 1]

## Alternatives Considered

### Option 1: [Name]
**Pros**: [List]  
**Cons**: [List]  
**Why rejected**: [Reason]

### Option 2: [Name]
...

## References
- [Link to relevant discussion]
- [Related ADRs]
```

### .ai/README.md Template

```markdown
# AI Assistant Documentation — E-Commerce Platform

> **Last Updated**: [Date]  
> **Status**: Production-Ready

This directory contains comprehensive instructions for AI coding assistants working on this codebase.

## 🚀 Quick Start (Read This First)

1. Read [quick-start.md](quick-start.md) — 5-minute overview
2. Review [architecture/overview.md](architecture/overview.md) — System design
3. Check your domain:
   - Backend: Start with [backend/overview.md](backend/overview.md)
   - Frontend: Start with [frontend/overview.md](frontend/overview.md)

## 📁 Documentation Map

### Architecture
High-level system design and patterns
- [System Overview](architecture/overview.md) — Tech stack, layers, deployment
- [Clean Architecture](architecture/clean-architecture.md) — Layer dependencies
- [Design Patterns](architecture/patterns.md) — Repository, UoW, Result<T>
- [Architecture Decisions](architecture/decisions/) — ADRs for key choices

### Backend (.NET 9 Clean Architecture)
- [Overview](backend/overview.md) — Backend tech stack
- [Entities](backend/entities.md) — Domain model rules
- [DTOs](backend/dtos.md) — Data transfer patterns
- [Repositories](backend/repositories.md) — Data access layer
- [Services](backend/services.md) — Business logic layer
- [Controllers](backend/controllers.md) — API layer
- [Validation](backend/validation.md) — FluentValidation patterns
- [Error Handling](backend/error-handling.md) — Result<T> pattern
- [Database](backend/database.md) — EF Core, migrations
- [Testing](backend/testing.md) — Unit, integration, E2E
- [API Contracts](backend/api-contracts.md) — Response shapes

### Frontend (React 19 + Redux Toolkit)
- [Overview](frontend/overview.md) — Frontend stack
- [Redux](frontend/redux.md) — RTK Query patterns
- [Components](frontend/components.md) — Component structure
- [Hooks](frontend/hooks.md) — Custom hooks
- [API Integration](frontend/api-integration.md) — baseApi, error handling
- [Type Safety](frontend/type-safety.md) — TypeScript patterns
- [Testing](frontend/testing.md) — Vitest, RTL, Playwright
- [Styling](frontend/styling.md) — CSS modules

### Workflows (How-To Guides)
- [Adding a Feature](workflows/adding-feature.md) — End-to-end guide
- [Database Migrations](workflows/database-migrations.md) — Create & apply
- [Testing Strategy](workflows/testing-strategy.md) — When to test what
- [Deployment](workflows/deployment.md) — Deploy to staging/prod
- [Troubleshooting](workflows/troubleshooting.md) — Common issues

### Standards (Code Quality)
- [Code Style](standards/code-style.md) — Naming, formatting
- [Git Workflow](standards/git-workflow.md) — Commits, PRs
- [Security](standards/security.md) — Security practices

### Reference (Look-Up)
- [File Structure](reference/file-structure.md) — Directory tree
- [Technologies](reference/technologies.md) — Tech stack versions
- [Common Mistakes](reference/common-mistakes.md) — Anti-patterns
- [Code Examples](reference/code-examples.md) — Reference implementations
- [Glossary](reference/glossary.md) — Terms & acronyms

### Tracking (Status)
- [Technical Debt](tracking/technical-debt.md) — Known issues
- [Improvements](tracking/improvements.md) — Future enhancements

## 🎯 By Task Type

**Adding new API endpoint**: 
→ [workflows/adding-feature.md](workflows/adding-feature.md) → [backend/controllers.md](backend/controllers.md)

**Creating entity**: 
→ [backend/entities.md](backend/entities.md) → [backend/database.md](backend/database.md)

**Redux state management**: 
→ [frontend/redux.md](frontend/redux.md) → [frontend/api-integration.md](frontend/api-integration.md)

**Writing tests**: 
→ [workflows/testing-strategy.md](workflows/testing-strategy.md) → [backend/testing.md](backend/testing.md) or [frontend/testing.md](frontend/testing.md)

**Database migration**: 
→ [workflows/database-migrations.md](workflows/database-migrations.md) → [backend/database.md](backend/database.md)

## 📝 Contributing to Documentation

See [standards/documentation.md](standards/documentation.md) for how to update these docs.
```

---

## Migration Plan (7 Phases)

### Phase 1: Foundation Structure ⚙️
**Estimated Time**: 1-2 hours  
**Dependencies**: None (independent)  
**Parallelizable**: No (must run first)

**Tasks**:
1. Create `.ai/` directory at project root
2. Create subdirectories:
   - `architecture/`
   - `backend/`
   - `frontend/`
   - `workflows/`
   - `standards/`
   - `reference/`
   - `tracking/`
3. Create `.ai/README.md` as navigation hub (use template above)
4. Create `.ai/quick-start.md` — 5-minute onboarding (extract key points from `.github/copilot-instructions.md`)

**Deliverables**:
- [ ] `.ai/` directory structure exists
- [ ] `.ai/README.md` has full navigation tree
- [ ] `.ai/quick-start.md` provides 5-minute overview
- [ ] All subdirectories created

**Verification**:
```bash
# Verify structure
ls .ai/
# Should show: README.md, quick-start.md, architecture/, backend/, frontend/, workflows/, standards/, reference/, tracking/
```

---

### Phase 2: Backend Modularization 📦
**Estimated Time**: 3-4 hours  
**Dependencies**: Phase 1 complete  
**Parallelizable**: Yes (can run parallel with Phase 3)

**Tasks**:
1. Split `BACKEND_CODING_GUIDE.md` (1200+ lines) into 10 focused files:
   - `.ai/backend/entities.md` — Extract entity rules (BaseEntity, null!, enums, navigation props, status fields)
   - `.ai/backend/dtos.md` — Extract DTO patterns (organization, naming, Read vs Write, shared DTOs)
   - `.ai/backend/repositories.md` — Extract repository rules (specialized vs generic, no SaveChanges, Include patterns)
   - `.ai/backend/services.md` — Extract service patterns (UoW injection, Result<T>, logging, fire-and-forget)
   - `.ai/backend/controllers.md` — Extract controller rules (thin layer, ApiResponse<T>, ProducesResponseType)
   - `.ai/backend/validation.md` — Extract 3-layer validation strategy
   - `.ai/backend/error-handling.md` — Extract Result<T> pattern, error codes, exception handling
   - `.ai/backend/database.md` — Extract migrations, seeding, EF Core patterns
   - `.ai/backend/testing.md` — Extract test patterns (AAA, helpers, integration tests)
   - `.ai/backend/api-contracts.md` — Extract response shapes, pagination, status codes

2. Create `.ai/backend/overview.md`:
   - Tech stack summary (ASP.NET Core 9, EF Core 10, PostgreSQL, Clean Architecture)
   - Links to all 10 modular files
   - Quick decision tree ("Need to create entity? → entities.md")

**Source Files**:
- `BACKEND_CODING_GUIDE.md` (lines 1-1200+)
- `.github/copilot-instructions.md` (backend sections)

**Deliverables**:
- [ ] 11 backend documentation files created (overview + 10 modules)
- [ ] Each file keeps one coherent topic without artificial splitting
- [ ] Zero content duplication (cross-references used)
- [ ] All files have frontmatter (last_updated, status, category, related)
- [ ] Code examples extracted from BACKEND_CODING_GUIDE.md

**Verification**:
```bash
# Count lines in each file
ls .ai/backend/*.md | ForEach-Object { "$($_): $((Get-Content $_).Count) lines" }
# Ensure files are focused by topic and easy to scan
```

---

### Phase 3: Frontend Modularization 🎨
**Estimated Time**: 3-4 hours  
**Dependencies**: Phase 1 complete  
**Parallelizable**: Yes (can run parallel with Phase 2)

**Tasks**:
1. Extract `src/frontend/storefront/FRONTEND_CODING_GUIDE.md` → split into 7 files:
   - `.ai/frontend/redux.md` — RTK Query patterns (baseApi, injected endpoints, cache invalidation)
   - `.ai/frontend/components.md` — Component structure (props typing, CSS modules, composition)
   - `.ai/frontend/hooks.md` — Custom hooks patterns (useForm, useApiErrorHandler, useDebounce)
   - `.ai/frontend/api-integration.md` — baseApi pattern (error handling, token refresh, telemetry)
   - `.ai/frontend/testing.md` — Vitest + RTL + Playwright patterns
   - `.ai/frontend/styling.md` — CSS modules, design tokens, responsive design
   - `.ai/frontend/type-safety.md` — TypeScript conventions (no any, proper generics)

2. Create `.ai/frontend/overview.md`:
   - Tech stack (React 19, Redux Toolkit, RTK Query, Vite, TypeScript)
   - Navigation to all 7 files
   - Decision tree ("Need API call? → redux.md + api-integration.md")

3. Create `.ai/frontend/routing.md`:
   - React Router patterns
   - Protected routes
   - Navigation patterns

**Source Files**:
- `src/frontend/storefront/FRONTEND_CODING_GUIDE.md`
- `.github/copilot-instructions.md` (frontend sections)

**Deliverables**:
- [ ] 9 frontend documentation files created (overview + 8 modules)
- [ ] Each file keeps one coherent topic without artificial splitting
- [ ] All files have frontmatter
- [ ] Storefront patterns documented as reference
- [ ] Admin differences noted where applicable

**Verification**:
```bash
ls .ai/frontend/*.md | ForEach-Object { "$($_): $((Get-Content $_).Count) lines" }
```

---

### Phase 4: Architecture Documentation 🏗️
**Estimated Time**: 2-3 hours  
**Dependencies**: Phase 2 & 3 complete (needs backend/frontend context)  
**Parallelizable**: Yes (can run parallel with Phase 5)

**Tasks**:
1. Create `.ai/architecture/overview.md`:
   - System architecture diagram (ASCII or link to diagram)
   - Tech stack summary
   - Deployment architecture
   - Layer interaction diagram

2. Create `.ai/architecture/clean-architecture.md`:
   - 4-layer Clean Architecture explanation
   - Layer dependency rules (Core → nothing, Application → Core, etc.)
   - Enforcement strategies
   - Violation examples

3. Create `.ai/architecture/patterns.md`:
   - Repository pattern (specialized vs generic)
   - Unit of Work pattern
   - Result<T> pattern (functional error handling)
   - DTO pattern (direction separation)
   - Validation pattern (3-layer)

4. Create ADR system:
   - `.ai/architecture/decisions/template.md` — ADR template (use template above)
   - `.ai/architecture/decisions/001-clean-architecture.md` — Why 4-layer Clean Architecture
   - `.ai/architecture/decisions/002-result-pattern.md` — Why Result<T> over exceptions for business logic
   - `.ai/architecture/decisions/003-jwt-authentication.md` — Auth strategy decision

**Deliverables**:
- [ ] 3 architecture overview files created
- [ ] ADR template created
- [ ] 3 initial ADRs documented
- [ ] Architecture dependency diagram included

**Verification**:
- [ ] Can trace why each major pattern was chosen (ADRs)
- [ ] Layer dependency rules explicitly documented
- [ ] All patterns link to implementation examples

---

### Phase 5: Workflows (How-To Guides) 📖
**Estimated Time**: 2-3 hours  
**Dependencies**: Phase 2 & 3 complete (needs backend/frontend docs to reference)  
**Parallelizable**: Yes (can run parallel with Phase 4)

**Tasks**:
1. Create `.ai/workflows/adding-feature.md`:
   - Step-by-step guide: Entity → DTO → Validator → Repository → Service → Controller → Frontend
   - Code examples at each step
   - Checklist for complete feature
   - Reference implementations

2. Create `.ai/workflows/database-migrations.md`:
   - How to create migration: `dotnet ef migrations add MigrationName`
   - How to apply: `dotnet ef database update`
   - Migration best practices (immutability, data migrations)
   - Troubleshooting common errors

3. Create `.ai/workflows/testing-strategy.md`:
   - When to unit test vs integration test vs E2E
   - Test data generation (TestDataFactory)
   - AAA pattern examples
   - Coverage targets

4. Create `.ai/workflows/deployment.md`:
   - Extract from `DEPLOYMENT.md`
   - Local Docker deployment
   - Staging deployment
   - Production deployment
   - Environment variables checklist

5. Create `.ai/workflows/troubleshooting.md`:
   - Common errors and solutions
   - Migration failures
   - Build errors
   - Test failures
   - Runtime errors

**Source Files**:
- `BACKEND_CODING_GUIDE.md` (Feature Delivery Order section)
- `DEPLOYMENT.md`
- `DOTNET_ERROR_CHECKING_GUIDE.md`

**Deliverables**:
- [ ] 5 workflow guides created
- [ ] Each workflow is actionable (step-by-step)
- [ ] Code examples included
- [ ] Checklists for verification

**Verification**:
- [ ] Can a new developer add a feature following adding-feature.md?
- [ ] Migration workflow covers all edge cases?
- [ ] Troubleshooting covers all common errors?

---

### Phase 6: Standards & Reference 📚
**Estimated Time**: 2 hours  
**Dependencies**: None (can run parallel with any phase)  
**Parallelizable**: Yes

**Tasks - Standards**:
1. Create `.ai/standards/code-style.md`:
   - Naming conventions (PascalCase, camelCase, _privateFields)
   - File-scoped namespaces
   - Region blocks
   - Private methods at bottom

2. Create `.ai/standards/git-workflow.md`:
   - Commit message format (Conventional Commits)
   - Branching strategy
   - PR checklist
   - Code review guidelines

3. Create `.ai/standards/security.md`:
   - CORS configuration
   - JWT validation
   - Rate limiting
   - Security headers
   - Secrets management

4. Create `.ai/standards/documentation.md`:
   - How to update .ai/ docs
   - Frontmatter requirements
   - When to create ADR
   - Documentation review process

**Tasks - Reference**:
5. Create `.ai/reference/file-structure.md`:
   - Annotated directory tree
   - Explanation of each major folder
   - Where to put new files

6. Create `.ai/reference/technologies.md`:
   - Backend stack with versions (ASP.NET Core 9, EF Core 10, PostgreSQL 16)
   - Frontend stack (React 19, Redux Toolkit, Vite 6)
   - DevOps (Docker, GitHub Actions, Render)

7. Create `.ai/reference/common-mistakes.md`:
   - Extract from `.github/copilot-instructions.md` "Common Mistakes to Avoid"
   - Add examples of each anti-pattern
   - Link to correct patterns

8. Create `.ai/reference/code-examples.md`:
   - Links to reference implementations:
     - ProductService, AuthService (services)
     - ProductRepository (specialized repo)
     - ProductsController (controller)
     - baseApi.ts (frontend API)
     - authSlice.ts (Redux slice)

9. Create `.ai/reference/glossary.md`:
   - UoW (Unit of Work)
   - DTO (Data Transfer Object)
   - RTK Query (Redux Toolkit Query)
   - baseApi (shared API instance)
   - ADR (Architecture Decision Record)
   - AAA (Arrange-Act-Assert)

**Source Files**:
- `CONTRIBUTING.md` (git workflow)
- `.github/copilot-instructions.md` (common mistakes)
- `BACKEND_CODING_GUIDE.md` (conventions)

**Deliverables**:
- [ ] 4 standards files created
- [ ] 5 reference files created
- [ ] Glossary defines all domain-specific terms
- [ ] Code examples link to actual implementation files

**Verification**:
- [ ] All anti-patterns have corresponding correct patterns
- [ ] All acronyms defined in glossary
- [ ] File structure matches actual project structure

---

### Phase 7: Tracking & Cleanup 🧹
**Estimated Time**: 1-2 hours  
**Dependencies**: All other phases complete  
**Parallelizable**: No (must run last)

**Tasks - Tracking**:
1. Consolidate technical debt:
   - Move `docs/API_DOCUMENTATION_DEBT.md` → `.ai/tracking/technical-debt.md`
   - Add architectural violations identified in audit
   - Add frontend duplication issues
   - Add DevOps gaps

2. Consolidate improvements:
   - Move `docs/archives/SENIOR_DEVELOPER_RECOMMENDATIONS.md` → `.ai/tracking/improvements.md`
   - Add enterprise patterns (caching, feature flags, distributed tracing)
   - Add standardization needs (admin → storefront patterns)

3. Archive historical docs:
   - Move `docs/archives/*` → `.ai/tracking/archived/`
   - Keep clear labels ("Historical - Reference only")

**Tasks - Cleanup**:
4. Update root `README.md`:
   - Add section: "## For AI Assistants"
   - Link to `.ai/README.md`
   - One-liner: "AI coding assistants should start with [`.ai/README.md`](.ai/README.md)"

5. Update `CONTRIBUTING.md`:
   - Reference `.ai/standards/` for coding standards
   - Link to `.ai/workflows/adding-feature.md`
   - Keep human-friendly overview

6. Update `.github/copilot-instructions.md`:
   - Reduce to ≤30 lines
   - Content: "Full documentation at `.ai/README.md`. Quick summary: [tech stack, architecture pattern, key rules]"
   - Link to .ai/ directory

7. Delete redundant files:
   - `plan.md` (root - outdated)
   - `src/frontend/storefront/COLOCATION_*.md` (4 files - task-specific, archived)
   - `src/frontend/storefront/COMPREHENSIVE_FRONTEND_IMPLEMENTATION_PLAN.md` (merged into .ai/workflows/)
   - `src/frontend/storefront/TEST_ERRORS_ANALYSIS.md` (historical - archived)

8. Optional: Keep originals as redirects (temporary):
   - Add deprecation notice to `BACKEND_CODING_GUIDE.md`: "⚠️ DEPRECATED: See `.ai/backend/` for current documentation"
   - Same for `FRONTEND_CODING_GUIDE.md`

**Deliverables**:
- [ ] `.ai/tracking/technical-debt.md` created (consolidated)
- [ ] `.ai/tracking/improvements.md` created (consolidated)
- [ ] Historical docs archived to `.ai/tracking/archived/`
- [ ] `README.md` updated with AI assistant section
- [ ] `CONTRIBUTING.md` references `.ai/` docs
- [ ] `.github/copilot-instructions.md` reduced to pointer
- [ ] Redundant files deleted
- [ ] Optional: Deprecation notices added to old files

**Verification**:
```bash
# Verify copilot-instructions.md is minimal
(Get-Content .github/copilot-instructions.md).Count
# Should be ≤30 lines

# Verify redundant files deleted
Test-Path plan.md  # Should be False
Test-Path src/frontend/storefront/COLOCATION_*.md  # Should be False

# Verify navigation
cat README.md | Select-String "\.ai/"  # Should find link
cat CONTRIBUTING.md | Select-String "\.ai/"  # Should find link
```

---

## Final Validation (AI Test)

After all phases complete, validate with these tests:

### Test 1: Navigation Speed
**Goal**: New AI assistant can navigate to any topic in <30 seconds

**Procedure**:
1. Start at `.ai/README.md`
2. Try to find: "How do I add a new entity?"
3. Expected path: README → Workflows section → adding-feature.md → backend/entities.md
4. Time should be <30 seconds of reading

**Pass Criteria**:
- [ ] Can find target in ≤30 seconds
- [ ] No broken links encountered
- [ ] Destination file answers the question

### Test 2: Task-Based Index
**Goal**: Task-based index leads to correct documentation

**Procedure**:
Test each task in README "By Task Type" section:
- "Adding new API endpoint" → workflows/adding-feature.md + backend/controllers.md
- "Creating entity" → backend/entities.md + backend/database.md
- "Redux state management" → frontend/redux.md + frontend/api-integration.md
- "Writing tests" → workflows/testing-strategy.md + backend/testing.md or frontend/testing.md
- "Database migration" → workflows/database-migrations.md + backend/database.md

**Pass Criteria**:
- [ ] All links resolve correctly
- [ ] Destination docs answer the task
- [ ] No circular references

### Test 3: No Conflicting Information
**Goal**: Zero contradictions between documentation files

**Procedure**:
1. Pick a rule (e.g., "Services must inject IUnitOfWork")
2. Search across all .ai/ files for mentions of this rule
3. Verify all mentions are consistent

**Pass Criteria**:
- [ ] Same rule stated identically everywhere
- [ ] No outdated information in any file
- [ ] Cross-references match

### Test 4: Anti-Pattern Coverage
**Goal**: All anti-patterns documented with correct alternative

**Procedure**:
1. Open `.ai/reference/common-mistakes.md`
2. For each anti-pattern, verify there's a link to correct pattern
3. Follow link, verify correct pattern is documented

**Pass Criteria**:
- [ ] All anti-patterns have "correct way" documented
- [ ] Code examples for both wrong and right way
- [ ] Links to detailed documentation

### Test 5: Reference Implementation Links
**Goal**: All reference implementations are linked and accessible

**Procedure**:
1. Open `.ai/reference/code-examples.md`
2. Click each file link
3. Verify file exists and matches description

**Pass Criteria**:
- [ ] All file paths resolve
- [ ] Files match description (ProductService is indeed a service)
- [ ] Files follow patterns described in docs

### Test 6: Completeness Check
**Goal**: No critical topic missing

**Procedure**:
Check coverage of all major domains:
- [ ] Entity creation workflow documented
- [ ] API endpoint creation workflow documented
- [ ] Database migration workflow documented
- [ ] Testing strategy documented
- [ ] Deployment workflow documented
- [ ] Error handling strategy documented
- [ ] Validation strategy documented
- [ ] Redux state management documented
- [ ] API integration documented

**Pass Criteria**:
- [ ] Every critical domain has documentation
- [ ] No "TODO" placeholders in active docs
- [ ] Every workflow has verification checklist

---

## Decisions & Rationale

### Directory Name: `.ai/` vs Alternatives

**Chosen**: `.ai/`

**Alternatives Considered**:
- `.agentic/` — Too obscure, not widely recognized
- `.copilot/` — Tool-specific, locks us into GitHub Copilot
- `ai-docs/` — Not hidden, clutters root directory
- `docs/ai/` — Buried under existing docs/ folder

**Rationale**:
- Hidden directory (starts with `.`) keeps root clean
- Tool-aware via adapter files (Copilot, Claude, Cursor, Cline)
- Clear intent (AI-focused documentation)
- Short and memorable

### File Structure Guideline: One Topic Per File

**Reasoning**:
- Different topics need different depth; hard limits cause artificial splitting
- A focused, coherent topic is easier to maintain than fragmented files
- Concise docs reduce duplication and improve navigation

**Rule of Thumb**:
- Keep docs concise, but complete
- Split only when a file covers multiple topics, not because of raw line count
- Long workflow files are acceptable if they remain coherent

**Enforcement**:
- Manual review during PR for clarity, coherence, and duplication
- Optional linter checks for broken links and missing required sections

### ADR System for Decisions

**Why ADRs**:
- Documents "why" decisions were made (not just "what")
- Prevents revisiting settled questions
- Shows evolution of architecture
- Onboards new developers faster
- Makes architectural drift visible

**What Requires an ADR**:
- Major pattern choices (Clean Architecture, Result<T>)
- Technology selections (PostgreSQL, Redis, React)
- Cross-cutting concerns (logging, error handling)
- Breaking changes to architecture

**What Doesn't Require an ADR**:
- Implementation details within established patterns
- Tactical code changes
- Bug fixes
- Documentation updates

### Keep Human Docs Separate

**Philosophy**:
- `.ai/` is AI-first (actionable, code-focused, scannable)
- `docs/` is human-first (narrative, context, design rationale)

**Overlap Strategy**:
- Some overlap is OK (deployment exists in both)
- `.ai/` extracts actionable steps from human docs
- Human docs provide broader context

**Examples**:
- `DEPLOYMENT.md` (root) — Human-friendly deployment overview
- `.ai/workflows/deployment.md` — Step-by-step deployment checklist
- `docs/MODERN_DESIGN_SYSTEM.md` — Human design system guide
- `.ai/frontend/styling.md` — Code-level styling rules

---

## Success Metrics

### Quantitative Metrics

- [ ] All AI instructions in one directory (`.ai/`)
- [ ] Every active AI tool has adapter file pointing to `.ai/README.md`
- [ ] Every file has `last_updated` date in frontmatter
- [ ] Zero content duplication (use cross-references)
- [ ] 100% of reference implementations linked
- [ ] 100% of anti-patterns documented with alternatives
- [ ] 100% of common tasks have workflow guides
- [ ] ≥3 ADRs for major architectural decisions
- [ ] Technical debt tracked in single file
- [ ] `.github/copilot-instructions.md` reduced to ≤30 lines

### Qualitative Metrics

- [ ] New AI assistant can navigate to any topic in <30 seconds
- [ ] Task-based index covers 100% of common developer tasks
- [ ] No conflicting information between files
- [ ] Every workflow has verification checklist
- [ ] Glossary defines 100% of domain-specific terms
- [ ] Documentation structure is intuitive (no guessing where to look)
- [ ] Maintenance burden reduced (update one file per change)

### User Experience Metrics

**For AI Assistants**:
- [ ] Can find correct pattern in ≤2 hops from README
- [ ] Code examples compile and run
- [ ] Anti-patterns clearly marked
- [ ] All workflows are executable (step-by-step)

**For Human Developers**:
- [ ] Onboarding time reduced (can find coding standards quickly)
- [ ] PR review faster (point to specific .ai/ doc for violations)
- [ ] Architecture decisions transparent (ADRs explain "why")
- [ ] Reduced "where do I put this?" questions

---

## Risks & Mitigation

### Risk 1: Documentation Drift
**Risk**: `.ai/` docs become outdated as code evolves

**Mitigation**:
- Add PR checklist item: "Update .ai/ docs if patterns change"
- Pre-commit hook to check `last_updated` dates (warn if >6 months old)
- Quarterly documentation review

### Risk 2: Over-Documentation
**Risk**: Too much documentation becomes noise

**Mitigation**:
- Strict 300-line limit enforces focus
- Each file must answer "why does developer need this?"
- Delete files that haven't been accessed in 12 months

### Risk 3: Duplication Creep
**Risk**: Content gets duplicated across files over time

**Mitigation**:
- DRY principle enforced in PR review
- Cross-references preferred over copy-paste
- Quarterly deduplication audit

### Risk 4: Navigation Complexity
**Risk**: Too many files makes navigation hard

**Mitigation**:
- `README.md` task-based index (find by "what I need to do")
- Consistent folder structure (never nest >3 levels)
- `related` frontmatter links to adjacent topics

### Risk 5: Adoption Resistance
**Risk**: Team prefers old scattered docs

**Mitigation**:
- Deprecation notices on old files (point to new location)
- Keep old files as redirects for 1-2 months
- Demo the new structure in team meeting
- Highlight benefits (faster navigation, no duplication)

---

## Timeline & Effort Estimates

### Sequential Timeline (Single Developer)
- **Phase 1**: 1-2 hours
- **Phase 2**: 3-4 hours (depends on Phase 1)
- **Phase 3**: 3-4 hours (depends on Phase 1)
- **Phase 4**: 2-3 hours (depends on Phase 2 & 3)
- **Phase 5**: 2-3 hours (depends on Phase 2 & 3)
- **Phase 6**: 2 hours (independent)
- **Phase 7**: 1-2 hours (depends on all)

**Total Sequential**: 14-20 hours

### Parallel Timeline (Multiple Developers or Smart Sequencing)
- **Day 1**: Phase 1 (2 hours)
- **Day 2**: Phase 2 + Phase 3 in parallel (4 hours)
- **Day 3**: Phase 4 + Phase 5 in parallel (3 hours) + Phase 6 (2 hours)
- **Day 4**: Phase 7 (2 hours) + validation (2 hours)

**Total Parallel**: 13 hours (spread over 4 days)

### Best Approach
**Recommended**: 2 developers, 2-day sprint
- **Developer 1**: Phase 1 → Phase 2 → Phase 4 → Phase 7
- **Developer 2**: Phase 1 → Phase 3 → Phase 5 → Phase 6 → Phase 7

---

## Post-Implementation Maintenance

### Quarterly Review (Every 3 Months)
- [ ] Check `last_updated` dates (update stale docs)
- [ ] Review ADRs (any superseded decisions?)
- [ ] Update technology versions in `reference/technologies.md`
- [ ] Check for broken links
- [ ] Review tracking/technical-debt.md (items resolved?)

### Per-Feature Maintenance
When adding new feature:
- [ ] Update relevant `.ai/` file (e.g., new entity type → update `backend/entities.md`)
- [ ] Add to `reference/code-examples.md` if exemplary implementation
- [ ] Update `last_updated` frontmatter
- [ ] Create ADR if architectural decision made

### Documentation Review in PR
PR checklist should include:
- [ ] If new pattern introduced, documented in `.ai/`
- [ ] If existing pattern changed, `.ai/` updated
- [ ] If architectural decision made, ADR created
- [ ] All links in updated docs resolve correctly
- [ ] `last_updated` date current

---

## Appendix A: File Manifest

Complete list of files to be created:

### Root .ai/ Directory (2 files)
- `.ai/README.md`
- `.ai/quick-start.md`

### architecture/ (7 files)
- `overview.md`
- `clean-architecture.md`
- `patterns.md`
- `decisions/template.md`
- `decisions/001-clean-architecture.md`
- `decisions/002-result-pattern.md`
- `decisions/003-jwt-authentication.md`

### backend/ (11 files)
- `overview.md`
- `entities.md`
- `dtos.md`
- `repositories.md`
- `services.md`
- `controllers.md`
- `validation.md`
- `error-handling.md`
- `database.md`
- `testing.md`
- `api-contracts.md`

### frontend/ (9 files)
- `overview.md`
- `redux.md`
- `components.md`
- `hooks.md`
- `routing.md`
- `api-integration.md`
- `type-safety.md`
- `testing.md`
- `styling.md`

### workflows/ (5 files)
- `adding-feature.md`
- `database-migrations.md`
- `testing-strategy.md`
- `deployment.md`
- `troubleshooting.md`

### standards/ (4 files)
- `code-style.md`
- `git-workflow.md`
- `documentation.md`
- `security.md`

### reference/ (5 files)
- `file-structure.md`
- `technologies.md`
- `common-mistakes.md`
- `code-examples.md`
- `glossary.md`

### tracking/ (2 files + archive dir)
- `technical-debt.md`
- `improvements.md`
- `archived/` (directory for historical docs)

**Total**: 45 new files

---

## Appendix B: Migration Checklist

Print this checklist and check off as you complete each item:

### Pre-Work
- [ ] Read this entire plan
- [ ] Assign developers (if multiple)
- [ ] Schedule 2-4 day sprint
- [ ] Communicate to team

### Phase 1: Foundation
- [ ] Create `.ai/` directory
- [ ] Create all subdirectories
- [ ] Create `README.md` with full navigation
- [ ] Create `quick-start.md`
- [ ] Verify structure

### Phase 2: Backend
- [ ] Create `backend/overview.md`
- [ ] Split BACKEND_CODING_GUIDE.md into 10 files
- [ ] Add frontmatter to all files
- [ ] Verify each file covers one coherent topic
- [ ] Test all internal links

### Phase 3: Frontend
- [ ] Create `frontend/overview.md`
- [ ] Extract FRONTEND_CODING_GUIDE.md into 8 files
- [ ] Add frontmatter to all files
- [ ] Verify each file covers one coherent topic
- [ ] Test all internal links

### Phase 4: Architecture
- [ ] Create `architecture/overview.md`
- [ ] Create `architecture/clean-architecture.md`
- [ ] Create `architecture/patterns.md`
- [ ] Create ADR template
- [ ] Create 3 initial ADRs
- [ ] Verify ADRs follow template

### Phase 5: Workflows
- [ ] Create `workflows/adding-feature.md`
- [ ] Create `workflows/database-migrations.md`
- [ ] Create `workflows/testing-strategy.md`
- [ ] Create `workflows/deployment.md`
- [ ] Create `workflows/troubleshooting.md`
- [ ] Verify all workflows have checklists

### Phase 6: Standards & Reference
- [ ] Create all 4 standards files
- [ ] Create all 5 reference files
- [ ] Populate glossary
- [ ] Link all code examples
- [ ] Verify file structure matches actual project

### Phase 7: Cleanup
- [ ] Create `tracking/technical-debt.md`
- [ ] Create `tracking/improvements.md`
- [ ] Archive historical docs
- [ ] Update root `README.md`
- [ ] Update `CONTRIBUTING.md`
- [ ] Reduce `.github/copilot-instructions.md` to pointer
- [ ] Delete redundant files
- [ ] Add deprecation notices (optional)

### Validation
- [ ] Run all 6 validation tests
- [ ] Fix any broken links
- [ ] Verify no duplication
- [ ] Test with AI assistant
- [ ] Get team review

### Launch
- [ ] Announce to team
- [ ] Update onboarding docs
- [ ] Schedule first quarterly review
- [ ] Celebrate! 🎉

---

## Appendix C: Further Considerations

### 1. Documentation Generation Automation
**Question**: Should we auto-generate `reference/file-structure.md` from actual directory tree?

**Pros**:
- Always accurate (no manual updates)
- Reduces maintenance burden
- Can run in pre-commit hook

**Cons**:
- Loses manual annotations explaining each folder
- Requires script maintenance

**Recommendation**: Hybrid approach
- Auto-generate base tree structure
- Manually add annotations/explanations
- Script warns if structure changed since last update

### 2. Enforcement Strategy
**Question**: How to enforce documentation standards?

**Options**:
1. **Manual PR Review** — Reviewer checks frontmatter, line count
2. **Pre-commit Hook** — Validates frontmatter, checks broken links and required sections
3. **CI/CD Check** — Build fails if documentation invalid
4. **Quarterly Audit** — Manual review every 3 months

**Recommendation**: Combine 1 + 2
- Pre-commit hook for automated checks
- Manual PR review for content quality
- Quarterly audit for staleness

### 3. Migration of Existing Content
**Question**: Keep old files with deprecation notices or hard delete?

**Options**:
1. **Hard delete immediately** — Clean break, forces adoption
2. **Deprecation notices** — Keep old files with "See .ai/ for current docs"
3. **Redirect files** — Old files contain only link to new location
4. **Gradual sunset** — Keep for 1-2 months, then delete

**Recommendation**: Option 3 (Redirect files) for 1 month, then delete
- `BACKEND_CODING_GUIDE.md` → "⚠️ MOVED: See `.ai/backend/` for current documentation. This file will be deleted March 31, 2026."
- Gives team time to adjust
- Clear cutoff date forces migration

### 4. Code Coverage Targets
**Question**: Mentioned 70% backend / 60% frontend in main plan. Should documentation include coverage enforcement?

**Recommendation**: Add to `.ai/workflows/testing-strategy.md`
- Document target coverage percentages
- Explain why different targets for backend/frontend
- Link to CI/CD configuration

### 5. Performance Test Infrastructure
**Question**: Load tests need dedicated environment. Where to document this?

**Recommendation**: Add to `.ai/backend/testing.md`
- Section on performance testing
- Document test environment setup (docker-compose with 10k seeded products)
- Link to load test examples

### 6. Visual Regression Testing
**Question**: Mentioned as missing in audit. Should we document it even if not implemented?

**Recommendation**: Add to `.ai/tracking/improvements.md`
- List as planned improvement
- Link to relevant tools (Playwright visual comparisons)
- Reference in `.ai/frontend/testing.md` as "Future Enhancement"

---

## Conclusion

This plan transforms your documentation from **scattered notes** to **enterprise-level AI knowledge base**. After implementation:

- ✅ **Single entry point** — `.ai/README.md` is the navigation hub
- ✅ **Modular knowledge** — Each file covers one coherent topic with practical depth
- ✅ **Zero duplication** — DRY principle with cross-references
- ✅ **Task-oriented** — Find what you need by what you're doing
- ✅ **Maintainable** — Update one file per topic change
- ✅ **Discoverable** — Clear hierarchy, no guessing
- ✅ **Versioned** — Last-updated dates track freshness
- ✅ **Scalable** — Easy to add new domains
- ✅ **Tool-aware** — Works through native adapter files for each AI assistant
- ✅ **Production-ready** — Enterprise-level structure

Ready to start? Begin with **Phase 1** to create the foundation structure.

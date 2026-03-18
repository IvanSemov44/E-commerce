# Architecture Overview

## Clean Architecture — Backend

```mermaid
graph TD
    subgraph API["API Layer (ECommerce.API)"]
        C[Controllers]
        MW[Middleware]
        AF[ActionFilters]
    end

    subgraph APP["Application Layer (ECommerce.Application)"]
        SVC[Services]
        DTO[DTOs]
        VAL[Validators]
        MAP[MappingProfile]
    end

    subgraph CORE["Core Layer (ECommerce.Core)"]
        ENT[Entities]
        INT[Interfaces]
        RES[Result&lt;T&gt;]
        ERR[ErrorCodes]
        ENUM[Enums]
    end

    subgraph INFRA["Infrastructure Layer (ECommerce.Infrastructure)"]
        DB[AppDbContext]
        REPO[Repositories]
        UOW[UnitOfWork]
        SEED[Seeders]
        RES2[Resilience Policies]
    end

    subgraph EXT["External"]
        PG[(PostgreSQL)]
        SG[SendGrid]
        STR[Stripe placeholder]
    end

    C --> SVC
    MW --> SVC
    AF --> DTO
    SVC --> INT
    SVC --> RES
    SVC --> ERR
    REPO --> INT
    UOW --> REPO
    DB --> PG
    INFRA --> CORE
    APP --> CORE
    API --> APP

    style CORE fill:#1e3a5f,color:#fff
    style APP fill:#1a4731,color:#fff
    style INFRA fill:#4a2c0a,color:#fff
    style API fill:#3b1f5e,color:#fff
```

**Dependency rule:** arrows point inward only — API → Application → Core, Infrastructure → Core.

---

## Frontend Architecture

```mermaid
graph TD
    subgraph UI["UI Layer"]
        PG2[Pages / Routes]
        COMP[Feature Components]
        SHARED[Shared Components]
    end

    subgraph STATE["State Layer"]
        RTK[RTK Query — server state]
        SLICE[Redux Slices — UI state]
    end

    subgraph PERSIST["Persistence"]
        LS[localStorage — cart draft]
        MW2[Redux Middleware]
    end

    subgraph BACKEND["Backend API"]
        API2[ASP.NET Core REST API]
    end

    PG2 --> COMP
    COMP --> RTK
    COMP --> SLICE
    RTK -->|HTTP + JWT| API2
    SLICE --> MW2
    MW2 --> LS

    style UI fill:#1e3a5f,color:#fff
    style STATE fill:#1a4731,color:#fff
    style PERSIST fill:#4a2c0a,color:#fff
    style BACKEND fill:#3b1f5e,color:#fff
```

**Rule:** Components never call `fetch` directly — all server state goes through RTK Query. Slices hold UI-only state (auth flags, toast, cart local copy).

---

## System Context

```mermaid
graph LR
    USER([Customer]) -->|Browser| FRONT[React Storefront]
    ADMIN([Admin]) -->|Browser| ADMPANEL[React Admin Panel]
    FRONT -->|REST / JWT| BACK[.NET 10 API]
    ADMPANEL -->|REST / JWT| BACK
    BACK --> PG2[(PostgreSQL)]
    BACK -->|Email| SG2[SendGrid / SMTP]
    BACK -->|Payments — mocked| STR2[Stripe API]
    BACK -->|Logs| SERI[Serilog]
```

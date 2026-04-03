# Observability & Information Collection — Full Reference

> **Purpose:** This document defines every category of signal collected in this system,
> what question each one answers, concrete e-commerce examples, and the current
> implementation status in this codebase.
>
> **This is a reference document — not an implementation guide.**

---

## Mental Model: Three Layers

```
┌──────────────────────────────────────────────────────────┐
│  RAW SIGNALS                                             │
│  Logs · Metrics · Traces · Events                        │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│  PRODUCT & USER SIGNALS                                  │
│  Behavior · Feedback · KPIs                              │
└────────────────────────┬─────────────────────────────────┘
                         │
┌────────────────────────▼─────────────────────────────────┐
│  INTELLIGENCE LAYER                                      │
│  AI Insights · Trends · Recommendations                  │
└──────────────────────────────────────────────────────────┘
```

---

## Signal Categories

### 1. Logs

**What question it answers:** _What happened, and when?_

Logs are time-stamped, structured records of discrete events. Every log entry captures
a moment in the system's life — a request arrived, a value was computed, an error
occurred.

#### Log Levels

| Level | When to use | Example |
|-------|-------------|---------|
| `Debug` | Granular internal flow (dev only) | `Evaluating discount rule for cart {CartId}` |
| `Information` | Normal business events | `Order {OrderId} created for user {UserId}` |
| `Warning` | Expected but notable problems | `Promo code {Code} expired, skipping` |
| `Error` | Unexpected failures | `Payment gateway timeout after {Ms}ms` |
| `Critical` | System integrity at risk | `Database connection pool exhausted` |

#### Log Types

| Type | Description | E-commerce Example |
|------|-------------|-------------------|
| **Application logs** | General application flow | Request received, response sent |
| **Audit logs** | Security-sensitive actions | Admin changed product price |
| **Security logs** | Auth events, rate limits | Failed login attempt from IP 1.2.3.4 |
| **Error logs** | Exceptions and failures | NullReferenceException in OrderService |
| **Performance logs** | Slow operation warnings | Request took 1,240 ms — threshold 500 ms |
| **Database logs** | Query execution (dev) | SQL query executed in 23 ms |
| **Domain event logs** | Business-level state changes | ProductPriceChanged event dispatched |

#### What Gets Logged Automatically (Current Codebase)

| Event | Level | Mechanism |
|-------|-------|-----------|
| Every HTTP request (method, path, status, duration) | Information | Serilog request middleware |
| Unhandled exceptions | Error | `GlobalExceptionMiddleware` |
| Correlation ID on every request | — | `CorrelationIdMiddleware` |
| Validation failures | Warning | `ValidationFilterAttribute` |
| MediatR request + response | Information | `LoggingBehavior` |
| Slow MediatR requests (> 500 ms) | Warning | `PerformanceBehavior` |
| DB query errors | Error | EF Core + Serilog |
| Domain events dispatched | Information | `DomainEventDispatcher` |

#### Log Sinks (Current Codebase)

| Sink | Environment | Config Key |
|------|-------------|------------|
| Console | All | Always on |
| File `logs/app-*.txt` | All | Always on |
| File `logs/security-*.txt` | All | Warning+ only |
| Seq | Dev / Staging | `Seq:ServerUrl` |
| Application Insights | Production | `ApplicationInsights:InstrumentationKey` |

#### Sensitive Data Rules

Never log:
- Passwords or password hashes
- Raw JWT or refresh tokens
- Full credit card or payment data
- Government IDs or national insurance numbers

---

### 2. Metrics

**What question it answers:** _How much? How fast? How often?_

Metrics are numeric measurements sampled over time. They are aggregated (counters,
gauges, histograms) and support alerting and dashboards.

#### Technical Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `http_requests_total` | Counter | Total HTTP requests by method, path, status |
| `http_request_duration_ms` | Histogram | Request latency distribution (p50, p95, p99) |
| `http_active_requests` | Gauge | In-flight requests at any moment |
| `db_query_duration_ms` | Histogram | Database query execution time |
| `db_connection_pool_used` | Gauge | Active DB connections |
| `cache_hits_total` | Counter | Redis cache hits |
| `cache_misses_total` | Counter | Redis cache misses |
| `cache_hit_ratio` | Gauge | `hits / (hits + misses)` |
| `queue_depth` | Gauge | Background job queue length |
| `gc_collections_total` | Counter | .NET GC collections by generation |
| `memory_allocated_mb` | Gauge | Managed heap size |
| `thread_pool_queue_length` | Gauge | Threadpool saturation signal |

#### Business Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `orders_placed_total` | Counter | Orders placed (by payment method, status) |
| `orders_value_total` | Counter | Revenue by currency |
| `order_value_usd` | Histogram | Distribution of order values |
| `cart_items_added_total` | Counter | Cart add actions |
| `cart_abandonment_rate` | Gauge | Carts started but not completed |
| `checkout_started_total` | Counter | Checkout funnel entries |
| `checkout_completed_total` | Counter | Checkout funnel completions |
| `payment_success_total` | Counter | Successful payments by method |
| `payment_failure_total` | Counter | Failed payments by failure reason |
| `payment_success_rate` | Gauge | `success / (success + failure)` |
| `promo_code_applied_total` | Counter | Promo code usage |
| `product_views_total` | Counter | Product page views |
| `search_queries_total` | Counter | Search queries |
| `search_zero_results_total` | Counter | Searches that returned nothing |
| `wishlist_adds_total` | Counter | Wishlist add actions |
| `review_submissions_total` | Counter | Product review submissions |
| `user_registrations_total` | Counter | New account creations |
| `user_logins_total` | Counter | Login events |
| `user_login_failures_total` | Counter | Failed login attempts |

#### Infrastructure Metrics

| Metric | Type | Description |
|--------|------|-------------|
| `cpu_usage_percent` | Gauge | CPU load |
| `memory_usage_percent` | Gauge | RAM used vs available |
| `disk_io_bytes` | Counter | Disk read/write volume |
| `network_io_bytes` | Counter | Network in/out |
| `db_connections_active` | Gauge | Open DB connections |
| `redis_memory_used_mb` | Gauge | Redis memory pressure |
| `health_check_duration_ms` | Histogram | Health check response time |

---

### 3. Traces

**What question it answers:** _Where did it happen, and how long did each step take?_

A trace follows a single request across all services and operations it touches,
showing the full call graph with timing for each span.

#### Trace Anatomy

```
Trace: POST /api/orders  (total: 243ms)
├── Middleware pipeline          (2ms)
├── MediatR → CreateOrderCommand (239ms)
│   ├── Validation               (1ms)
│   ├── Stock check (DB query)   (12ms)
│   ├── Create order (DB write)  (8ms)
│   ├── Payment call (HTTP)      (210ms)
│   │   └── Stripe API           (205ms)
│   └── Domain event dispatch    (8ms)
└── Response serialisation       (2ms)
```

#### Trace Data Points

| Data Point | Description |
|------------|-------------|
| `trace_id` | Unique ID for the entire request |
| `span_id` | Unique ID for one operation within the trace |
| `parent_span_id` | Links child spans to parent |
| `operation_name` | What the span represents |
| `start_time` / `end_time` | Wall-clock boundaries |
| `duration_ms` | Elapsed time for the span |
| `status` | OK / ERROR |
| `attributes` | Metadata (HTTP method, DB table, user ID, etc.) |

#### Trace Sources in This Codebase

| Source | Mechanism | Status |
|--------|-----------|--------|
| HTTP request boundaries | W3C `Activity.Current.TraceId` via `CorrelationIdMiddleware` | Partial — passive only |
| MediatR request timing | `LoggingBehavior` (Stopwatch) | Logged, not traced |
| HTTP client calls | Not instrumented | Missing |
| EF Core queries | Not instrumented | Missing |
| Domain event dispatch | Not instrumented | Missing |

---

### 4. Business Events

**What question it answers:** _What did the user/system do at the domain level?_

Business events are high-level, domain-meaningful actions — distinct from low-level
technical logs. They represent state transitions in your business model.

#### Event Catalogue

| Event Type | Category | Trigger |
|------------|----------|---------|
| `user.registered` | user | New account created |
| `user.logged_in` | user | Successful authentication |
| `user.password_reset` | user | Password reset completed |
| `product.viewed` | product | Product detail page opened |
| `product.searched` | product | Search query submitted |
| `product.added_to_wishlist` | product | Wishlist add action |
| `cart.item_added` | cart | Item added to cart |
| `cart.item_removed` | cart | Item removed from cart |
| `cart.cleared` | cart | Entire cart cleared |
| `checkout.started` | checkout | Checkout flow entered |
| `checkout.address_entered` | checkout | Shipping address saved |
| `checkout.payment_method_selected` | checkout | Payment method chosen |
| `order.placed` | order | Order confirmed |
| `order.cancelled` | order | Order cancelled |
| `order.shipped` | order | Order shipped |
| `order.delivered` | order | Order delivered |
| `payment.succeeded` | payment | Payment processed successfully |
| `payment.failed` | payment | Payment declined or errored |
| `payment.refunded` | payment | Refund issued |
| `promo.applied` | promo | Promo code applied at checkout |
| `promo.rejected` | promo | Invalid/expired promo code used |
| `review.submitted` | review | Product review written |

#### Event Payload Schema

```json
{
  "eventType": "order.placed",
  "category": "order",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "sessionId": "sess_abc123",
  "correlationId": "req-xyz-789",
  "occurredAt": "2026-04-03T14:22:11Z",
  "payload": {
    "orderId": "ord-001",
    "totalAmount": 199.99,
    "currency": "USD",
    "itemCount": 3,
    "paymentMethod": "stripe"
  }
}
```

#### Current Status
Domain events exist (ProductCreated, ProductPriceChanged, ProductDeactivated via
MediatR) but business-level user/order/cart events are **not yet persisted or
queryable**.

---

### 5. User Behavior Analytics

**What question it answers:** _How do users interact with the product?_

| Signal | Description | E-commerce Example |
|--------|-------------|-------------------|
| Page views | Which pages are visited | `/products/sneakers` viewed 4,200×/day |
| Click tracking | What elements users click | "Add to Cart" vs "Save to Wishlist" |
| Session duration | Time spent per visit | Avg 4:32 minutes |
| Navigation paths | How users move through the app | Home → Category → Product → Cart |
| Funnel analysis | Drop-off at each step | 60% leave at checkout step 2 |
| Search behavior | What users search for | Top term: "running shoes" |
| Feature usage | Which features are used | Wishlist used by 23% of users |
| Scroll depth | How far users read | 80% read product description fully |
| Error encounters | UX errors in the frontend | Form submit failed 2% of sessions |
| Device / browser | Technical user context | 65% mobile, 35% desktop |

#### Collection Method
Frontend telemetry service (`telemetry.ts`) → pluggable sinks → backend analytics
endpoint → `BusinessEvents` table.

---

### 6. KPIs (Key Performance Indicators)

**What question it answers:** _Is the product working as a business?_

| KPI | Formula | Target |
|-----|---------|--------|
| **Conversion rate** | Orders / Sessions × 100 | > 3% |
| **Daily Active Users (DAU)** | Unique users with session in 24h | — |
| **Average Order Value (AOV)** | Revenue / Orders | — |
| **Cart abandonment rate** | 1 − (Orders / Cart starts) | < 70% |
| **Customer retention rate** | Returning buyers / Total buyers | > 40% |
| **Checkout completion rate** | Completed checkouts / Started | > 60% |
| **Payment success rate** | Succeeded / Attempted | > 97% |
| **Revenue per session** | Revenue / Sessions | — |
| **Return rate** | Refunded orders / Total orders | < 5% |
| **Time to first purchase** | Registration → first order | — |

---

### 7. Errors (Special Category)

**What question it answers:** _What is broken right now?_

Errors are treated as a distinct signal type because they drive **immediate alerting**
and require dedicated tracking beyond standard logs.

#### Error Categories

| Category | Description | Examples |
|----------|-------------|---------|
| **HTTP 4xx** | Client errors | 400 Bad Request, 404 Not Found, 429 Rate Limited |
| **HTTP 5xx** | Server errors | 500 Internal Error, 503 Service Unavailable |
| **Unhandled exceptions** | Unexpected crashes | NullReferenceException, DbException |
| **Business rule violations** | Domain failures | Insufficient stock, expired promo code |
| **External API failures** | Third-party errors | Payment gateway timeout, email provider 500 |
| **Frontend JS errors** | React render failures | ErrorBoundary caught, component crash |
| **Validation errors** | Input shape errors | Required field missing, invalid format |
| **Concurrency conflicts** | Optimistic lock violations | Two users modified same record |

#### Error Response Shape (Current Codebase)

```json
{
  "success": false,
  "error": {
    "message": "The promo code has expired.",
    "code": "PROMO_EXPIRED"
  },
  "traceId": "req-abc-123"
}
```

`traceId` links the client error to the server log line for support correlation.

#### Alerts to Configure

| Condition | Severity |
|-----------|----------|
| 5xx rate > 5% of requests for 5 min | High |
| Any `Critical` log entry | Critical |
| P99 latency > 2,000 ms for 5 min | Medium |
| Health check non-200 | Critical |
| Payment failure rate > 10% | High |
| > 50 failed logins/min from same IP | High |

---

### 8. System Health Signals

**What question it answers:** _Is the infrastructure healthy?_

| Signal | Mechanism | Current Status |
|--------|-----------|----------------|
| Database connectivity | `GET /health/ready` — PostgreSQL check | Active |
| Redis connectivity | `GET /health/ready` — Redis check | Active |
| Memory pressure | `MemoryHealthCheck` (threshold: 1,024 MB) | Active |
| API liveness | `GET /health` | Active |
| Readiness probe | `GET /health/ready` | Active |
| Full detail | `GET /health/detail` | Active |
| CPU usage | Not implemented | Missing |
| Disk I/O | Not implemented | Missing |
| Background queue depth | Not implemented | Missing |

#### Health Check Response Format

```json
{
  "status": "Healthy",
  "totalDurationMs": 45.2,
  "timestamp": "2026-04-03T12:00:00Z",
  "checks": [
    {
      "name": "postgresql",
      "status": "Healthy",
      "durationMs": 12.5,
      "tags": ["db", "ready"]
    },
    {
      "name": "memory",
      "status": "Healthy",
      "durationMs": 0.3,
      "data": { "allocatedMB": 145.2 }
    }
  ]
}
```

---

### 9. AI-Specific Signals

**What question it answers:** _Is the AI behaving correctly and cost-efficiently?_

> Relevant when AI/LLM features are added (e.g., product recommendations,
> search ranking, review summarisation, chatbot support).

#### AI Performance Metrics

| Metric | Description |
|--------|-------------|
| `ai_request_duration_ms` | End-to-end latency per AI call |
| `ai_tokens_input_total` | Input tokens consumed |
| `ai_tokens_output_total` | Output tokens generated |
| `ai_cost_usd_total` | Estimated cost (tokens × rate) |
| `ai_request_success_total` | Successful AI completions |
| `ai_request_failure_total` | Failed / errored AI calls |
| `ai_retry_total` | Retried AI requests |

#### AI Quality Signals

| Signal | Description |
|--------|-------------|
| User satisfaction score | Thumbs up/down on AI-generated content |
| Retry rate | Proportion of answers the user regenerated |
| Ignored answer rate | AI response shown but user ignored it |
| Hallucination markers | Keywords detected that signal fabrication |
| Response length distribution | Too short / too long answers |

#### AI Behavior Signals

| Signal | Description |
|--------|-------------|
| Which prompts fail most | Prompt templates with highest error rates |
| Which features use AI most | Recommendation vs search vs chatbot |
| Peak usage windows | Time-of-day AI load patterns |
| Model version breakdown | A/B test across model versions |

---

### 10. Content / Domain Signals

**What question it answers:** _What content drives engagement and what is ignored?_

| Signal | Description | E-commerce Application |
|--------|-------------|----------------------|
| Product view-to-cart rate | Views that convert to cart adds | Identify high/low converting products |
| Top search terms | Most searched queries | Drive inventory and SEO decisions |
| Zero-result searches | Searches with no matches | Reveal catalogue gaps |
| Category engagement | Which categories get most traffic | Navigation and homepage optimisation |
| Review engagement | Reviews marked helpful | Surface quality review content |
| Out-of-stock demand | Views on out-of-stock products | Restock prioritisation |
| Price sensitivity signals | View → no cart add (possible price issue) | Pricing strategy |
| Image quality signal | Products with images vs without, cart rate | Image investment ROI |

---

### 11. Feedback Data

**What question it answers:** _What do users think, in their own words?_

| Signal Type | Mechanism | Examples |
|-------------|-----------|---------|
| Product reviews | Star rating + text | "Fits true to size, fast delivery" |
| Order experience rating | Post-delivery survey | 1–5 stars + comment |
| Support tickets | Helpdesk messages | "My order hasn't arrived" |
| NPS score | Net Promoter Score survey | "How likely are you to recommend us?" |
| Return reasons | Selected on return form | "Wrong size", "Item damaged" |
| Feature requests | In-app feedback widget | "I want to save my size preferences" |

---

### 12. Derived Insights (AI-Generated)

**What question it answers:** _What should we do next?_

This is the **output** of analysing all the layers above. Derived insights are
generated by aggregating raw signals and applying analytical or AI reasoning.

| Insight Type | Input Signals | Example Output |
|--------------|---------------|----------------|
| Demand forecast | Order history + seasonality | "Stock running shoes — 3× usual demand expected next week" |
| Churn prediction | Session frequency + purchase gap | "User X hasn't visited in 30 days — send re-engagement email" |
| Price optimisation | View rate, cart rate, competitor prices | "Product Y's conversion dropped 15% after price increase" |
| Personalised recommendations | Purchase history + view history | "Users who bought X also buy Y" |
| Anomaly detection | Metrics + error rates | "Payment failures spiked 3× at 14:00 — gateway issue?" |
| Search intent mapping | Zero-result queries + browsing | "Users searching 'eco packaging' are not finding it — add filter" |
| Inventory alerts | Stock levels + sales velocity | "SKU-1234 will sell out in 4 days at current rate" |
| Customer segment trends | KPIs by cohort | "Mobile users have 40% lower AOV than desktop" |

---

## Collection Strategy Summary

| Signal Layer | Primary Collection Method | Storage |
|---|---|---|
| Logs | Serilog structured logging | Console, Files, Seq, App Insights |
| Metrics | OpenTelemetry → Prometheus / OTLP | Prometheus, Grafana |
| Traces | OpenTelemetry ActivitySource | Seq (OTLP), Jaeger, Tempo |
| Business Events | `IBusinessEventService` → DB + Serilog | PostgreSQL `business_events` table |
| User Behavior | Frontend telemetry → `POST /api/analytics/events` | PostgreSQL `business_events` table |
| KPIs | Aggregated queries on `business_events` | Queried on demand |
| Errors | `GlobalExceptionMiddleware` + Serilog | Files, Seq, Sentry (future) |
| Health Signals | ASP.NET Core Health Checks | `/health/*` endpoints |
| AI Signals | Per-call instrumentation in AI service | Metrics + business_events |
| Content Signals | Aggregated queries on product/order data | Queried on demand |
| Feedback | Product reviews entity in DB | PostgreSQL `reviews` table |
| Derived Insights | Background analysis jobs / AI queries | Cache + admin dashboard |

---

## Phased Roadmap

### Phase 1 — Foundation (Done)
- Serilog structured logging (Console, File, Seq, App Insights)
- Correlation ID propagation
- MediatR request/response/performance logging
- Global exception handling with TraceId in error response
- Health checks: PostgreSQL, Redis, Memory
- Frontend logger + telemetry service skeleton
- Frontend Core Web Vitals monitoring

### Phase 2 — Technical Observability
- OpenTelemetry distributed tracing (explicit ActivitySource spans)
- Prometheus metrics scrape endpoint (`/metrics`)
- Custom business counters via `AppMetrics` (orders, payments, cart)
- EF Core + HTTP client instrumentation

### Phase 3 — Business Events
- `BusinessEvent` entity + PostgreSQL table
- `IBusinessEventService` injected into Auth, Order, Cart, Payment services
- `BusinessEventTypes` constants catalogue
- Backend analytics endpoint (`POST /api/analytics/events`)
- Frontend sink wiring telemetry → backend

### Phase 4 — Aggregation & Insights
- Admin observability summary endpoint (`GET /api/admin/observability/summary`)
- KPI aggregate queries on `business_events`
- Anomaly detection alerts (error rate spike, payment failure spike)
- AI-generated insights layer (future)

---

## Tooling Map

| Tool | Role |
|------|------|
| **Serilog** | Structured log emission (backend) |
| **Seq** | Centralised log search and dashboards |
| **Application Insights** | Cloud-hosted log + telemetry (production) |
| **OpenTelemetry** | Vendor-neutral trace + metric instrumentation |
| **Prometheus** | Metric scrape and time-series storage |
| **Grafana** | Metric dashboards and alerting |
| **Jaeger / Tempo** | Distributed trace visualisation |
| **Sentry** | Frontend + backend error tracking (planned) |
| **RTK Query** | Frontend API state (hooks into telemetry on error) |
| **PerformanceObserver API** | Frontend Core Web Vitals (LCP, CLS, FID, FCP) |
| **PostgreSQL** | Business event and feedback persistence |
| **Redis** | Cache hit/miss metrics |

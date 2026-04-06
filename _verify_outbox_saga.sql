SELECT COUNT(*) AS outbox_total,
       COUNT(*) FILTER (WHERE "ProcessedAt" IS NOT NULL) AS outbox_processed
FROM integration.outbox_messages;

SELECT "OrderId","CorrelationId","CurrentState","CompletedAt"
FROM integration.order_fulfillment_saga_states
ORDER BY "CreatedAt" DESC
LIMIT 5;

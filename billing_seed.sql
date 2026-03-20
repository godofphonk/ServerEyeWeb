-- Create SubscriptionPlans table
CREATE TABLE IF NOT EXISTS "SubscriptionPlans" (
    "Id" uuid NOT NULL,
    "PlanType" text NOT NULL,
    "Name" text NOT NULL,
    "Description" text NOT NULL,
    "MonthlyPrice" numeric NOT NULL,
    "YearlyPrice" numeric NOT NULL,
    "MaxServers" integer NOT NULL,
    "MetricsRetentionDays" integer NOT NULL,
    "HasAlerts" boolean NOT NULL,
    "HasApiAccess" boolean NOT NULL,
    "HasPrioritySupport" boolean NOT NULL,
    "Features" jsonb NOT NULL,
    "IsActive" boolean NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL,
    "UpdatedAt" timestamp without time zone NOT NULL,
    CONSTRAINT "PK_SubscriptionPlans" PRIMARY KEY ("Id")
);

-- Create indexes
CREATE UNIQUE INDEX IF NOT EXISTS "IX_SubscriptionPlans_PlanType" ON "SubscriptionPlans" ("PlanType");

-- Insert seed data
INSERT INTO "SubscriptionPlans" ("Id", "PlanType", "Name", "Description", "MonthlyPrice", "YearlyPrice", "MaxServers", "MetricsRetentionDays", "HasAlerts", "HasApiAccess", "HasPrioritySupport", "Features", "IsActive", "CreatedAt", "UpdatedAt") VALUES
('00000000-0000-0000-0000-000000000001', 'Free', 'Free', 'Perfect for getting started with server monitoring', 0.00, 0.00, 3, 7, true, false, false, '{"maxAlerts": "10", "webhooks": "false", "slackIntegration": "false", "emailSupport": "false"}', true, NOW(), NOW()),
('00000000-0000-0000-0000-000000000002', 'Basic', 'Basic', 'Great for small teams and growing projects', 9.99, 99.99, 10, 30, true, true, false, '{"maxAlerts": "100", "webhooks": "false", "slackIntegration": "true", "emailSupport": "true"}', true, NOW(), NOW()),
('00000000-0000-0000-0000-000000000003', 'Pro', 'Pro', 'Advanced features for professional teams', 29.99, 299.99, 50, 90, true, true, true, '{"maxAlerts": "1000", "webhooks": "true", "slackIntegration": "true", "emailSupport": "true", "phoneSupport": "false", "customReports": "true"}', true, NOW(), NOW()),
('00000000-0000-0000-0000-000000000004', 'Enterprise', 'Enterprise', 'Complete solution for large organizations', 99.99, 999.99, -1, 365, true, true, true, '{"maxAlerts": "unlimited", "webhooks": "true", "slackIntegration": "true", "emailSupport": "true", "phoneSupport": "true", "customReports": "true", "dedicatedSupport": "true", "slaGuarantee": "true"}', true, NOW(), NOW());

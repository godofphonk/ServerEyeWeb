-- PostgreSQL initialization script for production
-- Creates separate users with least privilege for each database

-- Create main database user
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'servereye_main') THEN
    CREATE ROLE servereye_main WITH LOGIN PASSWORD '${MAIN_DB_PASSWORD}';
  END IF;
END
$$;

-- Grant main database user access
GRANT CONNECT ON DATABASE ServerEyeWeb_Prod TO servereye_main;
GRANT USAGE ON SCHEMA public TO servereye_main;
GRANT CREATE ON SCHEMA public TO servereye_main;

-- Create ticket database user
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'servereye_ticket') THEN
    CREATE ROLE servereye_ticket WITH LOGIN PASSWORD '${TICKET_DB_PASSWORD}';
  END IF;
END
$$;

-- Grant ticket database user access
GRANT CONNECT ON DATABASE ServerEyeWeb_Prod_Ticket TO servereye_ticket;
GRANT USAGE ON SCHEMA public TO servereye_ticket;
GRANT CREATE ON SCHEMA public TO servereye_ticket;

-- Create billing database user
DO $$
BEGIN
  IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'servereye_billing') THEN
    CREATE ROLE servereye_billing WITH LOGIN PASSWORD '${BILLING_DB_PASSWORD}';
  END IF;
END
$$;

-- Grant billing database user access
GRANT CONNECT ON DATABASE ServerEyeWeb_Prod_Billing TO servereye_billing;
GRANT USAGE ON SCHEMA public TO servereye_billing;
GRANT CREATE ON SCHEMA public TO servereye_billing;

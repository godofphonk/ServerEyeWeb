# PostgreSQL Users Migration Guide

## Overview
This document describes how to migrate from using the `postgres` superuser to separate users with least privilege for each database in production.

## Changes Made

### Infrastructure
- Created `environments/prod/infrastructure/init-db.sql` - SQL script that creates separate users for each database
- Updated `environments/prod/infrastructure/docker-compose.yml` to mount the init script and pass password variables

### Backend
- Updated `TicketDbContextFactory.cs` to use `servereye_ticket` user in dev
- Updated `BillingDesignTimeDbContextFactory.cs` to use `servereye_billing` user in dev

## Doppler Secrets Configuration

### Step 1: Add New Passwords to Doppler
Add the following secrets to the `prd_backend` config in Doppler:

```
MAIN_DB_PASSWORD=<strong_password>
TICKET_DB_PASSWORD=<strong_password>
BILLING_DB_PASSWORD=<strong_password>
```

### Step 2: Update Connection Strings in Doppler
Update the following connection strings in `prd_backend` config to use the new users:

#### Default Connection (Main Database)
```
DATABASE_DEFAULTCONNECTION=Host=ServerEyeWeb-postgres-main-prod;Port=5432;Database=ServerEyeWeb_Prod;Username=servereye_main;Password=${MAIN_DB_PASSWORD};SSL Mode=Disable
```

#### Ticket Database
```
DATABASE_TICKETDBCONTEXT=Host=ServerEyeWeb-postgres-ticket-prod;Port=5432;Database=ServerEyeWeb_Prod_Ticket;Username=servereye_ticket;Password=${TICKET_DB_PASSWORD};SSL Mode=Disable
```

#### Billing Database
```
DATABASE_BILLINGDBCONTEXT=Host=ServerEyeWeb-postgres-billing-prod;Port=5432;Database=ServerEyeWeb_Prod_Billing;Username=servereye_billing;Password=${BILLING_DB_PASSWORD};SSL Mode=Disable
```

## Deployment Steps

### 1. Deploy Infrastructure Changes
The init-db.sql script will automatically run when PostgreSQL containers start for the first time after the deployment.

### 2. Verify Users Created
Connect to each PostgreSQL container and verify users were created:

```bash
docker exec -it ServerEyeWeb-postgres-main-prod psql -U postgres -d ServerEyeWeb_Prod -c "\du"
docker exec -it ServerEyeWeb-postgres-ticket-prod psql -U postgres -d ServerEyeWeb_Prod_Ticket -c "\du"
docker exec -it ServerEyeWeb-postgres-billing-prod psql -U postgres -d ServerEyeWeb_Prod_Billing -c "\du"
```

Expected output should show:
- `servereye_main` with CONNECT on ServerEyeWeb_Prod
- `servereye_ticket` with CONNECT on ServerEyeWeb_Prod_Ticket
- `servereye_billing` with CONNECT on ServerEyeWeb_Prod_Billing

### 3. Test Application
Verify the application can connect to databases using the new users:
- Check application logs for connection errors
- Run integration tests
- Verify all database operations work correctly

## Rollback Plan
If issues occur, you can rollback by:
1. Revert connection strings in Doppler to use `postgres` user
2. Restart backend containers
3. The init-db.sql script won't recreate existing users, so no cleanup needed

## Security Benefits
- **Least Privilege**: Each application component has minimal required permissions
- **Audit Trail**: Database operations can be traced to specific users
- **Isolation**: Compromise of one database user doesn't affect others
- **Defense in Depth**: Additional security layer even within isolated Docker network

## Notes
- The `postgres` superuser is still required for database initialization and administrative tasks
- SSL remains disabled as databases are isolated within Docker network
- This change is backward compatible with existing data

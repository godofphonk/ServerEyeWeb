#!/bin/bash
set -e

# PostgreSQL initialization script
# Creates a separate user with least privilege for the current database
# This MUST be a .sh file (not .sql) so environment variables are substituted by bash
# Each container passes only its own password via environment variable

create_user() {
    local username="$1"
    local password="$2"

    if [ -z "$password" ]; then
        echo "⚠️ No password provided for $username, skipping"
        return
    fi

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        DO \$\$
        BEGIN
          IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$username') THEN
            CREATE ROLE $username WITH LOGIN PASSWORD '$password';
          END IF;
        END
        \$\$;

        GRANT CONNECT ON DATABASE "$POSTGRES_DB" TO $username;
        GRANT USAGE ON SCHEMA public TO $username;
        GRANT CREATE ON SCHEMA public TO $username;
        ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO $username;
        ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO $username;
EOSQL

    echo "✅ User $username created for database $POSTGRES_DB"
}

# Each container only has its own password env var set
# The others will be empty and skipped
create_user "servereye_main" "$MAIN_DB_PASSWORD"
create_user "servereye_ticket" "$TICKET_DB_PASSWORD"
create_user "servereye_billing" "$BILLING_DB_PASSWORD"

echo "✅ Database initialization completed"

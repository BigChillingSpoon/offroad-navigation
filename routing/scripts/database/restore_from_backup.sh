#!/bin/bash
# Restores the offroad database from a pg_dump backup created by
# build_routing_graph.sh's Step 7 (routing/graphhopper/data/backups/).
#
# Usage:
#   ./restore_from_backup.sh                     # restores offroad_latest.dump
#   ./restore_from_backup.sh path/to/some.dump    # restores a specific dump
set -e

DB_HOST="localhost"
DB_PORT="5433"
DB_NAME="offroad"
DB_USER="offroad"
export PGPASSWORD="offroad"

BACKUP_DIR="../../graphhopper/data/backups"
BACKUP_FILE="${1:-$BACKUP_DIR/offroad_latest.dump}"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Backup file not found: $BACKUP_FILE"
    exit 1
fi

echo "Restoring $BACKUP_FILE into database '$DB_NAME'..."
pg_restore -h $DB_HOST -p $DB_PORT -U $DB_USER -d $DB_NAME --clean --if-exists --no-owner "$BACKUP_FILE"
echo "Restore complete."

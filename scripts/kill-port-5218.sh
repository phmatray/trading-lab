#!/bin/bash
# Script to kill any process using port 5218

PORT=5218

echo "🔍 Checking for processes using port $PORT..."

# Find process ID using the port
PID=$(lsof -ti:$PORT)

if [ -z "$PID" ]; then
    echo "✅ No process found using port $PORT"
    exit 0
else
    echo "⚠️  Found process $PID using port $PORT"
    echo "🔪 Killing process $PID..."
    kill -9 $PID

    # Wait a moment and verify
    sleep 1

    # Check if process still exists
    if lsof -ti:$PORT > /dev/null 2>&1; then
        echo "❌ Failed to kill process on port $PORT"
        exit 1
    else
        echo "✅ Successfully killed process on port $PORT"
        exit 0
    fi
fi

#!/bin/bash
# Script to kill any existing process on port 5218 and start the application

PORT=5218
PROJECT_PATH="/Users/phmatray/Repositories/github-phm/TradingStrat/src/TradingStrat.Web"

echo "🚀 Starting TradingStrat Web Application..."
echo ""

# Kill any existing process on port 5218
echo "🔍 Checking for processes using port $PORT..."
PID=$(lsof -ti:$PORT)

if [ ! -z "$PID" ]; then
    echo "⚠️  Found process $PID using port $PORT"
    echo "🔪 Killing process $PID..."
    kill -9 $PID
    sleep 1
    echo "✅ Killed existing process"
fi

echo ""
echo "▶️  Starting application..."
echo ""

# Start the application
cd "$PROJECT_PATH"
dotnet run

# TradingStrat Scripts

This directory contains utility scripts for managing the TradingStrat application.

## Available Scripts

### kill-port-5218.sh
Kills any process currently using port 5218.

**Usage:**
```bash
./scripts/kill-port-5218.sh
```

**When to use:**
- When you get "address already in use" errors
- Before starting the application manually with `dotnet run`
- To clean up orphaned processes

### start-app.sh
Automatically kills any process on port 5218 and starts the TradingStrat Web application.

**Usage:**
```bash
./scripts/start-app.sh
```

**What it does:**
1. Checks if port 5218 is in use
2. Kills any existing process on that port
3. Starts the application with `dotnet run`

**Recommended usage:**
Use this script instead of `dotnet run` to avoid "address already in use" errors.

## Making Scripts Executable

If you get permission errors, make the scripts executable:

```bash
chmod +x scripts/kill-port-5218.sh
chmod +x scripts/start-app.sh
```

## Alternative: Manual Port Cleanup

If you prefer to manually find and kill the process:

```bash
# Find process using port 5218
lsof -ti:5218

# Kill the process (replace PID with the number from above)
kill -9 PID
```

Or use a one-liner:

```bash
lsof -ti:5218 | xargs kill -9 2>/dev/null || echo "No process found on port 5218"
```

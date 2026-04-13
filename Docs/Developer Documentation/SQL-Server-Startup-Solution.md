# SQL Server Startup Delay Solution

## Problem Description
When the production server restarts, the Collectibles application fails to start because SQL Server is not yet fully initialized. This results in connection errors and the application pool getting stuck in a failed state, requiring manual intervention to recycle the pool.

## Solution Overview
The implemented solution addresses this issue through multiple layers of resilience:

### 1. Database Connectivity Service
**File**: `Source/Collectibles.Infrastructure/Services/DatabaseConnectivityService.cs`

- Runs as the first hosted service during application startup
- Implements exponential backoff retry logic (up to 30 attempts over ~8 minutes)
- Ensures SQL Server is responsive before allowing the application to proceed
- Stops the application gracefully if database connection cannot be established

### 2. Hangfire Schema Initialization Service
**File**: `Source/Collectibles.Infrastructure/Services/HangfireSchemaInitializerService.cs`

- Separates Hangfire schema initialization from the main startup
- Runs after database connectivity is confirmed
- Implements retry logic specifically for Hangfire initialization
- Allows the application to start even if Hangfire fails (non-critical service)

### 3. Health Check Endpoint
**Endpoint**: `/health`

- Provides real-time database connectivity status
- Returns JSON response with detailed health information
- Used by IIS Application Initialization module to verify app readiness
- Can be monitored by external systems

### 4. Enhanced Connection Resilience
- SQL Server connections configured with retry logic (5 retries, 30-second max delay)
- Hangfire configured to not automatically create schema on startup
- All database operations wrapped in Polly retry policies

### 5. IIS Configuration
**Files**: 
- `Source/Collectibles.Web/web.Production.config`
- `Scripts/Configure-IISAppPool.ps1`

#### IIS Settings:
- **startupTimeLimit**: 300 seconds (5 minutes) - allows more time for startup
- **Application Initialization**: Configured to warm up the app using `/health` endpoint
- **Start Mode**: AlwaysRunning - ensures automatic start
- **Idle Timeout**: 0 - prevents shutdown due to inactivity
- **Rapid Fail Protection**: 10 failures in 10 minutes (more tolerant)

#### Scheduled Task:
- Delays application pool start by 120 seconds after system restart
- Waits for SQL Server service to be running
- Performs application warm-up after starting the pool
- Logs events to Windows Event Log for monitoring

## Usage Instructions

### Initial Setup (One-time on Production Server)

1. **Deploy the updated application** with all the new services and configurations

2. **Run the IIS configuration script** as Administrator:
   ```powershell
   .\Scripts\Configure-IISAppPool.ps1 -AppPoolName "YourAppPoolName" -SiteName "YourSiteName" -StartupDelaySeconds 120
   ```

3. **Verify the configuration**:
   - Check IIS Manager for updated application pool settings
   - Verify scheduled task exists in Task Scheduler
   - Test the health endpoint: `http://yourserver/health`

### Monitoring

1. **Windows Event Log**:
   - Source: "IIS-Configuration"
   - Events logged for startup delays and warm-up status

2. **Application Logs**:
   - Located in `Logs\` directory
   - Contains detailed startup sequence information

3. **Health Endpoint**:
   - Monitor `/health` for real-time status
   - Returns JSON with database connectivity status

### Troubleshooting

If the application still fails to start:

1. **Check Event Logs** for SQL Server and application errors
2. **Increase startup delay** in the PowerShell script if needed
3. **Verify SQL Server service dependencies** are correctly configured
4. **Review application logs** in the `Logs` folder for detailed error messages

### Manual Recovery

If automatic recovery fails:

1. Ensure SQL Server is running:
   ```powershell
   Get-Service MSSQLSERVER
   ```

2. Manually start the application pool:
   ```powershell
   Import-Module WebAdministration
   Start-WebAppPool -Name "YourAppPoolName"
   ```

3. Test the application:
   ```powershell
   Invoke-WebRequest -Uri "http://localhost/health" -UseBasicParsing
   ```

## Configuration Options

### Adjust Retry Behavior
Edit `DatabaseConnectivityService.cs` to modify:
- `retryCount`: Number of connection attempts (default: 30)
- `sleepDurationProvider`: Delay between retries (exponential backoff)

### Adjust Startup Delay
When running `Configure-IISAppPool.ps1`, use the `-StartupDelaySeconds` parameter:
```powershell
.\Scripts\Configure-IISAppPool.ps1 -AppPoolName "AppPool" -StartupDelaySeconds 180
```

### Database Timeout Settings
In `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...;Connection Timeout=60;..."
  }
}
```

## Benefits

1. **Automatic Recovery**: Application recovers automatically from SQL Server startup delays
2. **No Manual Intervention**: Eliminates the need to manually recycle the application pool
3. **Graceful Degradation**: Application can start even if non-critical services (like Hangfire) fail
4. **Comprehensive Logging**: Detailed logs for troubleshooting startup issues
5. **Health Monitoring**: Real-time health status available via HTTP endpoint
6. **Production-Ready**: Handles edge cases and provides fallback mechanisms
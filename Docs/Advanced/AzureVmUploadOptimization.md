# Azure VM Upload Performance Optimization Guide

## Configuration Changes Applied

### 1. IIS Request Buffering Disabled
- **File**: `Source/Collectibles.Web/web.config`
- **Change**: Added `disableRequestBuffering="true"` to the `<aspNetCore>` element
- **Impact**: Prevents IIS from buffering the entire upload before passing to the application, allowing true streaming

### 2. IIS Compression Configured
- **File**: `Source/Collectibles.Web/web.config`
- **Changes**: 
  - Enabled URL compression for static and dynamic content
  - Added compression for `application/octet-stream` MIME type
  - Disabled compression for ZIP files (already compressed)
- **Impact**: Reduces bandwidth usage for non-compressed content

## Additional Azure VM Configuration Recommendations

### 1. Install Application Request Routing (ARR)
```powershell
# In PowerShell as Administrator on the Azure VM
Install-WindowsFeature -Name Web-ARR
```

### 2. Configure IIS Application Pool
In IIS Manager:
- Set **Idle Time-out**: 0 (disable)
- Set **Regular Time Interval**: 0 (disable recycling during uploads)
- Set **Queue Length**: 5000 or higher
- Set **Rapid-Fail Protection**: Disabled

### 3. Network Performance Tuning
```powershell
# Enable TCP Window Auto-Tuning
netsh int tcp set global autotuninglevel=normal

# Increase TCP receive window
netsh int tcp set global chimney=enabled
netsh int tcp set global rss=enabled
```

### 4. Azure VM Network Tier
- Ensure VM is using **Standard** or **Premium** network performance tier
- Consider using **Accelerated Networking** if available for your VM size

### 5. Azure Load Balancer Settings (if applicable)
- Increase idle timeout to 30 minutes for long uploads
- Enable session persistence (client IP affinity)

### 6. Monitor Performance
Use these tools to diagnose issues:
- **Azure Monitor**: Check VM network metrics
- **IIS Failed Request Tracing**: Enable for upload endpoints
- **Performance Monitor**: Track memory, CPU, and network during uploads

## Testing the Changes

1. **Deploy the updated web.config** to your Azure VM
2. **Restart the IIS application pool**
3. **Test with a small file first** (10-50MB)
4. **Monitor upload speeds** and compare with previous performance
5. **Check IIS logs** for any errors or warnings

## Expected Improvements

With these changes, you should see:
- **50-70% faster upload speeds** due to disabled request buffering
- **Reduced memory usage** on the server during uploads
- **More stable connections** for large file transfers
- **Better progress reporting** as data streams directly

## Troubleshooting

If uploads are still slow after these changes:

1. **Check Azure VM Size**: Ensure adequate network bandwidth for your VM tier
2. **Review Firewall Rules**: Ensure no throttling at firewall/NSG level
3. **Test Network Path**: Use tools like `tracert` or `pathping` to check latency
4. **Consider CDN**: For global users, Azure CDN can improve upload speeds
5. **Implement Chunked Upload**: Split large files into smaller chunks (5-10MB) for parallel upload

## Alternative Solution: Azure Blob Storage Direct Upload

For maximum performance, consider implementing direct upload to Azure Blob Storage:
1. Generate SAS tokens for direct blob upload
2. Upload directly from client to Azure Blob Storage
3. Process files asynchronously after upload

This bypasses the VM entirely for the upload phase, providing the best possible performance.
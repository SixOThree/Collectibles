using System.Text.Json;

using Collectibles.Application.Interfaces;
using Collectibles.Domain.Entities;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Collectibles.Web.Components.Common;

public abstract class LoggablePageBase : ComponentBase
{
    [Inject]
    protected IEventLogService EventLogService { get; set; } = default!;

    [Inject]
    protected NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    [Inject]
    protected IJSRuntime JSRuntime { get; set; } = default!;

    [Inject]
    protected ISysLogService SysLogService { get; set; } = default!;

    protected string PageName => GetType().Name.Replace("Page", string.Empty).Replace("Component", string.Empty);

    protected TimeZoneInfo? _browserTimeZone;
    protected string _browserTimeZoneId = "UTC";
    protected bool _timeZoneDetected = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await DetectBrowserTimeZone();
            await LogPageView();
            StateHasChanged();
        }
    }

    private async Task DetectBrowserTimeZone()
    {
        try
        {
            var timeZoneId = await JSRuntime.InvokeAsync<string>("getBrowserTimeZone");

            if (!string.IsNullOrEmpty(timeZoneId))
            {
                _browserTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                _browserTimeZoneId = timeZoneId;
                _timeZoneDetected = true;
            }
        }
        catch
        {
            _browserTimeZone = null;
            _browserTimeZoneId = "UTC";
            _timeZoneDetected = false;
        }
    }

    protected string FormatTimestamp(DateTime utcTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        if (_browserTimeZone != null)
        {
            var utc = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(utc, _browserTimeZone);
            return localTime.ToString(format);
        }

        return utcTime.ToString(format);
    }

    protected string FormatTimestamp(DateTime? utcTime, string format = "yyyy-MM-dd HH:mm:ss")
    {
        if (!utcTime.HasValue)
        {
            return string.Empty;
        }

        return FormatTimestamp(utcTime.Value, format);
    }

    protected virtual async Task LogPageView()
    {
        try
        {
            var uri = new Uri(NavigationManager.Uri);

            // Get user context from AuthenticationStateProvider for Blazor Interactive components
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user?.Identity?.Name;

            var additionalData = new
            {
                Page = PageName,
                Url = uri.AbsolutePath,
                QueryString = uri.Query,
                Referrer = NavigationManager.BaseUri,
            };

            // Use the explicit context overload
            await EventLogService.LogEventAsync(
                EventAction.View,
                entityType: null,
                entityId: null,
                entityName: null,
                oldValues: null,
                newValues: null,
                JsonSerializer.Serialize(additionalData),
                userId,
                userEmail,
                ipAddress: null,
                userAgent: null,
                sessionId: null);
        }
        catch
        {
            // Silently fail to avoid disrupting the user experience
        }
    }

    protected async Task LogAction(EventAction action, object? data = null)
    {
        try
        {
            var additionalData = new
            {
                Page = PageName,
                Data = data,
            };

            // Get user context from AuthenticationStateProvider for Blazor Interactive components
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user?.Identity?.Name;

            // Use the explicit context overload for user activity
            await EventLogService.LogEventAsync(
                action,
                entityType: null,
                entityId: null,
                entityName: null,
                oldValues: null,
                newValues: null,
                JsonSerializer.Serialize(additionalData),
                userId,
                userEmail,
                ipAddress: null,
                userAgent: null,
                sessionId: null);
        }
        catch
        {
            // Silently fail to avoid disrupting the user experience
        }
    }

    protected async Task LogEntityAction(
        EventAction action,
        string entityType,
        long entityId,
        string entityName,
        object? oldValues = null,
        object? newValues = null,
        object? additionalData = null)
    {
        try
        {
            var extendedData = new
            {
                Page = PageName,
                AdditionalInfo = additionalData,
            };

            // Get user context from AuthenticationStateProvider for Blazor Interactive components
            var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userEmail = user?.Identity?.Name;

            // Use the overload with explicit context information
            await EventLogService.LogEventAsync(
                action,
                entityType,
                entityId,
                entityName,
                oldValues,
                newValues,
                JsonSerializer.Serialize(extendedData),
                userId,
                userEmail,
                ipAddress: null, // Can't get IP in Blazor Interactive without JS interop
                userAgent: null, // Can't get UserAgent in Blazor Interactive without JS interop
                sessionId: null); // Can't get SessionId in Blazor Interactive without special handling
        }
        catch
        {
            // Silently fail to avoid disrupting the user experience
        }
    }

    protected async Task LogErrorToSysLog(Exception ex, string context)
    {
        try
        {
            await SysLogService.LogErrorAsync($"{context}: {ex.Message}", ex, "BlazorPage");
        }
        catch
        {
            // Silently fail - don't disrupt UX for logging failures
        }
    }
}

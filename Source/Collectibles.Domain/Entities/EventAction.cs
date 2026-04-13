namespace Collectibles.Domain.Entities;

public enum EventAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    View = 3,
    Login = 4,
    Logout = 5,
    Upload = 6,
    Download = 7,
    Export = 8,
    Import = 9,
    Search = 10,
    Share = 11,
    BulkOperation = 12,
    PermissionChange = 13,
    ConfigurationChange = 14,
    PasswordChange = 15,
    ThemeChange = 16,
    AccountManagement = 17,
    TwoFactorSetup = 18,
    DataExport = 19,
    AccountDeletion = 20,
    Registration = 21,
    MaintenanceOperation = 22,
    Other = 99,
}

# Adding New Event Actions Guide

This guide explains how to add new event actions to the system and ensure they are properly displayed in the User Stories feature.

## Steps to Add a New Event Action

### 1. Add to EventAction Enum
First, add your new action to the `EventAction` enum in `/Source/Collectibles.Domain/Entities/EventLog.cs`:

```csharp
public enum EventAction
{
    // ... existing actions ...
    YourNewAction = 15,  // Use the next available number
    // ... other actions ...
}
```

### 2. Update User Stories Display (Optional)
The User Stories page will automatically handle new actions using the default formatting. However, for better user experience, you can add custom display text:

Edit `/Source/Collectibles.Web/Components/Pages/Management/UserStories.razor`:

```csharp
private static readonly Dictionary<EventAction, (string verb, string noun, bool useCount)> ActionDisplayConfig = new()
{
    // ... existing actions ...
    { EventAction.YourNewAction, ("performed your action on", "item", true) },
    // ... other actions ...
};
```

The tuple contains:
- `verb`: The action verb to display (e.g., "created", "updated", "downloaded")
- `noun`: The object of the action (e.g., "item", "file", "user")
- `useCount`: Whether to show count (true for "3 items", false for actions like "logged in")

### 3. Log Events Using the New Action
Use the new action when logging events:

```csharp
await _eventLogService.LogEventAsync(
    EventAction.YourNewAction,
    entityType: "YourEntity",
    entityId: entity.Id,
    entityName: entity.Name,
    additionalData: "Optional additional context"
);
```

## Default Behavior

If you don't add a custom display configuration, the User Stories page will:
1. Convert the enum name from PascalCase to space-separated lowercase
   - Example: `PasswordReset` → "password reset"
2. Display as: "password reset 3 times" (or "1 time" for single occurrences)

## Best Practices

1. **Use Descriptive Names**: Choose clear, action-oriented names for your enum values
2. **Be Consistent**: Follow the existing naming pattern (verb-based when possible)
3. **Consider Context**: Some actions work better with specific nouns (e.g., "uploaded 3 files" vs "uploaded 3 items")
4. **Test the Story**: After adding a new action, test how it appears in the User Stories page

## Example: Adding a "Archive" Action

1. Add to enum:
```csharp
Archive = 15,
```

2. Add to display config (optional):
```csharp
{ EventAction.Archive, ("archived", "item", true) },
```

3. Use in code:
```csharp
await _eventLogService.LogEventAsync(
    EventAction.Archive,
    entityType: "CollectibleItem",
    entityId: item.Id,
    entityName: item.Name
);
```

Result in User Story: "The user archived CollectibleItem 'Vintage Baseball Card'"

## Notes

- The system is designed to be forward-compatible with new actions
- All unrecognized actions will still be displayed using the default formatter
- The story generation logic groups consecutive similar actions for readability
- Entity-specific information (type, name) is preserved and displayed when available
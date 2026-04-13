# Configurable Card Dimensions Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add site-wide configurable card image height and card min-width settings, and fix a CSS isolation bug that broke fixed image heights.

**Architecture:** Two `SiteConfiguration` keys (`CardImageHeight`, `CardMinWidth`) read by `ShowcaseDetail` and passed to `CollectibleItemCard` via parameters. Admin configures values on the existing Site Configuration page.

**Tech Stack:** .NET 8, Blazor Server, Blazor CSS isolation (`::deep`), `ISiteConfigurationService`

---

### Task 1: Fix CSS isolation bug on CollectibleItemCard

The CSS consolidation moved `.item-image { height: 200px }` from an inline `<style>` (global) to `CollectibleItemCard.razor.css` (scoped). Since the `<img>` is rendered inside the `LazyImage` child component, the scoped selector no longer reaches it. Fix with `::deep`.

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/CollectibleItemCard.razor.css:18-33`

- [ ] **Step 1: Add `::deep` to `.item-image` and `.item-placeholder` selectors**

In `CollectibleItemCard.razor.css`, change lines 18-33 from:

```css
.item-image {
    width: 100%;
    height: 200px;
    object-fit: cover;
}

.item-placeholder {
    width: 100%;
    height: 200px;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #f3f4f6;
    font-size: 3rem;
    color: #9ca3af;
}
```

to:

```css
::deep .item-image {
    width: 100%;
    height: 200px;
    object-fit: cover;
}

::deep .item-placeholder {
    width: 100%;
    height: 200px;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #f3f4f6;
    font-size: 3rem;
    color: #9ca3af;
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Source/Collectibles.Web`
Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add Source/Collectibles.Web/Components/Shared/CollectibleItemCard.razor.css
git commit -m "fix: add ::deep to item-image CSS to fix isolation scoping into LazyImage"
```

---

### Task 2: Add ImageHeight parameter to CollectibleItemCard

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/CollectibleItemCard.razor:1-105`

- [ ] **Step 1: Add ImageHeight parameter**

In `CollectibleItemCard.razor`, in the `@code` block at line 69, add this parameter after `MaxTagsToShow`:

```csharp
[Parameter] public int ImageHeight { get; set; } = 200;
```

- [ ] **Step 2: Pass height style to LazyImage**

On line 7, change the `LazyImage` usage from:

```razor
<LazyImage Src="@Item.PreviewImageUrl"
           Alt="@Item.Name"
           Class="item-image"
           PlaceholderClass="item-placeholder" />
```

to:

```razor
<LazyImage Src="@Item.PreviewImageUrl"
           Alt="@Item.Name"
           Class="item-image"
           PlaceholderClass="item-placeholder"
           Style="@($"height:{ImageHeight}px")" />
```

- [ ] **Step 3: Pass height style to the static placeholder**

On line 14, change:

```razor
<div class="item-placeholder">
```

to:

```razor
<div class="item-placeholder" style="height:@(ImageHeight)px">
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build Source/Collectibles.Web`
Expected: Build succeeds with no errors.

- [ ] **Step 5: Commit**

```bash
git add Source/Collectibles.Web/Components/Shared/CollectibleItemCard.razor
git commit -m "feat: add ImageHeight parameter to CollectibleItemCard"
```

---

### Task 3: Wire ShowcaseDetail to read card settings and pass to cards

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/ShowcaseDetail.razor:1-513`

- [ ] **Step 1: Inject ISiteConfigurationService**

At line 20, after the existing `@inject` lines, add:

```razor
@inject ISiteConfigurationService SiteConfigurationService
```

- [ ] **Step 2: Add fields for card settings**

In the `@code` block, after line 266 (`private bool _showShareModal = false;`), add:

```csharp
private int _cardImageHeight = 200;
private int _cardMinWidth = 280;
```

- [ ] **Step 3: Load card settings in OnInitializedAsync**

In `OnInitializedAsync()`, after the `await LoadShowcase();` call on line 270, add:

```csharp
var imageHeightStr = await SiteConfigurationService.GetConfigurationValueAsync("CardImageHeight", "200");
var minWidthStr = await SiteConfigurationService.GetConfigurationValueAsync("CardMinWidth", "280");
if (int.TryParse(imageHeightStr, out var ih)) _cardImageHeight = ih;
if (int.TryParse(minWidthStr, out var mw)) _cardMinWidth = mw;
```

- [ ] **Step 4: Apply CardMinWidth to the grid via inline style**

On line 139, change:

```razor
<div class="items-grid">
```

to:

```razor
<div class="items-grid" style="grid-template-columns: repeat(auto-fill, minmax(@(_cardMinWidth)px, 1fr))">
```

- [ ] **Step 5: Pass ImageHeight to CollectibleItemCard**

On line 142, change:

```razor
<CollectibleItemCard Item="item" OnItemClick="HandleItemClick" />
```

to:

```razor
<CollectibleItemCard Item="item" OnItemClick="HandleItemClick" ImageHeight="_cardImageHeight" />
```

- [ ] **Step 6: Build and verify**

Run: `dotnet build Source/Collectibles.Web`
Expected: Build succeeds with no errors.

- [ ] **Step 7: Commit**

```bash
git add Source/Collectibles.Web/Components/Pages/ShowcaseDetail.razor
git commit -m "feat: read card dimension settings and pass to item cards"
```

---

### Task 4: Remove hardcoded grid column width from ShowcaseDetail CSS

Since the grid column width is now set via inline style, remove the hardcoded value from CSS (keep the rest of the grid rules).

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/ShowcaseDetail.razor.css:132-137`

- [ ] **Step 1: Remove grid-template-columns from .items-grid**

In `ShowcaseDetail.razor.css`, change lines 132-137 from:

```css
.items-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
    gap: 1.5rem;
    margin-top: 1.5rem;
}
```

to:

```css
.items-grid {
    display: grid;
    gap: 1.5rem;
    margin-top: 1.5rem;
}
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build Source/Collectibles.Web`
Expected: Build succeeds with no errors.

- [ ] **Step 3: Commit**

```bash
git add Source/Collectibles.Web/Components/Pages/ShowcaseDetail.razor.css
git commit -m "refactor: remove hardcoded grid column width, now set dynamically"
```

---

### Task 5: Add Card Display Settings to Site Configuration page

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Management/SiteConfiguration.razor:1-110`

- [ ] **Step 1: Add card settings fields to ConfigurationModel**

In the `ConfigurationModel` class at line 106, add these properties:

```csharp
private class ConfigurationModel
{
    public string RegistrationPendingMessage { get; set; } = "Please contact the site administrator to complete your registration.";
    public int CardImageHeight { get; set; } = 200;
    public int CardMinWidth { get; set; } = 280;
}
```

- [ ] **Step 2: Load card settings in LoadConfiguration**

In the `LoadConfiguration()` method, after loading `RegistrationPendingMessage` (line 71), add:

```csharp
var imageHeight = await SiteConfigurationService.GetConfigurationValueAsync("CardImageHeight", "200");
if (int.TryParse(imageHeight, out var ih)) configModel.CardImageHeight = ih;

var minWidth = await SiteConfigurationService.GetConfigurationValueAsync("CardMinWidth", "280");
if (int.TryParse(minWidth, out var mw)) configModel.CardMinWidth = mw;
```

- [ ] **Step 3: Add validation and save logic for card settings**

In the `SaveConfiguration()` method, after saving `RegistrationPendingMessage` (line 89), add validation and save:

```csharp
if (configModel.CardImageHeight < 100 || configModel.CardImageHeight > 500)
{
    statusMessage = "Card Image Height must be between 100 and 500 pixels.";
    isError = true;
    return;
}

if (configModel.CardMinWidth < 200 || configModel.CardMinWidth > 600)
{
    statusMessage = "Card Min Width must be between 200 and 600 pixels.";
    isError = true;
    return;
}

await SiteConfigurationService.SetConfigurationValueAsync(
    "CardImageHeight",
    configModel.CardImageHeight.ToString(),
    "Height in pixels for item card images (100-500)");

await SiteConfigurationService.SetConfigurationValueAsync(
    "CardMinWidth",
    configModel.CardMinWidth.ToString(),
    "Minimum width in pixels for item cards in the grid (200-600)");
```

- [ ] **Step 4: Add Card Display Settings UI section**

In the markup, after the closing `</div>` of the "Registration Settings" card (after line 51), add:

```razor
<div class="card mt-4">
    <div class="card-header">
        <h5>Card Display Settings</h5>
    </div>
    <div class="card-body">
        <EditForm Model="@configModel" OnValidSubmit="SaveConfiguration">
            <DataAnnotationsValidator />
            <ValidationSummary />
            
            <div class="mb-3">
                <label for="cardImageHeight" class="form-label">Card Image Height (px)</label>
                <InputNumber id="cardImageHeight" @bind-Value="configModel.CardImageHeight" class="form-control" style="max-width: 200px;" />
                <div class="form-text">
                    Height of item card images. Valid range: 100-500px. Default: 200px.
                </div>
            </div>

            <div class="mb-3">
                <label for="cardMinWidth" class="form-label">Card Min Width (px)</label>
                <InputNumber id="cardMinWidth" @bind-Value="configModel.CardMinWidth" class="form-control" style="max-width: 200px;" />
                <div class="form-text">
                    Minimum width of item cards in the grid. Valid range: 200-600px. Default: 280px.
                </div>
            </div>
            
            <button type="submit" class="btn btn-primary" disabled="@isSaving">
                @if (isSaving)
                {
                    <Spinner Type="SpinnerType.Dots" Size="SpinnerSize.Small" Color="SpinnerColor.Light" Class="me-2" />
                }
                Save Changes
            </button>
        </EditForm>
    </div>
</div>
```

- [ ] **Step 5: Build and verify**

Run: `dotnet build Source/Collectibles.Web`
Expected: Build succeeds with no errors.

- [ ] **Step 6: Commit**

```bash
git add Source/Collectibles.Web/Components/Pages/Management/SiteConfiguration.razor
git commit -m "feat: add card display settings to site configuration page"
```

---

### Task 6: Manual verification

- [ ] **Step 1: Run the application**

Run: `dotnet run --project Source/Collectibles.Web`

- [ ] **Step 2: Verify the CSS fix**

Navigate to a showcase detail page (e.g., `/showcase/maQLx8k5R6oY`). Confirm all item cards have uniform height and images are constrained to 200px (the default).

- [ ] **Step 3: Verify the admin settings**

Navigate to `/Management/SiteConfiguration`. Confirm:
- The "Card Display Settings" section appears with two numeric inputs
- Default values are 200 and 280
- Saving works and shows success message
- Out-of-range values (e.g., 50 or 999) show validation error

- [ ] **Step 4: Verify settings take effect**

Change Card Image Height to 300 and Card Min Width to 350, save. Navigate back to the showcase detail page. Confirm:
- Card images are now 300px tall
- Cards are wider (minimum 350px per column)

- [ ] **Step 5: Reset to defaults if desired**

Set values back to 200 and 280 if preferred.

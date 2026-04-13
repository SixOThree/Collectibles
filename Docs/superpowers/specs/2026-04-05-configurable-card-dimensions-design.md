# Configurable Card Dimensions

## Summary

Add site-wide configurable settings for card image height and card minimum width on the showcase detail page. Also fix a CSS isolation bug introduced during CSS consolidation where image height constraints no longer reach into the `LazyImage` child component.

## Bug Fix (Prerequisite)

During CSS consolidation, `.item-image { height: 200px }` was moved from an inline `<style>` block (global scope) to `CollectibleItemCard.razor.css` (component-scoped). Blazor CSS isolation scopes selectors to the declaring component only, so this style no longer applies to the `<img>` rendered inside the `LazyImage` child component. Images now grow to their natural size instead of being constrained.

**Fix:** Add `::deep` combinator to `.item-image` and `.item-placeholder` selectors in `CollectibleItemCard.razor.css` so they penetrate into child components.

## Storage

Two keys in the existing `SiteConfigurations` table via `ISiteConfigurationService`:

| Key | Default | Valid Range | Description |
|-----|---------|-------------|-------------|
| `CardImageHeight` | `200` | 100-500 px | Height of the image area on item cards |
| `CardMinWidth` | `280` | 200-600 px | Minimum width of item cards in the grid |

## Admin UI

A new "Card Display Settings" card section on the existing Site Configuration page (`/Management/SiteConfiguration`):

- `InputNumber` for "Card Image Height (px)" with help text: "Valid range: 100-500px. Default: 200px"
- `InputNumber` for "Card Min Width (px)" with help text: "Valid range: 200-600px. Default: 280px"
- Validation on save rejects out-of-range values with an error message
- Uses the existing save/status message pattern already on the page

## Data Flow

1. `ShowcaseDetail.razor` injects `ISiteConfigurationService` and reads `CardImageHeight` and `CardMinWidth` on load
2. `ShowcaseDetail` applies `CardMinWidth` to the grid via inline style: `grid-template-columns: repeat(auto-fill, minmax({CardMinWidth}px, 1fr))`
3. `ShowcaseDetail` passes `ImageHeight` to each `CollectibleItemCard` component
4. `CollectibleItemCard` accepts an `ImageHeight` parameter (default 200) and passes height via inline style to the image/placeholder elements
5. `LazyImage` already accepts a `Style` parameter, so height flows through naturally

## Files Modified

1. **`CollectibleItemCard.razor.css`** — Add `::deep` to `.item-image` and `.item-placeholder` selectors to fix CSS isolation scoping bug
2. **`CollectibleItemCard.razor`** — Add `ImageHeight` parameter; pass `style="height:{ImageHeight}px"` to image elements and `LazyImage`
3. **`ShowcaseDetail.razor`** — Inject `ISiteConfigurationService`; read card settings on load; pass `ImageHeight` to cards; apply grid min-width via inline style
4. **`ShowcaseDetail.razor.css`** — Remove hardcoded `minmax(280px, 1fr)` (replaced by dynamic inline style on the grid element)
5. **`SiteConfiguration.razor`** — Add "Card Display Settings" card section with two `InputNumber` fields and validation

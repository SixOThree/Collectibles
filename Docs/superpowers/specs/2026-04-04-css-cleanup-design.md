# CSS Cleanup: Remove Redundant Inline Styles

## Goal

Remove redundant CSS from inline `<style>` blocks across 35 Razor components. Styles that duplicate Bootstrap defaults, get overridden by the active theme, or can be replaced with Bootstrap utility classes should be removed. No visual changes allowed.

## Constraints

- Zero visual regression in both light and dark mode across themes
- Playwright screenshots before/after each file to verify
- Work split across 3 branches, each with its own PR

## Approach

For each file:

1. **Identify redundant declarations** - styles that restate Bootstrap or theme defaults (e.g., `.btn` colors, `.card` backgrounds, `.form-control` borders)
2. **Replace with Bootstrap utilities** - where a style rule exists solely to set margin, padding, flexbox, or similar layout properties that Bootstrap utility classes handle (e.g., `mb-3`, `d-flex`, `gap-2`)
3. **Extract to `.razor.css` isolation files** - remaining custom styles that are specific to the component move from inline `<style>` blocks to scoped CSS isolation files
4. **Consolidate duplicates** - CreateTemplate.razor and EditTemplate.razor share identical 219-line style blocks; extract to a shared CSS file
5. **Verify** - Playwright screenshot comparison before and after

### What counts as redundant

- Re-declaring a Bootstrap default (e.g., `color: #212529` on body text, `background-color: #fff` on cards)
- Declaring a property that the Bootswatch/custom theme already sets on the same selector
- Declaring layout properties that could be Bootstrap utility classes on the element
- Styles on elements that already have Bootstrap classes providing the same effect

### What stays

- Custom layout/positioning not covered by Bootstrap utilities
- Component-specific visual design (custom gradients, animations, unique color schemes)
- Intentional overrides that make a component look different from Bootstrap defaults
- Responsive breakpoints for component-specific layouts
- Any style whose removal changes the visual output

## Branch Structure

### Branch 1: `css-cleanup-shared` (16 files)

Shared components - highest reuse, changes propagate to many pages.

| File | Style Lines | Notes |
|------|------------|-------|
| AttachmentDetailModal.razor | 44 | Borders, backgrounds |
| AttachmentDetailView.razor | 31 | Background, hover |
| AttachmentThumbnailView.razor | 66 | Card styling, teal accent |
| AttachmentUpload.razor | 29 | Upload zone |
| CollectibleItemCard.razor | 92 | Card layout, placeholders |
| LazyImage.razor | 22 | Placeholder styling |
| DeleteItemConfirmationModal.razor | 13 | Minimal |
| DeleteUserConfirmationModal.razor | 13 | Minimal |
| ImagePreviewModal.razor | 240 | Dark overlay, animations |
| ParentSelectionModal.razor | 41 | Modal backdrop |
| PreviewImageSelectorModal.razor | 53 | Grid, focus states |
| QRScannerModal.razor | 64 | Scanner overlay |
| DynamicFieldRenderer.razor | 28 | Field styling |
| DynamicFieldsEditor.razor | 16 | Editor container |
| DynamicFieldsDisplay.razor | 34 | Display styling |
| MultiEntryEditor.razor | 28 | Multi-entry fields |

### Branch 2: `css-cleanup-pages` (15 files)

Main application pages.

| File | Style Lines | Notes |
|------|------------|-------|
| ShowcaseDetail.razor | 237 | Tags, grid, overlays |
| ShowcasesList.razor | 391 | Largest block, hero layout |
| PublicShowcases.razor | 188 | Grid, placeholders |
| CollectibleItemDetail.razor | 122 | Detail view, modals |
| AddCollectibleItem.razor | 51 | Form styling |
| EditCollectibleItem.razor | 76 | Form sections |
| EditShowcase.razor | 132 | Complex form |
| Welcome.razor | 291 | Mostly theme-aware already |
| AdminShowcases.razor | 186 | Grid, cards |
| ThemeSettings.razor | 66 | Theme config UI |
| Error.razor | 128 | Error page |
| ErrorPage.razor | 176 | Error messaging |
| NotFound.razor | 169 | 404 page |
| Unauthorized.razor | 67 | Auth error |
| UserStories.razor | 21 | Minimal |

### Branch 3: `css-cleanup-templates` (4 files)

Template management pages.

| File | Style Lines | Notes |
|------|------------|-------|
| CreateTemplate.razor | 219 | Identical to EditTemplate |
| EditTemplate.razor | 219 | Identical to CreateTemplate |
| TemplatesList.razor | 133 | Template card grid |
| TemplatedItemsList.razor | 84 | List styling |

CreateTemplate and EditTemplate share identical 219-line style blocks. Extract shared styles to a common CSS file referenced by both.

## Verification Strategy

For each branch:

1. Start the application locally (`dotnet run`)
2. Log in as test admin user (`test.user@collectibles.local`)
3. Take Playwright screenshots of affected pages in both light and dark mode before changes
4. Make the CSS changes
5. Take Playwright screenshots after changes
6. Compare before/after visually to confirm zero regression
7. Build passes (`dotnet build`)

## Out of Scope

- Adding new CSS custom properties or theme variables
- Changing any visual appearance
- Refactoring component markup beyond swapping in Bootstrap utility classes
- Touching files that don't have inline `<style>` blocks

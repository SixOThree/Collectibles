# CSS Cleanup: Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove redundant CSS from 15 page-level Razor components by eliminating Bootstrap duplicates, replacing inline styles with utility classes, and extracting remaining custom styles to `.razor.css` isolation files.

**Architecture:** Same approach as shared components plan — for each file, remove redundant CSS, add Bootstrap utility classes to markup, move remaining custom styles to `.razor.css`. Verify with Playwright screenshots.

**Tech Stack:** Blazor Server, Bootstrap 5, Playwright (via MCP)

**Branch:** `css-cleanup-pages` (create from `ReadyOK`)

---

### Task 0: Setup branch and baseline screenshots

**Files:**
- None modified

- [ ] **Step 1: Create branch**

```bash
git checkout ReadyOK && git checkout -b css-cleanup-pages
```

- [ ] **Step 2: Start the application**

```bash
dotnet run --project Source/Collectibles.Web
```

- [ ] **Step 3: Take baseline Playwright screenshots**

Log in as `test.user@collectibles.local` / `xA&%4hTVhTDixSOO`. Screenshot each page in both light and dark mode:
- `/` (Welcome)
- Showcases list page
- A showcase detail page
- A collectible item detail page
- Edit collectible item page
- Edit showcase page
- Add collectible item page
- Admin showcases page
- Theme settings page
- Public showcases page
- `/not-found-test` (NotFound)
- `/unauthorized` or trigger unauthorized state
- `/error` or trigger error state
- Management > User Stories

- [ ] **Step 4: Commit baseline**

```bash
git add -A && git commit -m "chore: baseline screenshots for pages CSS cleanup"
```

---

### Task 1: UserStories (smallest file, 4 rules)

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Management/UserStories.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Management/UserStories.razor.css`

- [ ] **Step 1: Move all custom styles to isolation file**

All 4 rules are custom display styling (`.story-content`, `.story-action`, `.story-entity`, `.story-time`). Move the `<style>` block to `.razor.css` as-is. Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract UserStories CSS to isolation file"
```

---

### Task 2: Unauthorized (small file)

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Unauthorized.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Unauthorized.razor.css`

- [ ] **Step 1: Replace utilities where possible, extract rest**

Replace on markup:
- Flexbox centering properties → `d-flex align-items-center justify-content-center` on container
- `text-align: center` → `text-center` on element
- `min-height: 100vh` → `min-vh-100` on element

Keep in `.razor.css`: the `@keyframes fadeIn` animation and `.error-container` animation reference.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract Unauthorized CSS to isolation file, use utilities"
```

---

### Task 3: AddCollectibleItem

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/AddCollectibleItem.razor`
- Create: `Source/Collectibles.Web/Components/Pages/AddCollectibleItem.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace on markup:
- `.required { color: #dc3545; }` → use `text-danger` class on elements
- `.form-actions { display: flex; gap: 1rem; justify-content: flex-end; }` → `d-flex gap-3 justify-content-end`

Keep in `.razor.css`: `.add-item-container` max-width, `.form-section` background/border/padding styling, responsive media queries.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract AddCollectibleItem CSS to isolation file, use utilities"
```

---

### Task 4: EditCollectibleItem

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/EditCollectibleItem.razor`
- Create: `Source/Collectibles.Web/Components/Pages/EditCollectibleItem.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace:
- `.required { color: #dc3545; }` → `text-danger` class
- `.page-header` flex → utilities on markup

Keep in `.razor.css`: `.form-section` styled sections, `.upload-section`, `.existing-attachments .card:hover`, responsive rules.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract EditCollectibleItem CSS to isolation file, use utilities"
```

---

### Task 5: EditShowcase

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/EditShowcase.razor`
- Create: `Source/Collectibles.Web/Components/Pages/EditShowcase.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace:
- `.required { color: #dc3545; }` → `text-danger`
- `.page-header` flex → utilities

Keep in `.razor.css`: `.form-section`, `.management-grid`, `.management-card`, `.danger-zone`, `.management-icon`, hover effects, transitions, responsive rules.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract EditShowcase CSS to isolation file, use utilities"
```

---

### Task 6: ThemeSettings

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Admin/ThemeSettings.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Admin/ThemeSettings.razor.css`

- [ ] **Step 1: Replace utilities where possible, extract rest**

Replace:
- `.theme-option` border/padding → `border rounded p-3` on elements

Keep in `.razor.css`: `.theme-option` selected state, `.background-option`, `.background-preview`, custom focus/selected styling for radio buttons.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract ThemeSettings CSS to isolation file"
```

---

### Task 7: Error + ErrorPage + NotFound

These three error pages share similar patterns (centered layout, animations, icon styling). Handle together.

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Error.razor`
- Modify: `Source/Collectibles.Web/Components/Pages/ErrorPage.razor`
- Modify: `Source/Collectibles.Web/Components/Pages/NotFound.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Error.razor.css`
- Create: `Source/Collectibles.Web/Components/Pages/ErrorPage.razor.css`
- Create: `Source/Collectibles.Web/Components/Pages/NotFound.razor.css`

- [ ] **Step 1: Replace utilities on all three**

Replace on markup for all:
- Centering containers → `d-flex align-items-center justify-content-center min-vh-100`
- Text centering → `text-center`
- Text colors like `color: #666` → `text-muted`

- [ ] **Step 2: Extract custom styles to isolation files**

Each file has unique animations and icon styling. Move remaining custom CSS to respective `.razor.css` files. These include `@keyframes fadeIn`, `@keyframes pulse`, `@keyframes bounce`, `@keyframes swing`, `.error-icon`, `.quick-link`, `.magnifying-glass`, etc.

Remove `<style>` blocks.

- [ ] **Step 3: Build and verify**

- [ ] **Step 4: Playwright verify** - trigger each error page, screenshot light+dark.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "refactor: extract error page CSS to isolation files, use utilities"
```

---

### Task 8: CollectibleItemDetail

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/CollectibleItemDetail.razor`
- Create: `Source/Collectibles.Web/Components/Pages/CollectibleItemDetail.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace:
- `.document-item` padding/border → `p-3 border rounded` on elements
- `.document-preview` flexbox centering → `d-flex align-items-center justify-content-center`

Keep in `.razor.css`: `.document-item:hover` effects, `.document-preview` dimensions, `.document-list` column layout, `.preview-loading-overlay` animation, responsive rules.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract CollectibleItemDetail CSS to isolation file"
```

---

### Task 9: AdminShowcases

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Admin/AdminShowcases.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Admin/AdminShowcases.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace:
- `.page-header` flex → utilities on markup
- `.empty-state-icon { font-size: 4rem; }` → `display-4` or keep custom

Keep in `.razor.css`: showcase card components, grid layout, hover animations, placeholder styling, responsive rules.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract AdminShowcases CSS to isolation file"
```

---

### Task 10: PublicShowcases

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/PublicShowcases.razor`
- Create: `Source/Collectibles.Web/Components/Pages/PublicShowcases.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace:
- `.page-header` flex → utilities
- `.empty-state` padding/centering → `p-5 text-center`

Keep in `.razor.css`: showcase card components, grid layout, image containers, text clamping, tag badges, hover effects, prefers-reduced-motion, responsive rules.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract PublicShowcases CSS to isolation file"
```

---

### Task 11: ShowcaseDetail

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/ShowcaseDetail.razor`
- Create: `Source/Collectibles.Web/Components/Pages/ShowcaseDetail.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace:
- `.showcase-description` color → `text-muted`
- `.showcase-meta` flex → `d-flex gap-3 flex-wrap`
- `.tag-list` flex → `d-flex flex-wrap gap-2`
- `.empty-items` centering → `text-center text-muted`

Keep in `.razor.css`: tag button styling, search box with icon, navigation loading overlay, fadeIn/pulse animations, modal styling, info section layout, items grid, responsive rules.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract ShowcaseDetail CSS to isolation file, use utilities"
```

---

### Task 12: ShowcasesList

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/ShowcasesList.razor`
- Create: `Source/Collectibles.Web/Components/Pages/ShowcasesList.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace:
- `.page-header` flex → utilities
- `.empty-state-icon` color → `text-muted`
- `.feature-card p` color → `text-muted`

Keep in `.razor.css`: This is the largest style block (391 lines) with extensive custom styling — hero card, showcase card, feature cards, getting-started gradient, pro-tips section, card description line-clamp, hover transforms, responsive breakpoints. Nearly all stays.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract ShowcasesList CSS to isolation file"
```

---

### Task 13: Welcome

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Welcome.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Welcome.razor.css`

- [ ] **Step 1: Replace utilities where possible, extract rest**

This page already uses CSS variables well. Replace:
- `.welcome-container` padding → utilities where applicable
- `.welcome-header h1` basic heading → let Bootstrap handle

Keep in `.razor.css`: All custom card styling, single-showcase layout, getting-started gradient, user-showcase-card, feature cards, responsive breakpoints. This file has 291 lines of mostly custom CSS.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract Welcome CSS to isolation file"
```

---

### Task 14: Final verification

- [ ] **Step 1: Full build**

```bash
dotnet build
```

- [ ] **Step 2: Take final Playwright screenshots** of all pages in light and dark mode.

- [ ] **Step 3: Compare with baseline** - verify zero visual regression.

- [ ] **Step 4: Push branch**

```bash
git push -u origin css-cleanup-pages
```

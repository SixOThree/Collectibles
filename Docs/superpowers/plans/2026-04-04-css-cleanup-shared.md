# CSS Cleanup: Shared Components Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove redundant CSS from 16 shared Razor components by eliminating Bootstrap duplicates, replacing inline styles with utility classes, and extracting remaining custom styles to `.razor.css` isolation files.

**Architecture:** For each file, remove redundant/utility-replaceable CSS from the inline `<style>` block, add corresponding Bootstrap utility classes to the markup, and move remaining custom styles to a co-located `.razor.css` file. Verify with Playwright screenshots before/after.

**Tech Stack:** Blazor Server, Bootstrap 5, Playwright (via MCP)

**Branch:** `css-cleanup-shared` (create from `ReadyOK`)

---

### Task 0: Setup branch and baseline screenshots

**Files:**
- None modified

- [ ] **Step 1: Create branch**

```bash
git checkout -b css-cleanup-shared
```

- [ ] **Step 2: Start the application**

```bash
dotnet run --project Source/Collectibles.Web
```

- [ ] **Step 3: Take baseline Playwright screenshots**

Log in as `test.user@collectibles.local` / `xA&%4hTVhTDixSOO`. Navigate to pages that exercise the shared components:
- A showcase detail page (exercises CollectibleItemCard, LazyImage, ImagePreviewModal, AttachmentThumbnailView)
- The edit collectible item page (exercises AttachmentUpload, AttachmentDetailView, ParentSelectionModal, PreviewImageSelectorModal, DynamicFieldsEditor, DynamicFieldRenderer, MultiEntryEditor)
- A collectible item detail page (exercises AttachmentDetailModal, DynamicFieldsDisplay)

Take screenshots in both light and dark mode. Save with descriptive filenames.

- [ ] **Step 4: Commit baseline**

```bash
git add -A && git commit -m "chore: baseline screenshots for CSS cleanup"
```

---

### Task 1: DeleteItemConfirmationModal + DeleteUserConfirmationModal

These two files have identical 3-rule style blocks. All rules are redundant or utility-replaceable.

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/DeleteItemConfirmationModal.razor`
- Modify: `Source/Collectibles.Web/Components/Shared/DeleteUserConfirmationModal.razor`
- Create: `Source/Collectibles.Web/Components/Shared/DeleteItemConfirmationModal.razor.css`
- Create: `Source/Collectibles.Web/Components/Shared/DeleteUserConfirmationModal.razor.css`

- [ ] **Step 1: Remove redundant rules from both files**

In both files, the `<style>` block contains:
```css
.modal.show { display: block; }              /* Redundant - Bootstrap handles .show */
.modal-header.bg-danger { background-color: #dc3545 !important; } /* Redundant - Bootstrap bg-danger already does this */
.btn-close-white { filter: invert(1) grayscale(100%) brightness(200%); } /* Custom - keep */
```

Remove the entire `<style>` block from both files. Create `.razor.css` files for each with only the custom rule:

**DeleteItemConfirmationModal.razor.css:**
```css
.btn-close-white {
    filter: invert(1) grayscale(100%) brightness(200%);
}
```

**DeleteUserConfirmationModal.razor.css:**
```css
.btn-close-white {
    filter: invert(1) grayscale(100%) brightness(200%);
}
```

- [ ] **Step 2: Build and verify**

```bash
dotnet build
```

- [ ] **Step 3: Playwright verify** - Navigate to a page with a delete confirmation, screenshot in light+dark mode, compare with baseline.

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract delete modal CSS to isolation files, remove Bootstrap duplicates"
```

---

### Task 2: DynamicFieldsEditor

All CSS in this file can be replaced with Bootstrap utility classes.

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/Templates/DynamicFieldsEditor.razor`

- [ ] **Step 1: Replace style block with utility classes**

Current CSS:
```css
.dynamic-fields-editor { border: 1px solid #dee2e6; border-radius: 8px; padding: 1.5rem; background: #f8f9fa; margin-bottom: 1rem; }
.dynamic-fields-editor h4 { margin-bottom: 1.5rem; color: #495057; border-bottom: 1px solid #dee2e6; padding-bottom: 0.5rem; }
```

Remove the entire `<style>` block. Update the markup:
- On the `.dynamic-fields-editor` div: add classes `border rounded p-4 bg-light mb-3`
- On the `h4` inside it: add classes `mb-4 text-secondary border-bottom pb-2`

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: replace DynamicFieldsEditor inline CSS with Bootstrap utilities"
```

---

### Task 3: DynamicFieldsDisplay

Most CSS replaceable with utilities; keep only the custom rules.

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/Templates/DynamicFieldsDisplay.razor`
- Create: `Source/Collectibles.Web/Components/Shared/Templates/DynamicFieldsDisplay.razor.css`

- [ ] **Step 1: Extract custom styles, replace rest with utilities**

Current CSS has 6 rule sets. Replace with utilities where possible:
- `.template-fields { background-color: var(--bs-gray-100); border-radius: 0.375rem; padding: 1rem; }` → add `bg-light rounded p-3` to element
- `.field-display { padding-bottom: 0.5rem; border-bottom: 1px solid var(--bs-gray-300); }` → add `pb-2 border-bottom` to element
- `.field-label { margin-bottom: 0.25rem; }` → add `mb-1` to element

Keep in `.razor.css`:
```css
.field-label {
    font-weight: 600;
    font-size: 0.875rem;
    text-transform: uppercase;
    letter-spacing: 0.025em;
}

.field-display:last-child {
    border-bottom: none;
    padding-bottom: 0;
}

.preserve-whitespace {
    white-space: pre-wrap;
}
```

Remove the `<style>` block entirely.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract DynamicFieldsDisplay CSS to isolation file, use utilities"
```

---

### Task 4: MultiEntryEditor

Most CSS replaceable with utilities.

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/Templates/MultiEntryEditor.razor`
- Create: `Source/Collectibles.Web/Components/Shared/Templates/MultiEntryEditor.razor.css`

- [ ] **Step 1: Extract custom styles, replace rest with utilities**

Replace with utilities on the `.multi-entry-editor` div: `border rounded p-4 bg-light mb-3`
Replace on `h4`: add `text-body border-bottom pb-2`

Keep in `.razor.css`:
```css
.multi-entry-editor .table td {
    padding: 0.25rem 0.5rem;
}

.multi-entry-editor .form-control-sm,
.multi-entry-editor .form-select-sm {
    font-size: 0.85rem;
}
```

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract MultiEntryEditor CSS to isolation file, use utilities"
```

---

### Task 5: DynamicFieldRenderer

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/Templates/DynamicFieldRenderer.razor`
- Create: `Source/Collectibles.Web/Components/Shared/Templates/DynamicFieldRenderer.razor.css`

- [ ] **Step 1: Replace utility-equivalent rules, keep custom**

Replace:
- `.dynamic-field .required { color: #dc3545; }` → use `text-danger` class on the element
- `.dynamic-field .form-check { padding-top: 0.375rem; }` → add `pt-1` to element

Keep in `.razor.css`:
```css
.inflation-adjusted-price .adjusted-value {
    font-size: 1.1rem;
    font-weight: 600;
    color: #28a745;
}

.inflation-adjusted-price .btn-link {
    color: #6c757d;
    text-decoration: none;
}

.inflation-adjusted-price .btn-link:hover {
    color: #495057;
}

.dynamic-field .invalid-feedback {
    font-size: 0.875rem;
}
```

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract DynamicFieldRenderer CSS to isolation file, use utilities"
```

---

### Task 6: LazyImage

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/LazyImage.razor`
- Create: `Source/Collectibles.Web/Components/Shared/LazyImage.razor.css`

- [ ] **Step 1: Replace utilities, keep custom**

Replace on `.image-placeholder` and `.image-error` elements: add `d-flex flex-column align-items-center justify-content-center bg-light border rounded-1`

Keep in `.razor.css`:
```css
.image-placeholder,
.image-error {
    min-height: 200px;
}

img.loading {
    opacity: 0;
}

img.error {
    display: none;
}
```

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract LazyImage CSS to isolation file, use utilities"
```

---

### Task 7: AttachmentUpload

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/AttachmentUpload.razor`
- Create: `Source/Collectibles.Web/Components/Shared/AttachmentUpload.razor.css`

- [ ] **Step 1: Replace utilities, keep custom**

Replace:
- `.upload-container { margin-top: 2rem; }` → add `mt-5` to element

Keep in `.razor.css`:
```css
.drop-zone {
    border: 3px dashed #ccc;
    border-radius: 1rem;
    padding: 3rem;
    background-color: #f8f9fa;
    transition: border-color 0.3s ease, background-color 0.3s ease, transform 0.3s ease;
    cursor: pointer;
}

.drop-zone:hover {
    border-color: #0d6efd;
    background-color: #e7f1ff;
}

.drop-zone.drag-over {
    border-color: #0d6efd;
    background-color: #cfe2ff;
    transform: scale(1.02);
}

.progress {
    min-width: 100px;
}
```

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract AttachmentUpload CSS to isolation file, use utilities"
```

---

### Task 8: AttachmentDetailModal

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/AttachmentDetailModal.razor`
- Create: `Source/Collectibles.Web/Components/Shared/AttachmentDetailModal.razor.css`

- [ ] **Step 1: Remove redundant, replace utilities, keep custom**

Remove: `.modal-xl { max-width: 90%; }` — Bootstrap already provides `modal-xl`
Replace: `.tags-section { margin-top: 0.5rem; }` → add `mt-2` to element

Keep in `.razor.css`:
```css
.attachment-preview-large {
    min-height: 400px;
    display: flex;
    align-items: center;
    justify-content: center;
    background-color: #f8f9fa;
    border-radius: 0.25rem;
    overflow: hidden;
}

.attachment-preview-large img {
    max-width: 100%;
    max-height: 600px;
    object-fit: contain;
}

.placeholder-icon-large {
    font-size: 8rem;
    color: #dee2e6;
}

dl.row dd {
    margin-bottom: 0.5rem;
}

.image-controls {
    padding: 10px;
    background-color: rgba(0, 0, 0, 0.05);
    border-radius: 0.25rem;
}

.image-controls .btn {
    min-width: 120px;
}
```

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract AttachmentDetailModal CSS to isolation file"
```

---

### Task 9: AttachmentDetailView

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/AttachmentDetailView.razor`
- Create: `Source/Collectibles.Web/Components/Shared/AttachmentDetailView.razor.css`

- [ ] **Step 1: Move all custom styles to isolation file**

All styles in this file are custom (hover effects, preview sizing). Move the entire style block to `.razor.css` as-is, then remove the `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract AttachmentDetailView CSS to isolation file"
```

---

### Task 10: AttachmentThumbnailView

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/AttachmentThumbnailView.razor`
- Create: `Source/Collectibles.Web/Components/Shared/AttachmentThumbnailView.razor.css`

- [ ] **Step 1: Move all custom styles to isolation file**

Nearly all styles are custom (aspect-ratio, feature button with opacity transitions, featured attachment highlight). Move entire style block to `.razor.css`, remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract AttachmentThumbnailView CSS to isolation file"
```

---

### Task 11: CollectibleItemCard

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/CollectibleItemCard.razor`
- Create: `Source/Collectibles.Web/Components/Shared/CollectibleItemCard.razor.css`

- [ ] **Step 1: Replace utility-equivalent rules, extract rest**

Replace on markup:
- `.item-card { display: flex; flex-direction: column; }` → already achievable but risky to change card layout; keep as custom
- `.item-content { padding: 1rem; }` → add `p-3` to element

Move remaining rules to `.razor.css`. These are all custom card component styles (hover transforms, line-clamp, placeholder styling, tag badges).

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract CollectibleItemCard CSS to isolation file"
```

---

### Task 12: ParentSelectionModal

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/ParentSelectionModal.razor`
- Create: `Source/Collectibles.Web/Components/Shared/ParentSelectionModal.razor.css`

- [ ] **Step 1: Remove Bootstrap-redundant modal rules, keep custom**

Remove rules that duplicate Bootstrap modal defaults:
- `.modal-content { background-color: #fff; border-radius: 0.5rem; box-shadow: ... }` — Bootstrap provides these
- `.list-group-item.active { z-index: 2; color: #fff; background-color: #0d6efd; border-color: #0d6efd; }` — Bootstrap `.active` default

Keep in `.razor.css`:
```css
.modal-backdrop {
    position: fixed;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.5);
    z-index: 1050;
    display: flex;
    align-items: center;
    justify-content: center;
}

.modal-dialog {
    max-width: 600px;
    margin: 1.75rem;
}

.list-group-item {
    cursor: pointer;
    transition: background-color 0.2s ease;
}

.list-group-item:hover:not(.active) {
    background-color: #f8f9fa;
}
```

Note: Check if this modal uses Bootstrap's JS modal or a custom implementation. If custom, the backdrop/dialog rules are needed.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract ParentSelectionModal CSS, remove Bootstrap duplicates"
```

---

### Task 13: PreviewImageSelectorModal

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/PreviewImageSelectorModal.razor`
- Create: `Source/Collectibles.Web/Components/Shared/PreviewImageSelectorModal.razor.css`

- [ ] **Step 1: Remove Bootstrap-redundant modal rules, replace utilities, keep custom**

Remove if modal header/footer/body rules duplicate Bootstrap defaults. Replace `.section-title { margin-bottom: 0.5rem; }` → add `mb-2` on element.

Keep in `.razor.css`: modal backdrop (if custom modal), `.card.selectable` interactive states.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract PreviewImageSelectorModal CSS, remove duplicates"
```

---

### Task 14: QRScannerModal

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/QRScannerModal.razor`
- Create: `Source/Collectibles.Web/Components/Shared/QRScannerModal.razor.css`

- [ ] **Step 1: Replace utilities, extract rest**

Replace: `.scanner-container { margin: 0 auto; }` → add `mx-auto` to element

Move all remaining rules (scanner frame, corner decorations, scan animation) to `.razor.css`. These are entirely custom.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract QRScannerModal CSS to isolation file"
```

---

### Task 15: ImagePreviewModal

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/ImagePreviewModal.razor`
- Create: `Source/Collectibles.Web/Components/Shared/ImagePreviewModal.razor.css`

- [ ] **Step 1: Move entire style block to isolation file**

This component has 240 lines of entirely custom CSS (dark theme modal, zoom/pan, animations, responsive breakpoints). No rules are redundant with Bootstrap. Move the entire `<style>` block to `.razor.css` as-is.

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract ImagePreviewModal CSS to isolation file"
```

---

### Task 16: Final verification and cleanup

- [ ] **Step 1: Full build**

```bash
dotnet build
```

- [ ] **Step 2: Take final Playwright screenshots** of all pages exercised in Task 0, in both light and dark mode.

- [ ] **Step 3: Compare with baseline screenshots** - verify zero visual regression.

- [ ] **Step 4: Push branch**

```bash
git push -u origin css-cleanup-shared
```

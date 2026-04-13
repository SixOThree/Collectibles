# CSS Cleanup: Templates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove redundant CSS from 4 template page Razor components, consolidate the duplicate CreateTemplate/EditTemplate style blocks into a shared CSS file, and extract remaining custom styles to `.razor.css` isolation files.

**Architecture:** CreateTemplate and EditTemplate share an identical 219-line style block (only the container class name differs). Extract shared styles to a common CSS file. For TemplatesList and TemplatedItemsList, remove redundant CSS, use utilities, extract custom styles to `.razor.css`.

**Tech Stack:** Blazor Server, Bootstrap 5, Playwright (via MCP)

**Branch:** `css-cleanup-templates` (create from `ReadyOK`)

---

### Task 0: Setup branch and baseline screenshots

**Files:**
- None modified

- [ ] **Step 1: Create branch**

```bash
git checkout ReadyOK && git checkout -b css-cleanup-templates
```

- [ ] **Step 2: Start the application**

```bash
dotnet run --project Source/Collectibles.Web
```

- [ ] **Step 3: Take baseline Playwright screenshots**

Log in as `test.user@collectibles.local` / `xA&%4hTVhTDixSOO`. Screenshot each page in both light and dark mode:
- Templates list page
- Create template page
- Edit template page (edit an existing template)
- Templated items list page (navigate via a template)

- [ ] **Step 4: Commit baseline**

```bash
git add -A && git commit -m "chore: baseline screenshots for templates CSS cleanup"
```

---

### Task 1: Extract shared CreateTemplate/EditTemplate CSS

The two files have identical style blocks except `.create-template-container` vs `.edit-template-container`. Extract to a shared CSS file.

**Files:**
- Create: `Source/Collectibles.Web/wwwroot/css/template-form.css`
- Modify: `Source/Collectibles.Web/Components/Pages/Templates/CreateTemplate.razor`
- Modify: `Source/Collectibles.Web/Components/Pages/Templates/EditTemplate.razor`

- [ ] **Step 1: Create shared CSS file**

Create `template-form.css` with the shared rules. Use a common class `.template-form-container` instead of the page-specific container names. Include only the custom rules that should stay — remove redundant/utility-replaceable ones:

Remove from shared file (replace with utilities on markup):
- `.required { color: #dc3545; }` → use `text-danger` class
- `.page-header` flex properties → `d-flex justify-content-between align-items-center mb-4 border-bottom pb-3`
- `.form-actions` flex → `d-flex gap-3 justify-content-end pt-4 border-top`
- `.section-header` flex → `d-flex justify-content-between align-items-center mb-4`
- `.basic-info-section h3`, `.fields-section h3` color → `text-secondary`

Keep in shared file:
```css
.template-form-container {
    max-width: 1000px;
    margin: 0 auto;
    padding: 2rem;
}

.template-form {
    background: white;
    border: 1px solid #dee2e6;
    border-radius: 8px;
    padding: 2rem;
    margin-bottom: 2rem;
}

.basic-info-section {
    margin-bottom: 2rem;
    padding-bottom: 2rem;
    border-bottom: 1px solid #e9ecef;
}

.field-editor {
    border: 1px solid #dee2e6;
    border-radius: 6px;
    margin-bottom: 1rem;
    background: #f8f9fa;
}

.field-editor.expanded {
    background: white;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.field-header {
    padding: 1rem;
    cursor: pointer;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-radius: 6px;
}

.field-header:hover {
    background: rgba(0,0,0,0.02);
}

.field-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

.field-name {
    font-weight: 500;
    color: #495057;
}

.field-actions {
    display: flex;
    align-items: center;
    gap: 0.25rem;
}

.field-details {
    padding: 0 1rem 1rem 1rem;
    border-top: 1px solid #e9ecef;
}

.no-fields {
    text-align: center;
    padding: 2rem;
    border: 2px dashed #dee2e6;
    border-radius: 6px;
}

.dropdown-option-item {
    display: flex;
    gap: 0.5rem;
    margin-bottom: 0.5rem;
}

.dropdown-option-item input {
    flex: 1;
}

.icon-grid {
    display: flex;
    flex-wrap: wrap;
    gap: 0.375rem;
}

.icon-option {
    width: 36px;
    height: 36px;
    display: flex;
    align-items: center;
    justify-content: center;
    border: 2px solid #dee2e6;
    border-radius: 6px;
    background: white;
    cursor: pointer;
    font-size: 1.1rem;
    color: #495057;
    transition: border-color 0.15s ease, background-color 0.15s ease, color 0.15s ease;
    padding: 0;
}

.icon-option:hover {
    border-color: #6c757d;
    background: #f8f9fa;
}

.icon-option.selected {
    border-color: #0d6efd;
    background: #e7f1ff;
    color: #0d6efd;
}

.card-preview {
    border-radius: 8px;
    padding: 1rem;
    background: white;
    max-width: 200px;
}

.card-preview-content {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    color: #6b7280;
    font-size: 0.875rem;
}

.card-preview-content i {
    font-size: 1rem;
}

@media (max-width: 768px) {
    .template-form-container {
        padding: 1rem;
    }

    .template-form {
        padding: 1rem;
    }

    .field-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
    }

    .field-actions {
        align-self: flex-end;
    }
}
```

- [ ] **Step 2: Update CreateTemplate.razor**

Remove the entire `<style>` block. Replace `.create-template-container` with `.template-form-container` in the markup. Add a CSS import at the top of the file or use a `<link>` element:

```html
<link rel="stylesheet" href="css/template-form.css" />
```

Note: The shared CSS file should be placed at `Source/Collectibles.Web/wwwroot/css/template-form.css` so it is served as a static file.

Update markup elements with Bootstrap utility classes where styles were removed (`.page-header` → add `d-flex justify-content-between align-items-center mb-4 border-bottom pb-3`, `.required` → `text-danger`, etc.)

- [ ] **Step 3: Update EditTemplate.razor**

Same changes as CreateTemplate — remove `<style>` block, replace `.edit-template-container` with `.template-form-container`, add the CSS link, update markup with utility classes.

- [ ] **Step 4: Build and verify**

```bash
dotnet build
```

- [ ] **Step 5: Playwright verify** - Screenshot create and edit template pages in light+dark mode.

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "refactor: consolidate CreateTemplate/EditTemplate CSS into shared file"
```

---

### Task 2: TemplatesList

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Templates/TemplatesList.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Templates/TemplatesList.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace on markup:
- `.page-header` flex properties → `d-flex justify-content-between align-items-center mb-4 border-bottom pb-3`
- `.template-description { color: #6c757d; }` → `text-muted` on element
- `.template-name { color: #495057; }` → `text-secondary`
- `.empty-state` centering → `text-center p-5`
- `.empty-state h3 { color: #495057; }` → `text-secondary`
- `.template-stats { background: #f8f9fa; }` → `bg-light`
- `.stat { color: #495057; }` → `text-secondary`
- `.templates-container { margin: 0 auto; }` → `mx-auto`

Keep in `.razor.css`:
```css
.templates-container {
    max-width: 1200px;
    padding: 2rem;
}

.templates-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
    gap: 1.5rem;
}

.template-card {
    border: 1px solid #dee2e6;
    border-radius: 8px;
    padding: 1.5rem;
    background: white;
    transition: box-shadow 0.3s ease, transform 0.3s ease;
    box-shadow: 0 2px 4px rgba(0,0,0,0.1);
}

.template-card:hover {
    box-shadow: 0 4px 8px rgba(0,0,0,0.15);
    transform: translateY(-2px);
}

.template-card.inactive {
    opacity: 0.7;
    border-color: #ced4da;
}

.template-description {
    margin-bottom: 1rem;
    font-size: 0.9rem;
    line-height: 1.4;
}

.template-stats {
    display: flex;
    gap: 1rem;
    margin-bottom: 1rem;
    padding: 0.75rem;
    border-radius: 4px;
}

.stat {
    display: flex;
    align-items: center;
    gap: 0.25rem;
    font-size: 0.875rem;
}

.template-dates {
    margin-bottom: 1rem;
    padding-top: 0.5rem;
    border-top: 1px solid #e9ecef;
}

.empty-state-icon {
    font-size: 4rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

.empty-state p {
    max-width: 400px;
    margin-left: auto;
    margin-right: auto;
}

@media (max-width: 768px) {
    .templates-container {
        padding: 1rem;
    }

    .templates-grid {
        grid-template-columns: 1fr;
    }
}
```

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract TemplatesList CSS to isolation file, use utilities"
```

---

### Task 3: TemplatedItemsList

**Files:**
- Modify: `Source/Collectibles.Web/Components/Pages/Templates/TemplatedItemsList.razor`
- Create: `Source/Collectibles.Web/Components/Pages/Templates/TemplatedItemsList.razor.css`

- [ ] **Step 1: Replace utilities, extract custom**

Replace on markup:
- `.page-header` flex → `d-flex justify-content-between align-items-center mb-4 border-bottom pb-3 gap-3`
- `.empty-state` centering → `text-center p-5`
- `.empty-state h3 { color: #495057; }` → `text-secondary`
- `.empty-state-icon { font-size: 4rem; }` → keep custom but can use `display-4`

Keep in `.razor.css`:
```css
.templated-items-container {
    max-width: 100%;
    margin: 0 auto;
    padding: 2rem;
}

.sortable-header {
    cursor: pointer;
    user-select: none;
    white-space: nowrap;
}

.sortable-header:hover {
    background-color: rgba(0, 0, 0, 0.05);
}

.item-row {
    cursor: pointer;
}

.item-row:hover {
    background-color: rgba(var(--bs-primary-rgb), 0.05);
}

.item-row td {
    vertical-align: middle;
    max-width: 250px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.empty-state-icon {
    font-size: 4rem;
    margin-bottom: 1rem;
    opacity: 0.5;
}

.empty-state p {
    max-width: 400px;
    margin-left: auto;
    margin-right: auto;
}

@media (max-width: 768px) {
    .templated-items-container {
        padding: 1rem;
    }
}
```

Remove `<style>` block.

- [ ] **Step 2: Build and verify**

- [ ] **Step 3: Playwright verify**

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "refactor: extract TemplatedItemsList CSS to isolation file, use utilities"
```

---

### Task 4: Final verification

- [ ] **Step 1: Full build**

```bash
dotnet build
```

- [ ] **Step 2: Take final Playwright screenshots** of all template pages in light and dark mode.

- [ ] **Step 3: Compare with baseline** - verify zero visual regression.

- [ ] **Step 4: Push branch**

```bash
git push -u origin css-cleanup-templates
```

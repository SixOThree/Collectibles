# Playwright E2E Milestone 7 Attachment Tags Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend Milestone 7 tag coverage to the attachment surfaces by validating tag creation/editing from the attachment detail modal and persistence on the dedicated attachment detail page.

**Architecture:** Reuse the authenticated attachments flow to upload a deterministic document, open its attachment detail modal from `/attachments`, update tags through the shared `TagSelector`, and then verify the saved tag on `/attachments/{hash}`. Fix the attachment tag editors to bind through the same `SelectedTags` API that `TagSelector` actually supports.

**Tech Stack:** Playwright Test, TypeScript, ASP.NET Core Blazor Server, Playwright LocalDB configuration from `appsettings.Playwright.json`

---

## File Structure

- Modify: `Test/Playwright/tests/attachments/attachments-list.spec.ts`
- Modify: `Test/Playwright/tests/helpers/attachments.ts`
- Modify: `Source/Collectibles.Web/Components/Shared/AttachmentDetailModal.razor`
- Modify: `Source/Collectibles.Web/Components/Pages/AttachmentDetail.razor`
- Modify: `agent_docs/claude/playwright-testing.md`

### Task 1: Add Failing Attachment Tag Coverage

**Files:**
- Modify: `Test/Playwright/tests/attachments/attachments-list.spec.ts`

- [ ] **Step 1: Add a failing attachment-tag spec**

Create a test that:

- uploads a document attachment
- opens the attachment detail modal from `/attachments`
- opens the tag editor
- creates a unique tag
- saves the tag
- verifies the tag persists on the dedicated attachment detail page

- [ ] **Step 2: Run the targeted attachments spec and verify the new test fails**

Run:

```powershell
npx playwright test --project=chromium tests/attachments/attachments-list.spec.ts --grep "tag an attachment"
```

Expected: FAIL until the attachment tag editor and selector interactions match the real UI.

### Task 2: Fix The Attachment Tag Editor Integration

**Files:**
- Modify: `Source/Collectibles.Web/Components/Shared/AttachmentDetailModal.razor`
- Modify: `Source/Collectibles.Web/Components/Pages/AttachmentDetail.razor`

- [ ] **Step 1: Replace unsupported `InitialSelectedTags` usage**

Wire both attachment tag editors to:

- initialize a local editable tag list when the modal opens
- pass that list through `SelectedTags`
- save through `SetTagsForAttachmentCommand` using the edited list

- [ ] **Step 2: Re-run the targeted spec and verify the tag flow passes**

Run:

```powershell
npx playwright test --project=chromium tests/attachments/attachments-list.spec.ts --grep "tag an attachment"
```

Expected: PASS for attachment tag create/edit/display coverage.

### Task 3: Document The Milestone 7 Tag Slice

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`

- [ ] **Step 1: Update the Playwright guide**

Document:

- attachment-tag editing coverage from the detail modal
- persistence checks on the attachment detail page
- the targeted `attachments-list.spec.ts` command used for debugging this slice


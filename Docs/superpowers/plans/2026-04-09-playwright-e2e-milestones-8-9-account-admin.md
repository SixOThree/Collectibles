# Playwright E2E Milestones 8 And 9 Account And Admin Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Playwright coverage for in-app account-management flows plus the remaining admin and operational surfaces so the roadmap reaches Milestone 9.

**Architecture:** Keep this branch as one combined Milestone 8 and 9 pass, but execute it in two checkpoints. For Milestone 8, avoid mutating the shared seeded regular-user account by creating a unique throwaway user through the real admin user-management UI, then log in as that user to drive profile, password, personal-data, logout, and relogin flows. For Milestone 9, reuse the seeded admin state and drive the real `/users`, `/management`, and `/admin/*` pages end to end with stable heading-, form-label-, and table-based assertions rather than brittle CSS-only selectors.

**Tech Stack:** Playwright Test, TypeScript, ASP.NET Core Blazor Server, existing auth storage state, admin-created throwaway users, Playwright download assertions

---

## File Structure

- Create: `Test/Playwright/tests/account/account-management.spec.ts`
- Create: `Test/Playwright/tests/admin/admin-operations.spec.ts`
- Create: `Test/Playwright/tests/helpers/users.ts`
- Modify: `Test/Playwright/tests/helpers/auth.ts`
- Modify: `agent_docs/claude/playwright-testing.md`
- Modify: `Docs/DEVELOPER_README.md`
- Create: `Docs/superpowers/plans/2026-04-09-playwright-e2e-milestones-8-9-account-admin.md`

### Task 1: Add Milestone 8 Account Management Coverage

**Files:**
- Create: `Test/Playwright/tests/account/account-management.spec.ts`
- Create: `Test/Playwright/tests/helpers/users.ts`
- Modify: `Test/Playwright/tests/helpers/auth.ts`

- [ ] **Step 1: Write a failing account-management spec**

Create `Test/Playwright/tests/account/account-management.spec.ts` with a test that:

- signs in as the seeded admin
- creates a unique confirmed user through `/users/new`
- logs out
- logs in as that newly created user
- updates the profile display name on `/Account/Manage`
- changes the password on `/Account/Manage/ChangePassword`
- logs out and confirms the old password no longer works
- logs back in with the new password
- downloads personal data from `/Account/Manage/PersonalData`
- opens the 2FA page and verifies the expected empty-state controls
- deletes the throwaway account through `/Account/Manage/DeletePersonalData`
- confirms the deleted user can no longer log in

- [ ] **Step 2: Run the new account spec and verify it fails**

Run from `Test/Playwright`:

```powershell
npx playwright test --project=chromium tests/account/account-management.spec.ts
```

Expected: FAIL because the new account helper/spec do not exist yet.

- [ ] **Step 3: Implement stable account and auth helpers**

In `tests/helpers/users.ts`, add helpers that:

- create a unique user from `/users/new`
- log in and log out through the real account pages
- assert the standard authenticated landing page

In `tests/helpers/auth.ts`, extend the existing auth helpers with reusable manual-login and logout helpers without disturbing the storage-state helpers already used by the broader suite.

- [ ] **Step 4: Make the account-management flow pass**

Keep the assertions user-facing and stable:

- `Profile`
- `Your profile has been updated`
- `Change password`
- `Your password has been changed`
- invalid login after the old password is retired
- successful relogin with the new password
- downloaded personal-data payload contains the test email
- `Two-factor authentication (2FA)` empty-state controls
- successful self-delete and failed login afterward

- [ ] **Step 5: Re-run the account spec and verify it passes**

Run:

```powershell
npx playwright test --project=chromium tests/account/account-management.spec.ts
```

Expected: PASS for the Milestone 8 slice.

### Task 2: Add Milestone 9 User Management Coverage

**Files:**
- Create: `Test/Playwright/tests/admin/admin-operations.spec.ts`
- Modify: `Test/Playwright/tests/helpers/users.ts`

- [ ] **Step 1: Add a failing user-management test**

Extend `Test/Playwright/tests/admin/admin-operations.spec.ts` with a test that:

- signs in as the seeded admin
- creates a unique user through `/users/new`
- verifies the new user appears in `/users`
- opens `/users/{id}` and checks the detail page
- edits the user through `/users/{id}/edit`
- changes the name or active status and verifies the update persists
- uses the detail page reset-password modal to assign a replacement password
- optionally locks and unlocks the user if the controls remain deterministic
- deletes the same user and verifies the list no longer shows it

- [ ] **Step 2: Run the admin spec and verify the user-management slice fails**

Run:

```powershell
npx playwright test --project=chromium tests/admin/admin-operations.spec.ts
```

Expected: FAIL until the selectors and helper plumbing match the real user-management UI.

- [ ] **Step 3: Tune the user-management selectors and assertions until they pass**

Prefer real text and role selectors over implementation details, and assert:

- `User Management`
- `Create New User`
- `User Details`
- `Edit User`
- `User updated successfully!`
- delete success through the list or post-delete absence from search results

- [ ] **Step 4: Re-run the admin spec and verify the user-management slice passes**

Run:

```powershell
npx playwright test --project=chromium tests/admin/admin-operations.spec.ts
```

Expected: PASS for user-management coverage.

### Task 3: Add Milestone 9 Management And Admin Surface Coverage

**Files:**
- Modify: `Test/Playwright/tests/admin/admin-operations.spec.ts`

- [ ] **Step 1: Add failing coverage for management dashboard and operational pages**

Extend `tests/admin/admin-operations.spec.ts` with tests that:

- open `/management` and verify the dashboard plus refresh behavior
- open `/management/event-logs`, `/management/sys-logs`, `/management/email-logs`, and `/management/user-stories`
- exercise at least one safe filter or refresh action on each page
- open `/Management/SiteConfiguration`, change the registration pending message, save it, and restore the prior value in the same test
- open `/admin/diagnostics` and exercise `Refresh`
- open `/admin/showcases` and verify search/filter behavior
- open `/admin/theme-settings` and verify the settings surface loads and exposes the save workflow without committing a destructive global change
- open `/admin/update-file-sizes` and `/admin/update-preview-images` and verify the safe management controls render

- [ ] **Step 2: Run the admin spec and verify the management/admin slice fails**

Run:

```powershell
npx playwright test --project=chromium tests/admin/admin-operations.spec.ts
```

Expected: FAIL until the broader management and admin selectors are tuned.

- [ ] **Step 3: Implement stable management/admin assertions**

Keep the assertions focused on safe, deterministic behavior:

- dashboard headings/cards and manual refresh
- logs pages loading rows or empty-state tables plus filter reset behavior
- site configuration save success message and restore
- diagnostics refresh and recent-log navigation presence
- admin showcase search results
- theme-settings page load plus option visibility
- preview/file-size utility headings and guarded action buttons

- [ ] **Step 4: Re-run the admin spec and verify the full Milestone 9 slice passes**

Run:

```powershell
npx playwright test --project=chromium tests/admin/admin-operations.spec.ts
```

Expected: PASS for user management, management dashboard/log pages, and admin operational surfaces.

### Task 4: Update Docs And Run Milestone Verification

**Files:**
- Modify: `agent_docs/claude/playwright-testing.md`
- Modify: `Docs/DEVELOPER_README.md`

- [ ] **Step 1: Document the new account/admin milestone coverage**

Update the docs to mention:

- the new `tests/account` and `tests/admin` folders
- the admin-created throwaway-user strategy for account-management flows
- targeted commands:

```powershell
npx playwright test --project=chromium tests/account
npx playwright test --project=chromium tests/admin
```

- [ ] **Step 2: Run Milestone 0-9 verification**

Run:

```powershell
npx playwright test --project=chromium tests/smoke tests/showcases tests/items tests/attachments tests/zip-upload tests/templates tests/sharing tests/discovery tests/account tests/admin tests/authorization
```

Expected: PASS for the Milestone 0-9 Chromium suite.

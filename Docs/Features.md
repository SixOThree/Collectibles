## Features

### Showcases

Showcases are the primary way to organize collectibles into themed collections. Each showcase acts as a container for related items.

- Create unlimited public or private showcases with names, descriptions, and preview images
- Tag showcases for easy categorization and discovery
- Browse other users' public showcases with search and tag filtering
- Share private showcases via secure links with optional expiration dates, view tracking, revocation, and notes to share links and remember who they were shared with

![My Showcases](/Docs/Screenshots/my-showcases.png)

Explore what other collectors have shared, with search and tag-based filtering to find showcases that match your interests.

![Browse Public Showcases](/Docs/Screenshots/browse-showcases.png)

Setting up a new showcase takes seconds — give it a name, a description, and optionally choose a preview image.

![Create New Showcase](/Docs/Screenshots/create-showcase.png)

### Collectible Items

Items live inside showcases and represent individual collectibles. Each item can have custom template-driven fields, file attachments, and relationships to other items in the collection.

- Full item management with name, description, and custom template-driven fields
- **Hierarchical organization** with parent-child relationships, allowing folders and sub-items
- **Component relationships** where items can be marked as composed of other items
- Breadcrumb navigation through item hierarchies
- Per-item QR code assignment for physical tracking and scanning (public redirect, no auth required to view)
- External URL references with cached metadata and screenshots

![Showcase Detail - Items Grid](/Docs/Screenshots/showcase-detail.png)

Drill into any item to see its full details, child items, template fields, and attached files.

![Item Detail - Child Items and Attachments](/Docs/Screenshots/item-detail.png)

Items with multiple attachments display them in a scrollable gallery with thumbnail previews.

![Item with Attachment Gallery](/Docs/Screenshots/item-attachments.png)

### Dynamic Template System

Templates (Content Definitions) define the structure and fields for different types of collectibles. A vintage computer collection can have completely different fields than a coin collection.

- Define custom field schemas with drag-and-drop field ordering
- **Field types:** Text, Multiline Text, Large Text, Number, Date, DateTime, Boolean, Dropdown, and Inflation-Adjusted Price
- **Validation rules** per field: min/max length, regex patterns, numeric ranges, date ranges, decimal precision
- Global templates available to all showcases, or showcase-specific templates
- **Multi-entry mode** for items that need repeating rows of data (e.g., a magazine with multiple issues, a box set with multiple discs)
- Custom border colors and Bootstrap icons per template for visual distinction
- Set a default template that new items use automatically
- Option to hide attachments on template items to show only related child items. This is useful to allow attachments for the item preview, but allowing the item to only have related entries.

![Templates List](/Docs/Screenshots/templates.png)

The template editor lets you define fields, set validation rules, choose icons, and pick border colors — all with drag-and-drop ordering.

![Template Editor - Fields, Icons, and Border Colors](/Docs/Screenshots/template-editor.png)

Once a template is applied, items display their data in a structured table view with all the custom fields visible at a glance.

![Template Items - Table View with Dynamic Fields](/Docs/Screenshots/templated-items-table.jpg)

![Template Items - Table View with Dynamic Fields](/Docs/Screenshots/templated-items-table2.png)

### Attachments and Media

Every collectible can have photos, documents, videos, and other files attached to it. The application generates thumbnails and previews automatically, so items look good without extra effort.

- Multi-file upload with drag-and-drop support
- **Supported types:** Images (JPEG, PNG, GIF, WebP), Documents (PDF, DOCX, XLSX, PPTX), Videos (MP4, WebM, MOV), Audio, Archives (ZIP), and other files
- **Automatic preview and thumbnail generation:**
  - Images resized to 512x512 thumbnails
  - PDFs rendered from first page at 150 DPI
  - Videos extract a frame at the 1-second mark
  - Documents extract text previews
- Featured attachments with custom display ordering
- SHA-256 content hashing for automatic duplicate detection
- Searchable attachment library with filters for type, date range, and filename
- **Collage preview generation** composites up to 4 attachment images into a single item card preview

![Attachments Library with Search and Filters](/Docs/Screenshots/attachments.png)

### ZIP Bulk Upload

Upload a ZIP file to automatically create items and attachments from the folder structure inside it.

- Top-level folders become collectible items
- Nested folders create parent-child item hierarchies
- Files within folders become attachments on the corresponding item
- Background processing with real-time progress tracking (items processed, folders created, files attached, errors)
- Navigate away without interrupting the upload
- **Chunked upload mode** for large ZIP files with resume capability
- Active job monitoring with status indicators

![ZIP Upload with Folder Structure Guide](/Docs/Screenshots/zip-upload.png)

### CSV Import

Import template field data in bulk from CSV files. This is useful for software collections, books, and magazines.

- Upload a CSV file and map columns to template fields
- Header row detection and column position matching
- Batch creation of multi-entry data rows
- Error reporting for invalid or unmappable rows

### Tagging and Taxonomy

Tags help organize and discover collectibles at every level — showcases, items, and attachments can all be tagged independently.

- Tags can be applied to showcases, items, and attachments independently
- Related and suggested tags on items
- **Taxonomy vocabularies** with hierarchical terms for structured classification (e.g., "Materials" > "Metal" > "Brass")
- Configurable sort order per vocabulary (alphabetical, manual)
- Pre-seeded with **2,000+ vintage computer and technology tags** covering:
  - Computer systems and manufacturers (Apple, Commodore, IBM, Atari, etc.)
  - Hardware components (processors, graphics cards, storage, peripherals)
  - Software and operating systems
  - Collectible condition and status (Mint, Needs Repair, For Sale, etc.)
  - Handheld devices (PDAs, calculators, gaming handhelds, organizers)

### QR Code Management

Physical collectibles can be labeled with printed QR codes. Scanning a code with a phone opens the item page directly, making it easy to look up details while browsing a physical collection.

- QR Codes link to the collectible item when scanned from a mobile phone
- Generate batches of up to 100 QR codes at a time
- Assign, unassign, or revoke codes on items
- Track scan count and last-scanned date per code
- **Public QR redirect** allows anyone to scan a code and view the item without authentication (assuming the collection is public)
- Dashboard with statistics: total, assigned, and unassigned codes

![QR Code Management](/Docs/Screenshots/qr-codes.png)

### External Link Caching (Production Playwright Use)

Collectible Items have fields for external links. The application uses the `Microsoft.Playwright` C# NuGet package to run a headless Chromium browser in the background, so that when a link is added, a cached version of the page will be saved as HTML and as a full page screenshot. This is handy for reference items. This production use of Playwright is separate from the Playwright E2E test suite used for development testing.

- Add URLs with optional titles to any item
- Background service automatically caches link content and metadata
- **Screenshot capture** of linked pages via Playwright headless browser automation
- Cache status tracking: Pending, Processing, Completed, Failed
- Preserved snapshots protect against link rot

### Inflation-Adjusted Pricing

Items can have an "Inflation-adjusted price". The user enters the original price of an item along with a year. When the item is viewed, the built-in inflation calculator uses CPI data to show the modern-day price equivalent.

- Store original purchase price with the year of purchase
- Automatically adjusts to current-year dollars using US CPI data
- Available as a dedicated template field type (Inflation-Adjusted Price)
- Calculate inflation rates between any two supported years

![Inflation-Adjusted Price on Item Detail](/Docs/Screenshots/inflation-adjusted-price.png)

### Showcase Sharing

Showcases can be shared publicly without requiring viewers to create accounts. Collections can also be shared via a link while still remaining hidden from normal users.

- Generate unique, secure share links for any showcase
- Optional expiration dates for temporary access
- View count tracking per share link
- Notes system to remember who each link was shared with
- Instant revocation to disable access
- Clean, read-only public view with items, attachments, and tags
- History of expired and revoked links for audit purposes

![Showcase Sharing with Expiration and Notes](/Docs/Screenshots/showcase-sharing.png)

---

## Storage and Uploads

### Multi-Provider File Storage

Choose where attachment files are stored, and migrate between providers at any time.

- **Database storage** (legacy default): files stored as binary in the database
- **Azure Blob Storage** (preferred method): cloud-based storage with SAS token direct uploads
- **Local file system**: files stored on disk at a configurable path
- Admin migration tool with progress tracking, verification, and rollback

### Large File Upload System

Large files are handled automatically. The application picks the best upload method based on file size, using direct-to-Azure uploads and chunked transfers for files that would otherwise time out.

| File Size | Upload Method |
|-----------|--------------|
| Under 50 MB | Standard server-side upload |
| 50 MB - 2 GB | Direct-to-Azure via SAS tokens (bypasses Cloudflare/proxy limits) |
| Over 2 GB | Chunked upload in 10 MB segments with resume support |

- Maximum file size: 20 GB
- Real-time progress tracking with percentage, speed, and bytes transferred

---

## Security

![Login Page](/Docs/Screenshots/login.png)

### Authentication and Access Control

- ASP.NET Identity with email/password authentication
- **Two-factor authentication** with authenticator app and recovery codes
- **API key authentication** for programmatic access via `X-Api-Key` header
- Resource-based authorization ensures users can only access their own data
- Role-based access control: Administrator, UserManager, Viewer
- External authentication provider support (Google, Microsoft, etc.)

### Password Policy

- Minimum 12 characters with digit, lowercase, uppercase, and special character requirements
- Minimum 6 unique characters
- **Password history tracking** prevents reuse of recent passwords
- Account lockout after 5 failed attempts (15-minute cooldown)

### ID Obfuscation

Database IDs are never exposed to users. All user-facing URLs and identifiers use HashIds for secure, URL-safe obfuscation (e.g., `/item/x9K2mN` instead of `/item/12345`).

### Security Middleware

- **Security scan blocking**: detects suspicious URL patterns (SQL injection, directory traversal) with IP-based rate limiting and automatic blocking
- **Crawler blocking**: user-agent based bot filtering with allow/block lists
- **Security headers**: Content Security Policy, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy

### First-Run Setup

Secure initial administrator creation requires physical server access.

1. On first launch with no admins, the app generates a cryptographic setup token saved to `App_Data/setup-token.txt`
2. Navigate to `/Setup` and enter the token to create the first admin account
3. The token file is automatically deleted and the setup page becomes inaccessible

---

## Administration

### Management Dashboard

A centralized admin dashboard with at-a-glance statistics and quick actions.

- Counts for users, showcases, items, attachments, tags, and active jobs
- Activity charts and indexing status
- Orphaned file detection
- Recent events and system log entries

![Management Dashboard](/Docs/Screenshots/dashboard.png)

### User Management

- Create, edit, deactivate, and delete user accounts
- Assign and modify roles
- Debug-mode test user creation for development

![User Management](/Docs/Screenshots/user-management.jpg)

### Logging and Audit Trail

Comprehensive logging across four separate systems:

- **Event logs**: full audit trail of user actions (create, update, delete, view, login, share, bulk operations, and 20+ other action types) with old/new value tracking
- **User stories**: session-based activity tracking and analysis
- **System logs**: application-level logging with severity filtering (Trace through Critical)
- **Request logs**: HTTP request/response logging with performance metrics and correlation IDs
- **Email logs**: delivery status, retry tracking, and template data for all outgoing emails

All log views support advanced filtering, search, date ranges, export to CSV, and pagination.

![Event Logs](/Docs/Screenshots/event-logs.png)

System logs capture application-level diagnostics with severity filtering, making it easy to trace issues across requests.

![System Logs](/Docs/Screenshots/system-logs.png)

### Email System

Multi-provider email with templating, queuing, and full delivery tracking.

- **Providers:** SMTP, SendGrid, Azure Communication Services, or log-only (null provider for development)
- **Templates:** Welcome, Password Reset, Email Confirmation, Notification
- Queue-based sending with configurable retry (exponential backoff)
- Priority and scheduled send support
- Complete email audit log with status tracking

![Email Logs](/Docs/Screenshots/email-logs.png)

### Site Configuration

Runtime key-value configuration stored in the database, changeable without application restart.

![Site Configuration](/Docs/Screenshots/site-configuration.png)

### Theme System

- Multiple Bootswatch theme options
- Custom background images
- Dark and light mode support
- Real-time preview and switching from admin settings

![Theme Settings - Bootswatch Themes and Background Images](/Docs/Screenshots/theme-settings.png)

### Maintenance Tools

- **Storage migration**: move attachments between storage providers with progress, verification, and rollback
- **Preview regeneration**: rebuild missing or outdated attachment thumbnails
- **Orphaned file cleanup**: detect and remove files no longer linked to any item
- **System diagnostics**: environment info, database connectivity, and log file browser

![Maintenance Tools](/Docs/Screenshots/maintenance-tools.png)

The diagnostics page provides a quick health check of the environment, database connectivity, and access to log files.

![System Diagnostics](/Docs/Screenshots/diagnostics.png)

---

## Background Processing

Hangfire-powered background jobs handle long-running and recurring tasks.

| Job | Schedule | Purpose |
|-----|----------|---------|
| Email processing | Every 1 minute | Send queued emails with retry |
| Email log cleanup | Daily at 2 AM | Remove email logs older than 90 days |
| Attachment hashing | Every 5 minutes | Compute SHA-256 hashes for deduplication |
| Preview generation | Every 5 minutes | Generate missing thumbnails (batches of 20) |
| Collage generation | Every 5 minutes | Create item card preview collages |
| Link processing | Every 1 minute | Cache external link metadata and screenshots |
| Request log cleanup | Daily at 3 AM | Remove request logs older than 7 days |
| ZIP job cleanup | Every hour | Mark abandoned upload jobs as failed |

- Hangfire dashboard available at `/hangfire` with optional authorization
- Automatic retry on failure (3 attempts with 10s, 30s, 60s delays)

---

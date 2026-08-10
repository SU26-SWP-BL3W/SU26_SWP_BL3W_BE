# Event Dashboard Design Spec

**Date:** 2026-06-12  
**Status:** Design Approved  
**Scope:** 3-screen event dashboard with sidebar navigation for team competitions

---

## Overview

The Event Dashboard enables teams to manage their participation in competition events. It provides three main screens accessed via sidebar tabs:

1. **Event Dashboard** — Event overview, team registry, registration entry point
2. **Submission** — Team submission management (ZIP upload or external URLs)
3. **Results** — Leaderboard and score breakdown with team comparison metrics

The dashboard follows a **classic left-sidebar layout** with fixed-height viewport (no scrolling on main page). All content fits on a single screen. The design adheres to the **monogreen design system** for typography, colors, spacing, and component patterns.

---

## User Flows

### Flow 1: Browse Events → Register Team
1. User lands on Event Dashboard tab
2. Sees event details and registered teams list
3. Clicks "Register" button → Modal opens for team creation
4. Fills team name, adds members (user-linked)
5. Modal closes, team appears in registry
6. User can now submit → Results tabs become active

### Flow 2: Submit Entry (Configured by Event Coordinator)
1. User clicks Submission tab
2. Form shows submission type (ZIP, External URL, or both) per event config
3. User uploads/submits, sees confirmation
4. Submission history appears below with timestamps and status

### Flow 3: View Results & Rankings
1. After event closes, Results tab shows team's standing
2. User sees rank, score breakdown by criteria, comparison vs. average
3. Full leaderboard visible at bottom (current team highlighted)

---

## Layout Structure

### Viewport Grid
- **Fixed height:** Full viewport (no scrolling on main page)
- **3-column grid:** Sidebar (240px) | Header (full width) | Content (remainder)
- **Background:** `{colors.canvas}` (white)

### Design System Adherence
All components use **monogreen tokens**:
- Colors: `{colors.primary}` (#76b900), `{colors.surface-dark}` (#000000), `{colors.canvas}` (#ffffff), `{colors.hairline}` (#cccccc)
- Typography: monogreen hierarchy from `{typography.heading-md}` (20px) down to `{typography.caption-sm}` (12px)
- Spacing: 8px base unit — `{spacing.lg}` (16px), `{spacing.xl}` (24px)
- Borders: `{colors.hairline}` (1px), `{rounded.sm}` (2px corners)

---

## Component Designs

### 1. Sidebar Navigation

**Visual Structure:**
- **Background:** `{colors.surface-dark}` (#000000)
- **Width:** 240px fixed
- **Layout:** Flex column, full height
- **Border-right:** `{colors.hairline-strong}` 1px divider

**Tabs (3 stacked):**
- BarChart3 icon + Event Dashboard
- Upload icon + Submission
- Trophy icon + Results

**Tab Styling:**
- Padding: `{spacing.lg}` (16px) vertical, `{spacing.lg}` (16px) horizontal
- Icon (18px from Heroicons) + label text side-by-side, 8px gap
- Border-bottom: `{colors.hairline-strong}` separator
- Inactive: `{colors.on-dark}` text (#ffffff), icon opacity 0.6
- Active: `{colors.primary}` (#76b900) left border accent bar (4px), full text, icon opacity 1.0
- Typography: `{typography.body-strong}` (16px bold)
- Hover: Subtle background tint (`rgba(255,255,255,0.08)`), smooth transition (150ms)
- Focus state: Outline 2px solid `{colors.primary}`, outline-offset 2px (keyboard navigation)

**User Section (sticky bottom):**
- Border-top: `{colors.hairline-strong}` divider
- Layout: Avatar (32px circle) + user name + role badge
- Typography: `{typography.body-sm}` (14px)
- Padding: `{spacing.lg}` (16px)
- Role badge: Pill shape, background `{colors.surface-soft}` (#f7f7f7)
- Mobile (< 768px): Avatar only at 60px width; full profile accessible via settings icon (18px Heroicons, right-align). Click opens user profile drawer.
- Hover: Slight background tint, smooth transition (150ms)
- Focus state: Outline 2px solid `{colors.primary}`, outline-offset -2px

---

### 2. Header

**Position:** Fixed top, spans sidebar + content area  
**Background:** `{colors.canvas}` (#ffffff)  
**Border-bottom:** `{colors.hairline}` (#cccccc) 1px divider

**Content:**
- **Left:** Event title — `{typography.heading-md}` (20px bold, `{colors.ink}`)
- **Center:** Event meta row (date range, status badge, submission type)
  - Status badge: `{colors.primary}` for "Open", `{colors.stone}` for "Closed"
  - Typography: `{typography.body-sm}` (14px)
- **Padding:** `{spacing.lg}` (16px) horizontal, `{spacing.md}` (12px) vertical

---

### 3. Event Dashboard Content Area

**Layout:** 2-column grid (60% | 40%)

**Left Column: Event Details Card**
- Title: "Event Details" — `{typography.heading-sm}` (18px bold)
- Content:
  - Description text — `{typography.body-md}` (16px)
  - Key info rows (label | value):
    - Start Date
    - End Date
    - Status
    - Submission Type (ZIP / URL / Both)
    - Total Teams Registered
  - Typography: `{typography.body-sm}` (14px) labels, `{typography.body-strong}` values
- Card styling: `{colors.canvas}` background, `{colors.hairline}` border, `{spacing.xl}` (24px) padding, `{rounded.sm}` corners
- Spacing: `{spacing.lg}` (16px) between rows

**Right Column: Team List Card**
- Title: "Registered Teams" — `{typography.heading-sm}` (18px bold)
- List rows (one per team):
  - Team Name | Leader Name | Member Count
  - Typography: `{typography.body-sm}` (14px)
  - Divider: `{colors.hairline}` between rows
  - Max visible: 5-6 teams; if more, internal scroll within card (not main page)
- Card styling: Same as Event Details (border, padding, background)

**Full Width Bottom: Register Button**
- Primary CTA button: `{colors.primary}` background (#76b900)
- Text: "Register Team" — `{typography.button-md}` (16px bold, white)
- Padding: `{spacing.lg}` horizontal, `{spacing.md}` vertical
- Hover: `{colors.primary-dark}` (#5a8d00) pressed state
- Disabled state: `{colors.stone}` (#898989)
- Border: `{rounded.sm}` (2px corners)
- Action: Opens Team Registration Modal

---

### 4. Team Registration Modal

**Overlay:** Semi-transparent dark — `rgba(0, 0, 0, 0.5)`  
**Modal Box:** Centered, 500px width, `{colors.canvas}` background, `{colors.hairline}` border

**Header:**
- Title: "Create New Team" — `{typography.heading-md}` (20px bold)
- Close button (X icon from Heroicons, 20px) top-right, `{colors.body}` color, hover → `{colors.ink}`
  - Button: `cursor-pointer`, transparent background, focus state: outline 2px solid `{colors.primary}`
  - aria-label="Close dialog"

**Form Fields:**

**1. Team Name Input**
- Label: "Team Name" — `{typography.body-strong}` (16px bold)
- Input field:
  - Placeholder: "Enter team name"
  - Padding: `{spacing.lg}` (16px)
  - Border: `{colors.hairline}` 1px, `{rounded.sm}` corners
  - Focus state: `{colors.primary}` border, shadow
  - Typography: `{typography.body-md}` (16px)

**2. Team Members Section**
- Label: "Add Members" — `{typography.body-strong}` (16px bold)
- Current user auto-set as "Leader" (read-only display)
- Add member input:
  - Search/dropdown by user name or email
  - Placeholder: "Search by name or email"
  - Button: "+ Add" (secondary, small)
- Display selected members as removable chips below:
  - Chip: `{colors.surface-soft}` background, `{colors.ink}` text, `{rounded.sm}` corners
  - Remove button: X icon (12px from Heroicons) on right side, `cursor-pointer`
    - Hover: `{colors.error}` background (light red tint)
    - aria-label="Remove {memberName} from team"
  - Typography: `{typography.caption-xs}` (11px)
  - Spacing: 4px gap between chips, 8px margin-top from label
- Max members: Determined by event config (no hard limit in form)
- Keyboard: Users can remove chips with Backspace (if last chip focused)

**Action Buttons (bottom, right-aligned):**
- "Cancel" button: Secondary, `{colors.canvas}` background, `{colors.hairline}` border
  - Typography: `{typography.button-md}` (16px bold)
- "Create Team" button: Primary, `{colors.primary}` background, 16px margin-left
  - Disabled during submission

**Spacing:** `{spacing.xl}` (24px) padding inside modal, `{spacing.lg}` (16px) between form sections

---

### 5. Submission Content Area

**Layout:** 2-column (50% | 50%)

**Left Column: Submission Form Card**
- Title: "Submit for Event" — `{typography.heading-sm}` (18px bold)
- Submission Type Badge (monogreen badge component):
  - Shows "ZIP Upload", "External URL", or "Both"
  - Background: `{colors.surface-soft}`, text: `{colors.ink}`
  - Typography: `{typography.caption-sm}` (12px)

**Form (conditional on submission type):**
- **If ZIP:** 
  - File upload input (drag-drop + click to browse)
  - Allowed formats: .zip
  - Max size: TBD by event
  - Drag-drop area: `{colors.surface-soft}` background, dashed `{colors.hairline}` border, min-height 120px
    - Icon: Upload icon (32px from Heroicons, `{colors.mute}`)
    - Text: "Drag ZIP file here or click to browse" — `{typography.body-md}` (16px)
    - Supported: ".zip only, max {size}MB" — `{typography.body-sm}` (14px, muted)
  - Hover: Border color changes to `{colors.primary}`, icon opacity 1.0, smooth transition (150ms)
  - Focus: Outline 2px solid `{colors.primary}`, outline-offset -2px
  - Error state: Border color `{colors.error}`, icon color `{colors.error}`

- **If External URL:**
  - Text input for link
  - Label: "Submission Link" — `{typography.body-strong}` (16px bold)
  - Icon in input: Link icon (16px from Heroicons, right-aligned, `{colors.mute}`)
  - Placeholder: "https://github.com/... or https://drive.google.com/..."
  - Validation: URL format check (real-time, green checkmark icon on valid)
  - Help text: "Paste GitHub, Drive, or similar link" — `{typography.body-sm}` (14px, muted)
  - Error: Red error text below, Error icon (12px) prefix

- **If Both:**
  - Tab switcher: 2 buttons above form
    - "ZIP Upload" button (outlined, primary color on active)
    - "External Link" button (outlined, primary color on active)
  - Show appropriate form per selection
  - Active tab: `{colors.primary}` background, white text; Inactive: transparent, border `{colors.hairline}`
  - Spacing: `{spacing.md}` (12px) gap between tabs

- Submit button: Primary `{colors.primary}`, `{typography.button-md}` (16px bold)
- Disabled during upload/submission

**Card styling:** `{colors.canvas}` background, `{colors.hairline}` border, `{spacing.xl}` (24px) padding

**Right Column: Submission History Card**
- Title: "Submission History" — `{typography.heading-sm}` (18px bold)
- Table/list rows:
  - Columns: Date | Submission Type | Status
  - Each row: 
    - Date: `{typography.caption-sm}` (12px)
    - Type: "ZIP" or "URL"
    - Status badge: "Submitted" (`{colors.primary}`), "Pending Review" (`{colors.warning}`), "Rejected" (`{colors.error}`)
  - Divider: `{colors.hairline}` between rows
  - Hover: Subtle `{colors.surface-soft}` background
- Card styling: Same as form card

**Spacing:** `{spacing.lg}` (16px) between columns, `{spacing.xl}` (24px) internal padding

---

### 6. Results Content Area

**Layout:** Single column, stacked sections

**Top Section: Current Standing Card**
- Background: `{colors.canvas}` with `{colors.primary}` left border accent (4px)
- Content:
  - Rank display: "#3 out of 12 teams" — `{typography.display-lg}` (36px bold, `{colors.primary}`)
  - Score: "Total Score: 255/300" — `{typography.heading-md}` (20px bold)
  - Status badge: "Submitted" / "Under Review" / "Graded" — monogreen badge
  - Card padding: `{spacing.xl}` (24px)

**Middle Section: Score Breakdown Grid (2 columns)**

**Left Column (60%):**
- Title: "Score Breakdown" — `{typography.heading-sm}` (18px bold)
- Criteria list (rows):
  - Format: "Criterion Name | Score/Max Points | Status Icon"
  - Examples:
    - Code Quality: 85/100 (CheckCircle icon, 16px, `{colors.success-deep}` #3f8500)
    - Functionality: 92/100 (CheckCircle icon, 16px, `{colors.success-deep}`)
    - Design: 78/100 (Clock icon, 16px, `{colors.warning}` #df6500)
  - Typography: `{typography.body-sm}` (14px) labels, `{typography.body-strong}` scores
  - Status icon placement: Right-aligned before score
    - Graded: CheckCircle icon (green) with aria-label="Graded"
    - Pending: Clock icon (warning) with aria-label="Pending"
  - Row divider: `{colors.hairline}`
  - Hover: Subtle `{colors.surface-soft}` background, smooth transition (150ms)
  - Focus: Outline 2px solid `{colors.primary}`, outline-offset 4px
- Card styling: `{colors.canvas}` background, `{colors.hairline}` border, `{spacing.xl}` (24px) padding

**Right Column (40%):**
- Title: "Comparison" — `{typography.heading-sm}` (18px bold)
- Content:
  - "Average Score: 220/300" — `{typography.body-md}` (16px)
  - "Your Score: 255/300" — `{typography.body-strong}` (16px, `{colors.primary}`)
  - Delta: "+35 above average" — `{typography.body-sm}` (14px, `{colors.primary}`)
  - Optional: Simple bar chart or progress visualization (if space)
- Card styling: Same as breakdown card

**Bottom Section: Leaderboard Table**
- Full width
- Title: "Leaderboard" — `{typography.heading-sm}` (18px bold)
- Columns: Rank | Team Name | Total Score | Status
- Rows (up to 10 visible, internal scroll if more):
  - Rank: `{typography.body-strong}` (16px)
  - Team Name: `{typography.body-md}` (16px)
  - Score: `{typography.body-md}` (16px)
  - Status: Monogreen badge
  - Current team row: `{colors.primary}` left border accent (4px) to highlight
  - Alternate row backgrounds: None (hairline dividers only)
- Card styling: `{colors.canvas}` background, `{colors.hairline}` border, `{spacing.lg}` (16px) padding

---

## Error Handling & Loading States

### Loading States

**Tab Transition (200ms fade):**
- Content fades out (0ms) → In (100ms) → Visible (100ms) — total 200ms
- Skeleton loaders visible while data loads (optional, for slow connections)

**Skeleton Loader Component:**
- Placeholder cards matching content layout
- Background: `{colors.surface-soft}` (#f7f7f7)
- Shimmer effect: Animated gradient from left to right
  - Gradient: `linear-gradient(90deg, rgba(255,255,255,0) 0%, rgba(255,255,255,0.3) 50%, rgba(255,255,255,0) 100%)`
  - Animation: 2s loop, `animation-delay` for staggered effect
  - Respect `prefers-reduced-motion`: No animation if enabled
- Line heights: Match expected text (e.g., 16px skeleton for body text)
- Spacing: Margin matches actual card layout

**Form Submission:**
- Submit button becomes disabled + loading spinner inside
- Spinner: 16px animated SVG (monogreen primary color)
- Text change: "Create Team" → "Creating..." (optional, button stays same width)
- Aria-live region: "Creating team, please wait..." (aria-live="polite")

**Data Fetch on Tab Switch:**
- Show skeleton loaders in content area while data loads
- Tab button itself doesn't disable (user can switch to another tab)
- Timeout: Show error message if data doesn't load in 10 seconds

### Error States

**Form Validation Errors:**
```html
<input aria-invalid="true" aria-describedby="error-teamname">
<span id="error-teamname" role="alert" style="color: {colors.error}">
  <span aria-hidden="true">⚠️</span> Team name is required
</span>
```
- Red error text below field, color: `{colors.error}` (#e52020)
- Typography: `{typography.body-sm}` (14px bold for accessibility)
- Icon: AlertCircle icon (16px, `{colors.error}`) prefix
- Border: Input border changes to `{colors.error}` on focus
- Multiple errors: List them below field, all visible at once (no collapse)

**API Error Cards:**
- Card background: `{colors.error}` at 10% opacity (very light red)
- Border: 1px solid `{colors.error}`
- Icon: AlertTriangle icon (20px, `{colors.error}`, top-left)
- Title: "Error" — `{typography.body-strong}` (16px bold)
- Message: Error description — `{typography.body-sm}` (14px)
- Action: "Retry" button (secondary style), optional "Dismiss" button
- Placement: Top of content area OR inline in affected card
- Aria-role: `role="alert"` for immediate screen reader announcement

**Submission Errors (File Upload):**
- Example: "File upload failed — max size is 50MB. Please compress your ZIP or try again."
- Error card in submission form area with retry button
- File input remains visible for retry (don't clear)

### Empty States

**No Teams Registered (Event Dashboard):**
- Illustration: Simple geometric icon (e.g., User icon + Plus icon, monogreen outline, 64px)
- Heading: "No teams yet" — `{typography.heading-md}` (20px bold)
- Message: "Be the first to register a team for this event" — `{typography.body-md}` (16px, `{colors.mute}`)
- CTA: "Register Team" button (primary, `{colors.primary}`)
- Placement: Center of Team List card

**No Submissions (Submission Tab):**
- Illustration: Upload icon with arrow, monogreen outline, 64px
- Heading: "No submissions yet" — `{typography.heading-md}` (20px bold)
- Message: "Complete your solution and submit before the deadline" — `{typography.body-md}` (16px, `{colors.mute}`)
- CTA: "Upload" button (primary, focuses form input)

**Results Not Available (Results Tab):**
- Illustration: Trophy icon, monogreen outline, 64px (grayed out)
- Heading: "Results pending" — `{typography.heading-md}` (20px bold)
- Message: "Check back after the event closes on [DATE]" — `{typography.body-md}` (16px, `{colors.mute}`)
- Time remaining: "Closes in 2 days, 4 hours" — `{typography.body-strong}` (16px, `{colors.primary}`)

**Empty State Common Styling:**
- Container: Minimum 200px height, center content vertically
- Background: Transparent (inherit card background)
- Icon: 64px Heroicons, `{colors.stone}` (#898989) color
- All empty state content: Center-aligned

---

## Responsive Design

### Desktop (1024px+)
- Sidebar: 240px fixed
- Content: 2-column layouts as designed
- All single-screen (no scroll)

### Tablet (768px - 1023px)
- Sidebar: 200px, icons + condensed labels
- Content: 2-column layouts reflow if needed, maintain single-screen goal
- Modal: 80% viewport width (capped at 500px)

### Mobile (< 768px)
- Sidebar: **Collapses to 60px icon-only tab bar** (left edge)
  - Icons only (18px from Heroicons, BarChart3, Upload, Trophy)
  - Labels hidden; accessible via tooltip on long-press (use `title` attribute)
  - Tabs remain stacked vertically, full height
  - Active tab: Same `{colors.primary}` left border accent (4px)
  - Focus: Outline 2px solid `{colors.primary}`, outline-offset -2px (inside button)
  
- Header: Remains full-width, text may truncate (font-size may reduce to 18px heading-sm at 375px viewport)
  
- Content: **1-column layout** (full width minus 60px sidebar)
  - Cards stack vertically
  - Register button full-width
  - All card padding maintained at `{spacing.lg}` (16px)
  
- **Event Dashboard Tab:**
  - Event Details card (100% width)
  - Team List card (100% width, below details card)
  - Register button (full width)
  
- **Submission Tab:**
  - Submission form card (100% width)
  - Submission history card (100% width, below form)
  
- **Results Tab:**
  - Current Standing card (100% width)
  - Score Breakdown section (100% width, single column — no 60%/40% split)
  - Comparison metrics (below breakdown, full width)
  - Leaderboard: **Card layout instead of table**
    - Each team as a card: Rank badge (left) | Team name (center) | Score (right) | Status (top-right corner)
    - Cards stack vertically, 1 per row
    - Current user's team: `{colors.primary}` left border accent (4px)
    - Tap to expand (optional): Show additional details
  
- Modal: Full viewport width minus padding
  - Padding: `{spacing.lg}` (16px) on all sides
  - Max-width: 500px (centered, even on wide phones)
  - Form fields: Full width
  - Button group: Stack vertically (Cancel above, Create Team below) OR keep side-by-side with smaller padding
  
- Scrolling: Content within each card (team list, leaderboard) may scroll internally, but main page doesn't scroll (fixed viewport)

---

## Interactions & Micro-Behaviors

### Tab Switching
- Active tab shows `{colors.primary}` accent bar
- Content fades in/out (200ms transition)
- Scroll position reset (everything visible on screen)
- No history/back button needed (same event context)

### Modal Interactions
- Opens centered with fade-in animation
- Closes on "Cancel", overlay click, or ESC key
- Form resets on close (validation cleared)
- Focus trap inside modal (Tab cycles through form fields)

### Form Submissions
- Submit button disabled during request
- Loading spinner visible
- Success: Toast notification ("Team created!"), modal closes, Event Dashboard refreshes
- Error: Show error message inline, allow retry without closing modal

### Data Refresh
- No polling — data updates on tab switch or manual action
- Submission success: Auto-refresh Submission History
- Event closes: Results appear automatically (backend driven)

---

## Icon Library

**Primary Icon Source:** Heroicons (https://heroicons.com) v2.0
- License: MIT (free for commercial use)
- Format: SVG, 24x24 default (scale as needed: 16px, 18px, 20px, 32px)
- Style: Outline (consistent with monogreen sharp aesthetic)

**Icons Used in Dashboard:**

| Component | Icon | Size | Color | Accessibility |
|-----------|------|------|-------|---|
| Event Dashboard tab | BarChart3 | 18px | `{colors.on-dark}` / `{colors.primary}` | aria-label="Event Dashboard tab" |
| Submission tab | ArrowUpTray (Upload) | 18px | `{colors.on-dark}` / `{colors.primary}` | aria-label="Submission tab" |
| Results tab | Trophy | 18px | `{colors.on-dark}` / `{colors.primary}` | aria-label="Results tab" |
| Modal close | XMark (X) | 20px | `{colors.body}` on hover → `{colors.ink}` | aria-label="Close dialog" |
| Remove member chip | XMark (X) | 12px | `{colors.ink}` | aria-label="Remove {name}" |
| File upload | ArrowUpTray (Upload) | 32px | `{colors.mute}` | aria-hidden="true" (decorative) |
| External URL input | Link | 16px | `{colors.mute}` | aria-hidden="true" (decorative) |
| Graded status | CheckCircle | 16px | `{colors.success-deep}` (#3f8500) | aria-label="Graded" |
| Pending status | Clock | 16px | `{colors.warning}` (#df6500) | aria-label="Pending" |
| Settings (mobile user menu) | Cog6Tooth | 18px | `{colors.on-dark}` | aria-label="User settings" |

**Implementation Notes:**
- Import as React components (e.g., `from 'lucide-react'` OR use Heroicons SVG directly)
- Always include `aria-label` or `aria-hidden` for screen reader accessibility
- Set SVG `viewBox="0 0 24 24"` and apply width/height via CSS classes
- Decorative icons (file upload background): `aria-hidden="true"`
- Interactive icons (buttons): Include parent `<button>` with aria-label

---

## Component Reuse

### Existing monogreen Components to Use
- Status badges (Open/Closed/Graded) — update with proper icons instead of text
- Primary/secondary buttons — all with proper focus states (outline 2px)
- Input fields with focus states — include icons where relevant
- Card layout with border — consistent hairline styling
- Typography hierarchy — maintained across all sections

### New Components to Create
- Team registration modal with form validation
- Submission type badge (ZIP/URL/Both) — icon + text badge
- Leaderboard table row (desktop) + card layout (mobile)
- Score breakdown row with status icons
- Comparison metric card with optional chart visualization
- Icon button wrapper (handles focus, hover, aria-labels)
- Chip component with remove functionality
- Toast notification component for feedback
- Skeleton loader component (shimmer effect)

---

## Data Flows

### Event Dashboard Tab
- Fetch: Event details, team list, user's registration status
- Display: Event info, teams, conditionally show "Register" or show user's team

### Submission Tab
- Fetch: Submission type config, user's team, submission history
- Display: Submission form (conditional), history table
- Action: POST submission data, refresh history

### Results Tab
- Fetch: User's team, score breakdown, all teams' scores
- Display: Rank card, breakdown, comparison, leaderboard
- State: Read-only (no user actions)

---

## Accessibility

### Semantic HTML Structure

**Sidebar Navigation:**
```html
<nav aria-label="Event dashboard navigation">
  <ul role="tablist">
    <li role="presentation">
      <button role="tab" aria-selected="true" aria-label="Event Dashboard">
        <!-- BarChart3 icon -->
      </button>
    </li>
    <!-- repeat for other tabs -->
  </ul>
</nav>
```

**Main Content Area:**
- Use `<main>` for primary content
- Use `<section>` for major content blocks (e.g., Event Details, Team List)
- Use `<article>` for leaderboard team cards

**Form Structure:**
```html
<form>
  <label for="teamname">Team Name</label>
  <input id="teamname" type="text" aria-required="true">
  <span id="error-teamname" role="alert"></span> <!-- error message, hidden initially -->
</form>
```

**Modal Dialog:**
```html
<dialog aria-labelledby="modal-title" aria-modal="true">
  <h2 id="modal-title">Create New Team</h2>
  <!-- form content -->
</dialog>
```

### Keyboard Navigation

| Element | Keyboard Behavior |
|---------|-------------------|
| Sidebar tabs | `Tab` to focus, `Enter` or `Space` to activate, `ArrowDown/Up` to navigate |
| Form inputs | `Tab` between fields, `Enter` to submit (button), `Shift+Tab` backwards |
| Modal | `Tab` cycles through form fields (focus trap), `Esc` closes, `Enter` submits |
| Member chips | `Tab` to focus, `Backspace` to remove (if focused), `ArrowLeft/Right` to navigate |
| Leaderboard (mobile) | `Tab` to focus cards, `Enter` to expand details (optional) |

### Focus States (WCAG AA Compliant)

All interactive elements must have visible focus:
- **Outline style:** 2px solid `{colors.primary}` (#76b900)
- **Outline offset:** 2px (outside) OR -2px (inside, for icon buttons)
- **Contrast ratio:** 3:1 minimum against background
- **No removal:** Never use `outline: none` without replacement

Example: `button:focus-visible { outline: 2px solid {colors.primary}; outline-offset: 2px; }`

### Color Contrast

**All text on backgrounds must meet WCAG AA (4.5:1 for normal text, 3:1 for large text):**
- ✅ Black (#000000) on white (#ffffff): 21:1
- ✅ Green (#76b900) on white (#ffffff): 6.5:1
- ✅ Gray (#757575) on white (#ffffff): 5.8:1
- ⚠️ Light gray (#a7a7a7) on white (#ffffff): 3.2:1 — use for secondary only, not body text
- ✅ White (#ffffff) on black (#000000): 21:1
- ✅ White (#ffffff) on dark green hover: 8:1+

Test contrast with WebAIM Contrast Checker before deployment.

### ARIA Labels & Descriptions

**Icon buttons (no visible text):**
```html
<button aria-label="Close dialog">
  <!-- X icon -->
</button>
```

**Form fields with errors:**
```html
<input aria-describedby="error-teamname">
<span id="error-teamname" role="alert">Team name is required</span>
```

**Status badges:**
```html
<span aria-label="Graded">
  <!-- CheckCircle icon -->
</span>
```

**Decorative icons:**
```html
<span aria-hidden="true">
  <!-- Upload icon in background -->
</span>
```

### Screen Reader Testing

- Sidebar navigation should announce: "Event Dashboard tab, selected" when active
- Form labels should associate with inputs (test with Tab key)
- Error messages should announce with role="alert"
- Modal should announce dialog title on open
- Leaderboard should read: "Rank 3, Team Name, Score 255, Graded"

### Animation & Motion

- Respect `prefers-reduced-motion` media query:
  ```css
  @media (prefers-reduced-motion: reduce) {
    * { animation-duration: 0.01ms !important; transition-duration: 0.01ms !important; }
  }
  ```
- All transitions should be 150-300ms (150ms default for micro-interactions)
- Avoid flashing (> 3 flashes per second)

### Mobile & Touch Accessibility

- **Touch target size:** Minimum 44x44px for all interactive elements
- **Sidebar icons:** 60px width = 60x60px touch targets ✅
- **Modal buttons:** 48px height (button-md + padding) ✅
- **Icon buttons:** Minimum 44x44px, apply padding as needed

---

## Browser Support

- Chrome/Edge (latest 2 versions)
- Firefox (latest 2 versions)
- Safari (latest 2 versions)
- Mobile browsers (iOS Safari, Chrome Android)

---

## Success Criteria

✓ All 3 tabs accessible via sidebar navigation  
✓ Event Dashboard displays event details and team list  
✓ Register button opens modal, creates team, updates UI  
✓ Submission form matches event config (ZIP/URL/Both)  
✓ Results tab shows rank, score breakdown, leaderboard  
✓ No scrolling on main page (all fits single screen)  
✓ Mobile layout collapses sidebar to icon bar  
✓ Follows monogreen design system tokens/components  
✓ Error handling and loading states functional  
✓ Smooth tab transitions (fade, 200ms)

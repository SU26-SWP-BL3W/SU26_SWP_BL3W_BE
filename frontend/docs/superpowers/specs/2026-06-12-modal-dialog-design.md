# Modal Dialog with Fixed Header/Footer - Design Spec

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:writing-plans to implement this design.

**Goal:** Design and implement a responsive modal dialog with fixed header/footer and scrollable form content for team registration on all screen sizes (375px-1440px).

**Architecture:** Modal uses a 3-section layout (fixed header, scrollable content, fixed footer) to ensure form is always accessible and action buttons never scroll out of view.

**Tech Stack:** React, Next.js, Tailwind CSS, TypeScript

---

## Problem Statement

Current modal implementation causes content overflow:
- Modal title "Create New Team" gets cut off at top on small viewports
- Cancel/Create Team buttons partially hidden at bottom
- No scrolling mechanism for form content
- Poor UX on mobile (375px) where viewport is constrained

Solution: Implement fixed header/footer with scrollable middle section.

---

## Design: Modal Layout Structure

### Container Architecture

```
┌─────────────────────────────────┐
│ Modal Overlay (fixed, z-[100])  │
├─────────────────────────────────┤
│ Modal Dialog (z-[101])          │
│ ┌───────────────────────────┐   │
│ │ HEADER (Fixed)            │   │  height: auto
│ │ Title + Close Button      │   │
│ ├───────────────────────────┤   │
│ │ CONTENT (Scrollable)      │   │  max-h-[calc(100vh-header-footer)]
│ │ Form Elements             │   │  overflow-y-auto
│ │ (can scroll)              │   │
│ ├───────────────────────────┤   │
│ │ FOOTER (Fixed)            │   │  height: auto
│ │ Cancel + Create Button    │   │
│ └───────────────────────────┘   │
└─────────────────────────────────┘
```

### Responsive Dimensions

| Viewport | Modal Width | Header Height | Content Max-Height | Footer Height |
|----------|-------------|---------------|--------------------|---------------|
| 375px (mobile) | calc(100% - 32px) | 60px | calc(100vh - 140px) | 80px |
| 768px (tablet) | max-w-lg (512px) | 70px | calc(100vh - 150px) | 90px |
| 1440px (desktop) | max-w-lg (512px) | 70px | calc(100vh - 160px) | 90px |

---

## Visual Design

### Header Section (Fixed)

**Elements:**
- Title: "Create New Team"
- Close button: X icon

**Styling:**
- Background: `bg-canvas`
- Border: `border-b border-hairline` (subtle divider)
- Padding: `p-6 md:p-8`
- Title: `t-heading-sm text-ink font-bold` (prevents wrapping)
- Close button: `p-2 text-mute hover:text-ink bg-surface-soft hover:bg-surface-dark`

**Behavior:**
- Always visible at top
- Close button: Closes modal, releases focus trap
- Title: Single line, no wrapping
- No shadow (clean appearance)

### Content Section (Scrollable)

**Elements:**
- Team Name input field
- Team Leader info (non-editable display)
- Member search input
- Member dropdown results
- Selected member tags
- Error messages

**Styling:**
- Background: `bg-canvas` (same as header for seamless scroll)
- Padding: `px-6 md:px-8` (horizontal padding)
- Vertical padding: `py-4` between elements
- Scrollbar: Custom styled (thin, muted gray, matches monogreen)
- Overflow: `overflow-y-auto` with `max-h-[calc(100vh - 160px)]`

**Scrollbar Styling:**
```css
.modal-content::-webkit-scrollbar {
  width: 6px;
}
.modal-content::-webkit-scrollbar-track {
  background: var(--color-surface-soft);
}
.modal-content::-webkit-scrollbar-thumb {
  background: var(--color-mute);
  border-radius: 3px;
}
.modal-content::-webkit-scrollbar-thumb:hover {
  background: var(--color-stone);
}
```

**Behavior:**
- Scrolls independently from header/footer
- Content doesn't jump when scrollbar appears/disappears
- Smooth scroll behavior on all browsers
- Touch-friendly on mobile (larger touch area for scroll)

### Footer Section (Fixed)

**Elements:**
- Cancel button (secondary, left)
- Create Team button (primary, right)

**Styling:**
- Background: `bg-canvas`
- Border: `border-t border-hairline` (subtle divider)
- Padding: `p-6 md:p-8`
- Layout: `flex gap-3 justify-end`
- Buttons: `min-h-11 px-6 py-2` (44px minimum touch target)

**Button Styling:**
- Cancel: `btn btn-secondary` (white bg, black text)
- Create Team: `btn btn-primary` (green bg, white text)
- Both: `transition-all duration-150` with hover effects

**Behavior:**
- Always visible at bottom
- Buttons always accessible without scrolling
- Form submit via "Create Team" button
- Cancel button closes modal without saving

---

## Responsive Behavior

### Mobile (375px - 767px)

```
┌─────────────────────┐
│ Create New Team [X] │  Header: 60px
├─────────────────────┤
│                     │
│  Team Name Input    │
│                     │
│  Team Leader        │
│                     │  Content: scrollable
│  Search & Select    │  height: calc(100vh - 140px)
│                     │
│  [Member Tags]      │
│                     │
├─────────────────────┤
│ [Cancel] [Create]   │  Footer: 80px
└─────────────────────┘
```

**Adjustments:**
- Modal: `w-full px-4` (full width with padding)
- Title: `t-heading-sm` (slightly smaller)
- Padding: `p-4` (tighter spacing)
- Content height: `calc(100vh - 140px)` (accounts for smaller header/footer)
- Buttons: `w-full` stacked or side-by-side based on space

### Tablet (768px - 1023px)

- Modal: `max-w-lg` (512px, centered)
- Standard padding: `p-6`
- Header height: ~70px
- Content height: `calc(100vh - 150px)`
- Buttons: Side-by-side layout

### Desktop (1024px+)

- Modal: `max-w-lg` (512px, centered)
- Standard padding: `p-8`
- Same proportions as tablet
- Generous spacing around modal

---

## Interaction Patterns

### Opening Modal
1. User clicks "Register Team" button
2. Modal overlay appears with backdrop `bg-black/70`
3. Focus shifts to modal (focus trap)
4. Header, content, and footer render
5. Content scrolls if height exceeds available space

### Scrolling Behavior
1. User scrolls in content area
2. Header and footer remain fixed
3. Scrollbar appears on right side (if needed)
4. Content scrolls smoothly without layout shift

### Closing Modal
1. User clicks Cancel button → Close without saving
2. User clicks X button → Close without saving
3. User presses Escape → Close without saving
4. User clicks backdrop → Close without saving
5. Focus returns to "Register Team" button

### Form Submission
1. User fills Team Name
2. User adds team members
3. User clicks "Create Team"
4. Form validates
5. Modal closes on success, shows error if validation fails

---

## Accessibility

- **ARIA:** `aria-modal="true"`, `aria-labelledby="modal-title"`
- **Focus Trap:** Tab cycles within modal only
- **Keyboard:** Escape closes, Tab navigates, Enter submits
- **Semantics:** `<dialog>` element for proper modal semantics
- **Contrast:** All text meets WCAG AA (4.5:1 minimum)
- **Touch Targets:** All interactive elements ≥44x44px
- **Scrollbar:** Visible and usable on all devices

---

## Error States

**Validation Errors:**
- Team name required → Error message below input
- Members required → Error message below member section
- Errors persist until fixed
- Error styling: `text-error bg-error/10 border border-error`

**Submission Error:**
- API failure → Error message in modal header (below title)
- User can retry or close modal

---

## Testing Checklist

- [ ] Modal opens/closes correctly
- [ ] Header stays fixed while scrolling
- [ ] Footer buttons always visible
- [ ] Scrollbar appears when content exceeds height
- [ ] No layout shift when scrollbar appears
- [ ] Responsive on 375px, 768px, 1024px, 1440px
- [ ] Touch-friendly scrolling on mobile
- [ ] Keyboard navigation works (Tab, Escape, Enter)
- [ ] Focus trap prevents tabbing outside modal
- [ ] Form validation displays errors correctly
- [ ] Buttons are accessible (min 44x44px)
- [ ] Color contrast meets WCAG AA

---

## Files to Create/Modify

**Create:**
- None (only modify existing Modal.tsx and TeamRegistrationForm.tsx)

**Modify:**
- `src/features/events/components/EventDashboard/Modal.tsx` (add scrollable content section)
- `src/app/globals.css` (add scrollbar styling)

---

## Success Criteria

✅ Modal header never scrolls off-screen  
✅ Modal footer buttons always visible  
✅ Form content scrolls smoothly when exceeding viewport  
✅ Mobile (375px) fully functional without horizontal scroll  
✅ No content cutoff on any viewport  
✅ Keyboard navigation and accessibility intact  
✅ WCAG AA color contrast maintained  
✅ Touch-friendly on all devices

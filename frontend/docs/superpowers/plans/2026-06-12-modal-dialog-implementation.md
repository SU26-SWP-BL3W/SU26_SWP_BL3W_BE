# Modal Dialog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement fixed header/footer modal with scrollable form content that works responsively across all viewports (375px-1440px).

**Architecture:** Modal uses CSS Grid with 3 sections (fixed header, scrollable content, fixed footer). Header and footer stay visible while only middle content scrolls. Custom scrollbar styling matches monogreen design system.

**Tech Stack:** React, Next.js, Tailwind CSS, TypeScript

---

## Files to Modify

- `src/features/events/components/EventDashboard/Modal.tsx` — Split into 3-section layout with scrollable content
- `src/app/globals.css` — Add custom scrollbar styling
- `src/features/events/components/EventDashboard/TeamRegistrationForm.tsx` — Minor spacing adjustments

---

## Task 1: Update Modal.tsx with 3-Section Layout

**Files:**
- Modify: `src/features/events/components/EventDashboard/Modal.tsx`

- [ ] **Step 1: Update Modal component structure**

Replace the entire Modal.tsx with the new 3-section layout:

```typescript
'use client';

import React, { useEffect } from 'react';
import { X } from 'lucide-react';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

export function Modal({ isOpen, onClose, title, children }: ModalProps) {
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };

    if (isOpen) {
      document.addEventListener('keydown', handleEscape);
      document.body.style.overflow = 'hidden';
    }

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div 
      className="fixed inset-0 z-[100] flex items-center justify-center p-4 md:p-0 bg-black/70" 
      onClick={onClose}
    >
      {/* Dialog with 3-section layout */}
      <dialog
        open={isOpen}
        className="relative z-[101] w-full max-w-lg bg-canvas border border-hairline rounded-sm shadow-2xl mx-4 flex flex-col h-auto max-h-[90vh]"
        aria-labelledby="modal-title"
        aria-modal="true"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header Section (Fixed) */}
        <div className="flex items-center justify-between gap-4 p-6 md:p-8 border-b border-hairline flex-shrink-0">
          <h2 id="modal-title" className="t-heading-sm text-ink m-0 flex-1">
            {title}
          </h2>
          <button
            onClick={onClose}
            className="p-2 text-mute hover:text-ink bg-surface-soft hover:bg-surface-dark rounded-sm transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary flex-shrink-0"
            aria-label="Close dialog"
          >
            <X size={20} aria-hidden="true" />
          </button>
        </div>

        {/* Content Section (Scrollable) */}
        <div className="flex-1 overflow-y-auto px-6 md:px-8 py-4 modal-content">
          {children}
        </div>

        {/* Footer Section (Fixed) */}
        <div className="flex gap-3 justify-end p-6 md:p-8 border-t border-hairline flex-shrink-0 bg-canvas">
          {/* Note: Buttons will be provided by TeamRegistrationForm */}
        </div>
      </dialog>
    </div>
  );
}
```

- [ ] **Step 2: Verify TypeScript compilation**

```bash
npm run build 2>&1 | grep -E "error|✓"
```

Expected: Build passes with no TypeScript errors

- [ ] **Step 3: Commit changes**

```bash
git add src/features/events/components/EventDashboard/Modal.tsx
git commit -m "refactor: update modal with 3-section layout (fixed header/footer, scrollable content)"
```

---

## Task 2: Add Custom Scrollbar Styling

**Files:**
- Modify: `src/app/globals.css`

- [ ] **Step 1: Add scrollbar CSS to globals.css**

Add this CSS before the closing style tag (find the end of the CSS file):

```css
/* Modal scrollbar styling */
.modal-content {
  scrollbar-color: var(--color-mute) var(--color-surface-soft);
  scrollbar-width: thin;
}

.modal-content::-webkit-scrollbar {
  width: 6px;
}

.modal-content::-webkit-scrollbar-track {
  background: var(--color-surface-soft);
  border-radius: 3px;
}

.modal-content::-webkit-scrollbar-thumb {
  background: var(--color-mute);
  border-radius: 3px;
}

.modal-content::-webkit-scrollbar-thumb:hover {
  background: var(--color-stone);
}

/* Smooth scroll behavior */
.modal-content {
  scroll-behavior: smooth;
}

/* Prevent layout shift when scrollbar appears */
@supports (scrollbar-gutter: stable) {
  .modal-content {
    scrollbar-gutter: stable;
  }
}
```

- [ ] **Step 2: Verify styling works**

```bash
npm run build 2>&1 | grep -E "error|✓"
```

Expected: Build passes with no CSS errors

- [ ] **Step 3: Commit scrollbar styling**

```bash
git add src/app/globals.css
git commit -m "style: add custom modal scrollbar styling matching monogreen design"
```

---

## Task 3: Update TeamRegistrationForm to Use Modal Footer

**Files:**
- Modify: `src/features/events/components/EventDashboard/TeamRegistrationForm.tsx`

- [ ] **Step 1: Wrap form in modal footer structure**

The form is already wrapped in a `<form>` tag. We need to ensure buttons are in the footer section. Update the form return to include the footer wrapper:

Find the form's return statement and update the button section. Look for the existing buttons around line 190-200 and ensure they're structured like this:

```typescript
// Inside the form's return, find the button section and replace with:
<div className="flex gap-3 justify-end p-6 md:p-8 border-t border-hairline flex-shrink-0 bg-canvas">
  <Button 
    variant="secondary" 
    onClick={() => setIsModalOpen(false)} 
    disabled={createTeamMutation.isPending}
    type="button"
  >
    Cancel
  </Button>
  <Button 
    type="submit" 
    isLoading={createTeamMutation.isPending}
    disabled={!teamName.trim() || selectedMembers.length === 0}
  >
    Create Team
  </Button>
</div>
```

This positions buttons in the modal's fixed footer section.

- [ ] **Step 2: Adjust form spacing**

The form content (inputs and member selection) should have proper spacing. Ensure there's adequate padding between form elements:

```typescript
// Update the main form content section to have proper spacing:
<div className="space-y-6">
  {submitError && (
    <div className="bg-error/10 border border-error rounded-sm p-3">
      <p className="t-body-sm text-error font-bold">{submitError}</p>
    </div>
  )}

  {/* Team Name */}
  <div>
    <label htmlFor="teamName" className="block t-body-strong text-ink mb-2">
      Team Name
    </label>
    {/* input field... */}
  </div>

  {/* Team Members */}
  <div>
    <label className="block t-body-strong text-ink mb-2">Add Members</label>
    {/* member selection... */}
  </div>
</div>
```

- [ ] **Step 3: Build and verify**

```bash
npm run build 2>&1 | grep -E "error|✓"
```

Expected: Build passes with no errors

- [ ] **Step 4: Commit form updates**

```bash
git add src/features/events/components/EventDashboard/TeamRegistrationForm.tsx
git commit -m "refactor: update form layout to work with new modal footer section"
```

---

## Task 4: Test Responsive Behavior on Mobile (375px)

**Files:**
- Test: Manual browser testing

- [ ] **Step 1: Start dev server**

```bash
npm run dev
```

Expected: Dev server starts on http://localhost:3000

- [ ] **Step 2: Open event dashboard**

Navigate to: `http://localhost:3000/events/evt-001`

- [ ] **Step 3: Open DevTools and set viewport to 375px**

- Press F12 to open DevTools
- Click device toggle (top left of DevTools)
- Select iPhone SE (375x812)

- [ ] **Step 4: Click "Register Team" button**

Verify:
- ✓ Modal appears centered
- ✓ Title "Create New Team" is fully visible (not cut off)
- ✓ Modal doesn't extend beyond viewport edges
- ✓ Close button [X] is visible and clickable

- [ ] **Step 5: Scroll form content**

Verify:
- ✓ Form content scrolls smoothly
- ✓ Header stays at top (doesn't scroll away)
- ✓ Cancel/Create buttons stay at bottom (always visible)
- ✓ Scrollbar appears on right side
- ✓ No horizontal scrolling

- [ ] **Step 6: Test form elements**

Verify:
- ✓ Team Name input is fully visible
- ✓ Team Leader section is visible
- ✓ Member search is accessible
- ✓ All form elements are tappable (44x44px minimum)

- [ ] **Step 7: Test closing modal**

Verify:
- ✓ Click Cancel button → modal closes
- ✓ Click [X] button → modal closes
- ✓ Click backdrop → modal closes
- ✓ Press Escape key → modal closes

---

## Task 5: Test Responsive Behavior on Tablet (768px)

**Files:**
- Test: Manual browser testing

- [ ] **Step 1: Adjust viewport to 768px**

DevTools → select iPad (768x1024)

- [ ] **Step 2: Click "Register Team" button**

Verify:
- ✓ Modal is `max-w-lg` (centered, wider than mobile)
- ✓ Title is fully visible
- ✓ More form content visible before scrolling needed
- ✓ Buttons visible at bottom

- [ ] **Step 3: Scroll and interact**

Verify:
- ✓ Content scrolls smoothly
- ✓ Header and footer remain fixed
- ✓ Scrollbar visible and functional
- ✓ All interactive elements accessible

---

## Task 6: Test Responsive Behavior on Desktop (1440px)

**Files:**
- Test: Manual browser testing

- [ ] **Step 1: Adjust viewport to 1440px**

DevTools → close device toggle (desktop mode)

- [ ] **Step 2: Click "Register Team" button**

Verify:
- ✓ Modal centered on screen
- ✓ `max-w-lg` width maintained
- ✓ Padding and spacing match spec
- ✓ Title and buttons clearly visible

- [ ] **Step 3: Interact with form**

Verify:
- ✓ Form content scrolls if needed
- ✓ All elements accessible without scrolling on typical screen
- ✓ Keyboard navigation works (Tab, Escape, Enter)
- ✓ Focus outline visible on all elements

---

## Task 7: Verify Accessibility and Keyboard Navigation

**Files:**
- Test: Manual accessibility testing

- [ ] **Step 1: Test keyboard-only navigation**

Without touching mouse:
- Press Tab repeatedly
- Verify focus cycles through: Title → Close button → Form inputs → Cancel → Create
- Verify focus is visible (outline around focused element)

- [ ] **Step 2: Test Escape key**

- Focus any element in modal
- Press Escape
- Verify modal closes

- [ ] **Step 3: Test Enter key on form**

- Fill in team name
- Press Tab to Create button
- Press Enter
- Verify form submits (or shows validation error if incomplete)

- [ ] **Step 4: Check screen reader (optional)**

Use VoiceOver (Mac) or NVDA (Windows):
- Modal title announced
- Close button labeled "Close dialog"
- Form fields have labels
- Error messages announced
- Button purposes clear

- [ ] **Step 5: Verify touch targets**

On mobile (375px):
- All buttons are at least 44x44px
- Form inputs are easily tappable
- Scrollbar is visible and usable

---

## Task 8: Final Cleanup and Verification

**Files:**
- All modified files

- [ ] **Step 1: Check for console errors**

Open DevTools → Console tab:
```bash
npm run dev
```

Navigate to modal and interact:
- Verify no red errors in console
- Verify no warnings about missing dependencies

- [ ] **Step 2: Run build**

```bash
npm run build 2>&1 | head -20
```

Expected: Build completes successfully with no TypeScript errors or warnings

- [ ] **Step 3: Verify all viewports one more time**

Quick visual check on 375px, 768px, 1440px:
- ✓ Header fixed
- ✓ Content scrollable
- ✓ Footer fixed
- ✓ No cutoff content
- ✓ Responsive spacing

- [ ] **Step 4: Final commit**

```bash
git log --oneline -5
git add -A
git commit -m "feat: implement fixed header/footer modal with scrollable content

- Modal has 3 sections: fixed header, scrollable content, fixed footer
- Works responsive across 375px-1440px viewports
- Custom scrollbar styling matches monogreen design
- Header and buttons always visible during scroll
- Keyboard navigation and accessibility verified
- All touch targets minimum 44x44px on mobile"
```

---

## Summary of Changes

**Modal.tsx:**
- Changed from simple overlay + dialog to 3-section grid layout
- Added fixed header section with title and close button
- Added scrollable content section with overflow handling
- Added fixed footer section for form buttons
- Proper z-index layering (z-[100] overlay, z-[101] dialog)

**globals.css:**
- Custom scrollbar styling (WebKit + Firefox)
- Scrollbar gutter support to prevent layout shift
- Smooth scroll behavior

**TeamRegistrationForm.tsx:**
- Buttons moved to modal footer section
- Form content properly spaced with `space-y-6`
- Maintains all validation and state management

---

## Testing Checklist

- [ ] Modal opens/closes on all viewports
- [ ] Header stays fixed while scrolling
- [ ] Footer buttons always visible
- [ ] No content cutoff on any viewport (375px, 768px, 1440px)
- [ ] Scrollbar appears when content exceeds height
- [ ] Keyboard navigation works (Tab, Escape, Enter)
- [ ] Touch targets ≥44x44px on mobile
- [ ] Form validation displays errors correctly
- [ ] No console errors or warnings
- [ ] Build passes with no TypeScript errors
- [ ] Accessibility features verified

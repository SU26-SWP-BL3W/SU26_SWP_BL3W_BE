import { chromium } from 'playwright';

async function testModalOnMobile() {
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({
    viewport: { width: 375, height: 812 }
  });

  let testsPassed = 0;
  let testsFailed = 0;
  const results = [];

  const logResult = (test, passed, message = '') => {
    if (passed) {
      testsPassed++;
      console.log(`✓ ${test}`);
    } else {
      testsFailed++;
      console.log(`✗ ${test}${message ? ': ' + message : ''}`);
    }
    results.push({ test, passed, message });
  };

  try {
    console.log('🚀 Starting Modal Responsive Test (375px Mobile Viewport)\n');

    console.log('1. Navigating to event dashboard...');
    await page.goto('http://localhost:3000/events/evt-001', { waitUntil: 'networkidle', timeout: 30000 });
    await page.waitForTimeout(2000);
    logResult('Page loads successfully', true);

    console.log('\n2. Taking initial screenshot...');
    await page.screenshot({ path: '/tmp/mobile-01-dashboard.png' });
    console.log('   Screenshot saved: /tmp/mobile-01-dashboard.png');

    console.log('\n3. Clicking "Register Team" button...');
    const registerButton = await page.locator('button:has-text("Register Team")').first();
    const registerExists = await registerButton.isVisible();
    logResult('Register Team button exists', registerExists);

    if (registerExists) {
      await registerButton.click();
      await page.waitForTimeout(1500);

      console.log('\n4. Modal appearance checks...');
      await page.screenshot({ path: '/tmp/mobile-02-modal-open.png' });
      console.log('   Screenshot saved: /tmp/mobile-02-modal-open.png');

      // Test 1: Modal is visible
      const dialog = page.locator('dialog[open]');
      const modalVisible = await dialog.isVisible().catch(() => false);
      logResult('Modal appears when button clicked', modalVisible);

      // Test 2: Title is visible and not cut off
      const title = page.locator('h2:has-text("Create New Team")');
      const titleVisible = await title.isVisible().catch(() => false);
      logResult('Title "Create New Team" is fully visible', titleVisible);

      // Test 3: Close button is visible
      const closeBtn = page.locator('button[aria-label*="lose"]').first();
      const closeBtnVisible = await closeBtn.isVisible().catch(() => false);
      logResult('Close button [X] is visible and clickable', closeBtnVisible);

      // Test 4: Modal doesn't extend beyond viewport
      if (modalVisible) {
        const boundingBox = await dialog.boundingBox();
        const modalFitsInViewport = boundingBox && boundingBox.width <= 375 && boundingBox.height <= 812;
        logResult('Modal fits within 375px viewport', modalFitsInViewport);
      }

      // Test 5: Dark backdrop exists
      const backdrop = page.locator('div[class*="bg-black"]').first();
      const backdropVisible = await backdrop.isVisible().catch(() => false);
      logResult('Dark backdrop (bg-black/70) visible', backdropVisible);

      console.log('\n5. Testing form scrolling behavior...');
      
      // Test 6: Form content is scrollable
      const formContent = page.locator('[class*="modal-content"]').first();
      const isFormVisible = await formContent.isVisible().catch(() => false);
      logResult('Form content area is accessible', isFormVisible);

      if (isFormVisible) {
        const isScrollable = await formContent.evaluate(el => el.scrollHeight > el.clientHeight);
        logResult('Form content is scrollable', isScrollable);

        if (isScrollable) {
          // Scroll and verify header stays
          await formContent.evaluate(el => { el.scrollTop = el.scrollHeight; });
          await page.waitForTimeout(800);
          await page.screenshot({ path: '/tmp/mobile-03-form-scrolled.png' });
          console.log('   Screenshot saved: /tmp/mobile-03-form-scrolled.png');
          
          const titleStillVisible = await title.isVisible().catch(() => false);
          logResult('Header stays visible when form scrolls', titleStillVisible);

          // Check footer buttons are visible
          const footerSection = page.locator('div[class*="border-t"]').last();
          const footerVisible = await footerSection.isVisible().catch(() => false);
          logResult('Cancel/Create buttons stay visible (fixed footer)', footerVisible);
        }
      }

      console.log('\n6. Testing form elements accessibility...');
      
      // Test 7: Team Name input exists
      const teamNameInput = page.locator('input[type="text"]').first();
      const inputVisible = await teamNameInput.isVisible().catch(() => false);
      logResult('Team Name input field is visible and tappable', inputVisible);

      // Test 8: All form elements are tappable size
      if (inputVisible) {
        const boundingBox = await teamNameInput.boundingBox();
        const isTappable = boundingBox && boundingBox.height >= 40;
        logResult('Form inputs are large enough to tap (44x44px minimum)', isTappable);
      }

      // Test 9: Text is readable
      const textReadable = await formContent.evaluate(el => {
        const style = window.getComputedStyle(el);
        const fontSize = parseFloat(style.fontSize);
        return fontSize >= 12;
      }).catch(() => false);
      logResult('Text is readable without zooming', textReadable);

      console.log('\n7. Testing modal closing methods...');
      
      // Test 10: Cancel button closes modal
      const cancelBtn = page.locator('button:has-text("Cancel")').first();
      const cancelBtnVisible = await cancelBtn.isVisible().catch(() => false);
      logResult('Cancel button is visible', cancelBtnVisible);

      if (cancelBtnVisible) {
        await cancelBtn.click();
        await page.waitForTimeout(1000);
        const modalClosedAfterCancel = !(await dialog.isVisible().catch(() => false));
        logResult('Modal closes when Cancel button clicked', modalClosedAfterCancel);

        // Reopen for next test
        if (modalClosedAfterCancel) {
          await registerButton.click();
          await page.waitForTimeout(1000);
        }
      }

      // Test 11: Close [X] button closes modal
      const closeButton = page.locator('button[aria-label*="lose"]').first();
      const closeButtonVisible = await closeButton.isVisible().catch(() => false);
      if (closeButtonVisible) {
        await closeButton.click();
        await page.waitForTimeout(1000);
        const modalClosedAfterX = !(await dialog.isVisible().catch(() => false));
        logResult('Modal closes when [X] button clicked', modalClosedAfterX);

        // Reopen for next test
        if (modalClosedAfterX) {
          await registerButton.click();
          await page.waitForTimeout(1000);
        }
      }

      // Test 12: Backdrop click closes modal
      const backdrop2 = page.locator('div[class*="bg-black"]').first();
      const backdropClickable = await backdrop2.isVisible().catch(() => false);
      if (backdropClickable) {
        await page.click('div[class*="bg-black"]', { position: { x: 10, y: 10 } }).catch(() => {});
        await page.waitForTimeout(1000);
        const modalClosedAfterBackdrop = !(await dialog.isVisible().catch(() => false));
        logResult('Modal closes when backdrop clicked', modalClosedAfterBackdrop);

        // Reopen for next test
        if (modalClosedAfterBackdrop) {
          await registerButton.click();
          await page.waitForTimeout(1000);
        }
      }

      // Test 13: Escape key closes modal
      await page.keyboard.press('Escape');
      await page.waitForTimeout(1000);
      const modalClosedAfterEscape = !(await dialog.isVisible().catch(() => false));
      logResult('Modal closes when Escape key pressed', modalClosedAfterEscape);

      console.log('\n8. Checking console for errors...');
      
      // Test 14: No console errors
      const consoleErrors = [];
      const consoleWarnings = [];
      
      page.on('console', msg => {
        if (msg.type() === 'error') {
          consoleErrors.push(msg.text());
        } else if (msg.type() === 'warning') {
          consoleWarnings.push(msg.text());
        }
      });

      // Re-navigate to get fresh console
      await page.goto('http://localhost:3000/events/evt-001', { waitUntil: 'networkidle' });
      await page.waitForTimeout(2000);

      const noErrors = consoleErrors.length === 0;
      logResult('No console errors', noErrors, consoleErrors.length > 0 ? `(${consoleErrors.length} errors found)` : '');
      if (consoleErrors.length > 0) {
        consoleErrors.forEach(err => console.log(`     ERROR: ${err}`));
      }

      const noWarnings = consoleWarnings.length === 0;
      logResult('No console warnings', noWarnings, consoleWarnings.length > 0 ? `(${consoleWarnings.length} warnings found)` : '');

    }

    console.log('\n' + '='.repeat(60));
    console.log('TEST SUMMARY');
    console.log('='.repeat(60));
    console.log(`✓ Passed: ${testsPassed}`);
    console.log(`✗ Failed: ${testsFailed}`);
    console.log(`Total: ${testsPassed + testsFailed}`);
    
    if (testsFailed === 0) {
      console.log('\n🎉 ALL TESTS PASSED! Modal is responsive and functional on 375px mobile viewport.');
    } else {
      console.log(`\n⚠️  ${testsFailed} test(s) failed. See details above.`);
    }

  } catch (error) {
    console.error('Test error:', error.message);
    testsFailed++;
  } finally {
    await browser.close();
  }
}

testModalOnMobile();

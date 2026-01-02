import { describe, expect, test, beforeEach, afterEach, vi } from 'vitest';
import toastManager from './toast-manager.mjs';

describe('ToastManager', () => {
  beforeEach(() => {
    // Clean up any existing toast containers
    document.body.innerHTML = '';
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('container management', () => {
    test('creates toast container when showing first toast', () => {
      expect(document.querySelector('.toast-container')).toBeNull();

      toastManager.show('Test message');

      const container = document.querySelector('.toast-container');
      expect(container).not.toBeNull();
      expect(container.parentElement).toBe(document.body);
    });

    test('reuses existing toast container', () => {
      toastManager.show('First message');
      toastManager.show('Second message');

      const containers = document.querySelectorAll('.toast-container');
      expect(containers.length).toBe(1);
    });
  });

  describe('show()', () => {
    test('creates toast with correct structure', () => {
      toastManager.show('Test message');

      const toast = document.querySelector('.toast');
      expect(toast).not.toBeNull();
      expect(toast.querySelector('.toast-icon')).not.toBeNull();
      expect(toast.querySelector('.toast-content')).not.toBeNull();
      expect(toast.querySelector('.toast-message')).not.toBeNull();
      expect(toast.querySelector('.toast-close')).not.toBeNull();
      expect(toast.querySelector('.toast-progress')).not.toBeNull();
      expect(toast.querySelector('.toast-progress-bar')).not.toBeNull();
    });

    test('displays the message text', () => {
      toastManager.show('Hello world');

      const message = document.querySelector('.toast-message');
      expect(message.textContent).toBe('Hello world');
    });

    test('creates success toast by default', () => {
      toastManager.show('Test message');

      const toast = document.querySelector('.toast');
      expect(toast.classList.contains('toast-success')).toBe(true);
      expect(toast.querySelector('.toast-icon').textContent).toBe('✓');
    });

    test('creates error toast when type is error', () => {
      toastManager.show('Error message', 'error');

      const toast = document.querySelector('.toast');
      expect(toast.classList.contains('toast-error')).toBe(true);
      expect(toast.querySelector('.toast-icon').textContent).toBe('✕');
    });

    test('renders link when provided', () => {
      toastManager.show('Test message', 'success', { url: '/test', text: 'View' });

      const link = document.querySelector('.toast-link');
      expect(link).not.toBeNull();
      expect(link.getAttribute('href')).toBe('/test');
      expect(link.textContent).toBe('View →');
    });

    test('does not render link when not provided', () => {
      toastManager.show('Test message');

      const link = document.querySelector('.toast-link');
      expect(link).toBeNull();
    });

    test('renders badge when provided', () => {
      toastManager.show('Test message', 'success', null, '12345');

      const badge = document.querySelector('.toast-badge');
      expect(badge).not.toBeNull();
      expect(badge.textContent).toBe('12345');
    });

    test('does not render badge when not provided', () => {
      toastManager.show('Test message');

      const badge = document.querySelector('.toast-badge');
      expect(badge).toBeNull();
    });

    test('can show multiple toasts', () => {
      toastManager.show('First');
      toastManager.show('Second');
      toastManager.show('Third');

      const toasts = document.querySelectorAll('.toast');
      expect(toasts.length).toBe(3);
    });
  });

  describe('success()', () => {
    test('creates success toast', () => {
      toastManager.success('Success message');

      const toast = document.querySelector('.toast');
      expect(toast.classList.contains('toast-success')).toBe(true);
    });

    test('passes link and badge parameters', () => {
      toastManager.success('Message', { url: '/link', text: 'Click' }, 'badge-text');

      expect(document.querySelector('.toast-link')).not.toBeNull();
      expect(document.querySelector('.toast-badge').textContent).toBe('badge-text');
    });
  });

  describe('error()', () => {
    test('creates error toast', () => {
      toastManager.error('Error message');

      const toast = document.querySelector('.toast');
      expect(toast.classList.contains('toast-error')).toBe(true);
    });

    test('passes link and badge parameters', () => {
      toastManager.error('Message', { url: '/link', text: 'Click' }, 'badge-text');

      expect(document.querySelector('.toast-link')).not.toBeNull();
      expect(document.querySelector('.toast-badge').textContent).toBe('badge-text');
    });
  });

  describe('auto-dismiss', () => {
    test('dismisses toast after 4 seconds', () => {
      toastManager.show('Test message');

      expect(document.querySelector('.toast')).not.toBeNull();

      vi.advanceTimersByTime(4000);

      const toast = document.querySelector('.toast');
      expect(toast.classList.contains('toast-hiding')).toBe(true);
    });

    test('does not dismiss before 4 seconds', () => {
      toastManager.show('Test message');

      vi.advanceTimersByTime(3999);

      const toast = document.querySelector('.toast');
      expect(toast.classList.contains('toast-hiding')).toBe(false);
    });
  });

  describe('close button', () => {
    test('dismisses toast when close button is clicked', () => {
      toastManager.show('Test message');

      const closeBtn = document.querySelector('.toast-close');
      closeBtn.click();

      const toast = document.querySelector('.toast');
      expect(toast.classList.contains('toast-hiding')).toBe(true);
    });

    test('has accessible aria-label', () => {
      toastManager.show('Test message');

      const closeBtn = document.querySelector('.toast-close');
      expect(closeBtn.getAttribute('aria-label')).toBe('Close');
    });
  });

  describe('hover pause', () => {
    test('pauses auto-dismiss timer on mouseenter', () => {
      toastManager.show('Test message');

      const toast = document.querySelector('.toast');
      const progressBar = toast.querySelector('.toast-progress-bar');

      // Advance halfway
      vi.advanceTimersByTime(2000);

      // Hover over toast
      toast.dispatchEvent(new MouseEvent('mouseenter'));

      // Advance past original timeout
      vi.advanceTimersByTime(3000);

      // Should not be hiding yet because timer was paused
      expect(toast.classList.contains('toast-hiding')).toBe(false);
      expect(progressBar.style.animationPlayState).toBe('paused');
    });

    test('resumes auto-dismiss timer on mouseleave', () => {
      toastManager.show('Test message');

      const toast = document.querySelector('.toast');
      const progressBar = toast.querySelector('.toast-progress-bar');

      // Advance halfway
      vi.advanceTimersByTime(2000);

      // Hover then leave
      toast.dispatchEvent(new MouseEvent('mouseenter'));
      vi.advanceTimersByTime(1000); // Wait while hovering
      toast.dispatchEvent(new MouseEvent('mouseleave'));

      expect(progressBar.style.animationPlayState).toBe('running');

      // Should dismiss after remaining time (2000ms)
      vi.advanceTimersByTime(2000);

      expect(toast.classList.contains('toast-hiding')).toBe(true);
    });
  });

  describe('progress bar', () => {
    test('sets animation duration to 4000ms', () => {
      toastManager.show('Test message');

      const progressBar = document.querySelector('.toast-progress-bar');
      expect(progressBar.style.animationDuration).toBe('4000ms');
    });
  });
});

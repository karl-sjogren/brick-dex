class ToastManager {
  #container = null;
  #duration = 4000;

  #getOrCreateContainer() {
    // Check if cached container is still in DOM
    if(this.#container && !this.#container.isConnected) {
      this.#container = null;
    }

    if(!this.#container) {
      this.#container = document.querySelector('.toast-container');
      if(!this.#container) {
        this.#container = document.createElement('div');
        this.#container.className = 'toast-container';
        document.body.appendChild(this.#container);
      }
    }
    return this.#container;
  }

  #dismiss(toast) {
    if(toast.classList.contains('toast-hiding')) return;
    toast.classList.add('toast-hiding');
    toast.addEventListener('animationend', () => toast.remove());
  }

  /**
   * Shows a toast notification.
   * @param {string} message - The message to display.
   * @param {'success' | 'error'} [type='success'] - The type of toast.
   * @param {{ url: string, text: string } | null} [link=null] - Optional action link.
   * @param {string | null} [badge=null] - Optional badge text displayed before the message.
   */
  show(message, type = 'success', link = null, badge = null) {
    const container = this.#getOrCreateContainer();

    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;

    const icon = type === 'success' ? '✓' : '✕';

    let linkHtml = '';
    if(link) {
      linkHtml = `<a href="${link.url}" class="toast-link">${link.text} →</a>`;
    }

    let badgeHtml = '';
    if(badge) {
      badgeHtml = `<span class="toast-badge">${badge}</span>`;
    }

    toast.innerHTML = `
      <div class="toast-icon">${icon}</div>
      <div class="toast-content">
        <p class="toast-message">${badgeHtml}${message}</p>
        ${linkHtml}
      </div>
      <button type="button" class="toast-close" aria-label="Close">×</button>
      <div class="toast-progress"><div class="toast-progress-bar"></div></div>
    `;

    container.appendChild(toast);

    // Start progress bar animation
    const progressBar = toast.querySelector('.toast-progress-bar');
    progressBar.style.animationDuration = `${this.#duration}ms`;

    // Track remaining time for hover pause
    let timeRemaining = this.#duration;
    let startTime = Date.now();
    let timeoutId = null;

    const startTimer = () => {
      startTime = Date.now();
      progressBar.style.animationPlayState = 'running';
      timeoutId = setTimeout(() => this.#dismiss(toast), timeRemaining);
    };

    const pauseTimer = () => {
      if(timeoutId) {
        clearTimeout(timeoutId);
        timeoutId = null;
      }
      timeRemaining -= Date.now() - startTime;
      progressBar.style.animationPlayState = 'paused';
    };

    // Pause on hover
    toast.addEventListener('mouseenter', pauseTimer);
    toast.addEventListener('mouseleave', startTimer);

    // Close button handler
    const closeBtn = toast.querySelector('.toast-close');
    closeBtn.addEventListener('click', () => {
      if(timeoutId) clearTimeout(timeoutId);
      this.#dismiss(toast);
    });

    // Start auto dismiss timer
    startTimer();
  }

  /**
   * Shows a success toast notification.
   * @param {string} message - The message to display.
   * @param {{ url: string, text: string } | null} [link=null] - Optional action link.
   * @param {string | null} [badge=null] - Optional badge text displayed before the message.
   */
  success(message, link = null, badge = null) {
    this.show(message, 'success', link, badge);
  }

  /**
   * Shows an error toast notification.
   * @param {string} message - The message to display.
   * @param {{ url: string, text: string } | null} [link=null] - Optional action link.
   * @param {string | null} [badge=null] - Optional badge text displayed before the message.
   */
  error(message, link = null, badge = null) {
    this.show(message, 'error', link, badge);
  }
}

export default new ToastManager();

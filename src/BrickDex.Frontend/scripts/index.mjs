import '../styles/main.scss';
import './elements/init.mjs';

// Hamburger menu toggle
const navbarToggler = document.querySelector('.navbar-toggler');
const navbarCollapse = document.querySelector('.navbar-collapse');

if(navbarToggler && navbarCollapse) {
  navbarToggler.addEventListener('click', () => {
    const isExpanded = navbarToggler.getAttribute('aria-expanded') === 'true';
    navbarToggler.setAttribute('aria-expanded', !isExpanded);
    navbarCollapse.classList.toggle('show');
  });

  // Close menu when clicking a nav link
  navbarCollapse.querySelectorAll('.nav-link').forEach((link) => {
    link.addEventListener('click', () => {
      navbarToggler.setAttribute('aria-expanded', 'false');
      navbarCollapse.classList.remove('show');
    });
  });
}

// Status dropdown functionality
const statusNames = {
  0: 'No Status',
  1: 'Ordered',
  2: 'In Storage',
  3: 'Building',
  4: 'Built',
  5: 'Sold'
};

const statusClasses = {
  0: 'none',
  1: 'ordered',
  2: 'instorage',
  3: 'building',
  4: 'built',
  5: 'sold'
};

function updateStatusDisplay(dropdown, newStatus) {
  const badge = dropdown.querySelector('.status-badge');
  const options = dropdown.querySelectorAll('.status-option');

  // Update badge text and class
  const statusClass = statusClasses[newStatus];
  badge.className = `status-badge status-${statusClass}`;
  badge.childNodes[0].textContent = statusNames[newStatus] + ' ';

  // Update selected state in menu
  options.forEach((opt) => {
    opt.classList.toggle('selected', parseInt(opt.dataset.status) === newStatus);
  });

  // Update data attribute
  dropdown.dataset.currentStatus = newStatus;

  // Update parent row/card status class
  const row = dropdown.closest('tr, .set-card');
  if(row) {
    // Remove old status classes
    row.classList.remove('has-status', 'status-none', 'status-ordered', 'status-instorage', 'status-building', 'status-built', 'status-sold');
    // Add new status class if not "None"
    if(newStatus !== 0) {
      row.classList.add('has-status', `status-${statusClass}`);
    }
  }
}

document.addEventListener('click', (e) => {
  const toggle = e.target.closest('.status-dropdown button.status-badge');
  if(toggle) {
    e.preventDefault();
    e.stopPropagation();
    const dropdown = toggle.closest('.status-dropdown');

    // Close other open dropdowns and remove their row highlights
    document.querySelectorAll('.status-dropdown.open').forEach((d) => {
      if(d !== dropdown) {
        d.classList.remove('open');
        const parentRow = d.closest('tr, .set-card');
        if(parentRow) {
          parentRow.classList.remove('has-open-dropdown');
        }
      }
    });

    dropdown.classList.toggle('open');

    // Add/remove class on parent row for z-index stacking
    const parentRow = dropdown.closest('tr, .set-card');
    if(parentRow) {
      parentRow.classList.toggle('has-open-dropdown', dropdown.classList.contains('open'));
    }
    return;
  }

  const option = e.target.closest('.status-option');
  if(option) {
    e.preventDefault();
    e.stopPropagation();

    const dropdown = option.closest('.status-dropdown');
    const userSetId = dropdown.dataset.userSetId;
    const newStatus = parseInt(option.dataset.status);
    const currentStatus = parseInt(dropdown.dataset.currentStatus);

    // Don't do anything if selecting the same status
    if(newStatus === currentStatus) {
      dropdown.classList.remove('open');
      const parentRow = dropdown.closest('tr, .set-card');
      if(parentRow) {
        parentRow.classList.remove('has-open-dropdown');
      }
      return;
    }

    // Optimistically update UI
    updateStatusDisplay(dropdown, newStatus);
    dropdown.classList.remove('open');
    const parentRow = dropdown.closest('tr, .set-card');
    if(parentRow) {
      parentRow.classList.remove('has-open-dropdown');
    }

    // Send API request
    fetch(`/api/usersets/${userSetId}/status`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ status: newStatus })
    })
      .then((res) => {
        if(!res.ok) {
          throw new Error('Failed to update status');
        }
        return res.json();
      })
      .catch(() => {
        // Rollback on error
        updateStatusDisplay(dropdown, currentStatus);
        alert('Failed to update status. Please try again.');
      });

    return;
  }

  // Close all dropdowns when clicking outside
  document.querySelectorAll('.status-dropdown.open').forEach((d) => {
    d.classList.remove('open');
    const parentRow = d.closest('tr, .set-card');
    if(parentRow) {
      parentRow.classList.remove('has-open-dropdown');
    }
  });
});
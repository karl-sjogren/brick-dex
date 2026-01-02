import '../styles/main.scss';
import './elements/init.mjs';
import toastManager from './utils/toast-manager.mjs';

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

// Add to collection/wishlist functionality
async function addSet(setNumber, isWishlist) {
  const endpoint = isWishlist ? '/api/usersets/wishlist' : '/api/usersets/collection';
  const targetName = isWishlist ? 'wishlist' : 'collection';

  try {
    const response = await fetch(endpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ setNumber })
    });

    const data = await response.json();

    if(!response.ok) {
      // Don't show set number badge for API errors since the message already includes it
      toastManager.error(data.error || `Failed to add set to ${targetName}`);
      return null;
    }

    toastManager.success(`${data.name} added to ${targetName}!`, {
      url: `/Sets/Details/${data.id}`,
      text: 'View set'
    }, setNumber);

    return data;
  } catch(error) {
    toastManager.error(`Failed to add set to ${targetName}. Please try again.`, null, setNumber);
    return null;
  }
}

document.addEventListener('click', async (e) => {
  const addCollectionBtn = e.target.closest('.btn-add-collection');
  if(addCollectionBtn) {
    e.preventDefault();
    const setNumber = addCollectionBtn.dataset.setNumber;
    addCollectionBtn.disabled = true;
    addCollectionBtn.textContent = 'Adding...';

    const result = await addSet(setNumber, false);
    if(result) {
      addCollectionBtn.textContent = 'Added!';
      addCollectionBtn.classList.remove('btn-primary');
      addCollectionBtn.classList.add('btn-success');
    } else {
      addCollectionBtn.disabled = false;
      addCollectionBtn.textContent = 'Add to Collection';
    }
    return;
  }

  const addWishlistBtn = e.target.closest('.btn-add-wishlist');
  if(addWishlistBtn) {
    e.preventDefault();
    const setNumber = addWishlistBtn.dataset.setNumber;
    addWishlistBtn.disabled = true;
    addWishlistBtn.textContent = 'Adding...';

    const result = await addSet(setNumber, true);
    if(result) {
      addWishlistBtn.textContent = 'Added!';
      addWishlistBtn.classList.remove('btn-secondary');
      addWishlistBtn.classList.add('btn-success');
    } else {
      addWishlistBtn.disabled = false;
      addWishlistBtn.textContent = 'Add to Wishlist';
    }
    return;
  }
});
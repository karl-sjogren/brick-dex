import toastManager from './utils/toast-manager.mjs';

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

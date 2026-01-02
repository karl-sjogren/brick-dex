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

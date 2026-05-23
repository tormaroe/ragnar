document.addEventListener('DOMContentLoaded', () => {
    // -------------------------------------------------------------
    // Hash Routing
    // -------------------------------------------------------------
    const menuItems = document.querySelectorAll('.menu-item');
    const sections = document.querySelectorAll('.content-section');
    const sidebar = document.getElementById('sidebar');
    const mobileNavToggle = document.getElementById('mobile-nav-toggle');

    function showSection(hash) {
        // Default to 'welcome' if no hash or invalid hash is provided
        const targetId = hash ? hash.replace('#', '') : 'welcome';
        let targetSection = document.getElementById(targetId);
        
        if (!targetSection) {
            // Fallback to welcome
            targetSection = document.getElementById('welcome');
        }

        // Hide all sections
        sections.forEach(section => {
            section.classList.remove('active');
        });

        // Show active section
        if (targetSection) {
            targetSection.classList.add('active');
            // Scroll content area back to top
            window.scrollTo({ top: 0, behavior: 'smooth' });
        }

        // Highlight active menu item
        menuItems.forEach(item => {
            const itemHash = item.getAttribute('href');
            if (itemHash === `#${targetId}` || (!hash && itemHash === '#welcome')) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });

        // Auto-close sidebar on mobile after clicking
        if (sidebar.classList.contains('open')) {
            sidebar.classList.remove('open');
            mobileNavToggle.classList.remove('open');
        }
    }

    // Listen to hash change
    window.addEventListener('hashchange', () => {
        showSection(window.location.hash);
    });

    // Run on initial load
    showSection(window.location.hash);

    // -------------------------------------------------------------
    // Mobile Nav Toggle
    // -------------------------------------------------------------
    if (mobileNavToggle && sidebar) {
        mobileNavToggle.addEventListener('click', (e) => {
            e.stopPropagation();
            sidebar.classList.toggle('open');
            mobileNavToggle.classList.toggle('open');
        });

        // Close sidebar when clicking outside of it on mobile
        document.addEventListener('click', (e) => {
            if (sidebar.classList.contains('open') && !sidebar.contains(e.target) && e.target !== mobileNavToggle) {
                sidebar.classList.remove('open');
                mobileNavToggle.classList.remove('open');
            }
        });
    }

    // -------------------------------------------------------------
    // Copy to Clipboard
    // -------------------------------------------------------------
    const copyButtons = document.querySelectorAll('.copy-btn');
    
    copyButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            const targetSelector = btn.getAttribute('data-clipboard-target');
            const targetElement = document.querySelector(targetSelector);
            
            if (targetElement) {
                // Get the text content, strip leading/trailing spaces if needed, but preserve structure
                const textToCopy = targetElement.textContent;
                
                navigator.clipboard.writeText(textToCopy).then(() => {
                    // Success feedback
                    const originalText = btn.textContent;
                    btn.textContent = 'Copied!';
                    btn.classList.add('copied');
                    
                    setTimeout(() => {
                        btn.textContent = originalText;
                        btn.classList.remove('copied');
                    }, 2000);
                }).catch(err => {
                    console.error('Failed to copy text: ', err);
                    btn.textContent = 'Failed';
                    setTimeout(() => {
                        btn.textContent = 'Copy';
                    }, 2000);
                });
            }
        });
    });
});

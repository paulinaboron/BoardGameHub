document.addEventListener('DOMContentLoaded', function () {
    const themeToggleBtn = document.getElementById('themeToggleBtn');
    const htmlElement = document.documentElement;
    const bodyElement = document.body;
    const storageKey = 'boardgamehub-theme';

    // Za³aduj zapisany motyw z localStorage
    const savedTheme = localStorage.getItem(storageKey) || 'light';
    if (savedTheme === 'dark') {
        bodyElement.classList.add('dark-theme');
        updateThemeIcon('light');
    } else {
        bodyElement.classList.remove('dark-theme');
        updateThemeIcon('dark');
    }

    // Toggle motywu po klikniêciu
    themeToggleBtn.addEventListener('click', function () {
        if (bodyElement.classList.contains('dark-theme')) {
            bodyElement.classList.remove('dark-theme');
            localStorage.setItem(storageKey, 'light');
            updateThemeIcon('dark');
        } else {
            bodyElement.classList.add('dark-theme');
            localStorage.setItem(storageKey, 'dark');
            updateThemeIcon('light');
        }
    });

    // Aktualizuj ikonê
    function updateThemeIcon(nextTheme) {
        if (nextTheme === 'light') {
            themeToggleBtn.innerHTML = '<i class="bi bi-sun-fill"></i>';
            themeToggleBtn.title = 'W³¹cz œwiat³y motyw';
        } else {
            themeToggleBtn.innerHTML = '<i class="bi bi-moon-fill"></i>';
            themeToggleBtn.title = 'W³¹cz ciemny motyw';
        }
    }
});
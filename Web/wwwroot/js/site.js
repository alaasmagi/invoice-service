(() => {
  const storageKey = "invoice-service-theme";
  const root = document.documentElement;

  function applyTheme(theme, persist) {
    const nextTheme = theme === "light" ? "light" : "dark";
    const isLight = nextTheme === "light";
    const toggle = document.getElementById("themeToggle");

    root.setAttribute("data-theme", nextTheme);
    root.setAttribute("data-bs-theme", nextTheme);

    if (toggle) {
      toggle.setAttribute("aria-checked", String(isLight));
      toggle.setAttribute("aria-label", `Switch to ${isLight ? "dark" : "light"} mode`);
      toggle.setAttribute("title", `Switch to ${isLight ? "dark" : "light"} mode`);
    }

    document.querySelectorAll("[data-theme-toggle-label]").forEach((label) => {
      label.textContent = isLight ? "Light" : "Dark";
    });

    if (persist) {
      try {
        localStorage.setItem(storageKey, nextTheme);
      } catch {
        // Ignore storage failures; the active document still receives the theme.
      }
    }
  }

  document.addEventListener("DOMContentLoaded", () => {
    applyTheme(root.getAttribute("data-theme"), false);

    document.getElementById("themeToggle")?.addEventListener("click", () => {
      applyTheme(root.getAttribute("data-theme") === "light" ? "dark" : "light", true);
    });
  });
})();

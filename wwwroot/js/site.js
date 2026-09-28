// All content is rendered on the server; this script only adds progressive enhancements
// (theme toggle, mobile navigation, active-section highlighting, section reveal, hero pointer glow).

const select = (selector) => document.querySelector(selector);
const selectAll = (selector) => [...document.querySelectorAll(selector)];

// A stored choice wins (applied before first paint by the inline script in the layout);
// otherwise the CSS follows prefers-color-scheme. The toggle stores an explicit choice.
const setupThemeToggle = () => {
  const button = select("[data-theme-toggle]");
  if (!button) return;

  const root = document.documentElement;
  const prefersLight = window.matchMedia("(prefers-color-scheme: light)");
  const currentTheme = () => root.dataset.theme || (prefersLight.matches ? "light" : "dark");

  const sync = () => {
    const theme = currentTheme();
    button.dataset.current = theme;
    button.setAttribute("aria-label", `Switch to ${theme === "dark" ? "light" : "dark"} theme`);
  };

  // keep the browser UI colour in step with an explicit choice
  const syncThemeColor = (theme) => {
    const color = theme === "dark" ? "#0c0f14" : "#f7f7f5";
    selectAll('meta[name="theme-color"]').forEach((meta) => meta.setAttribute("content", color));
  };

  button.addEventListener("click", () => {
    const next = currentTheme() === "dark" ? "light" : "dark";
    root.dataset.theme = next;
    syncThemeColor(next);
    try {
      localStorage.setItem("theme", next);
    } catch (error) {
      // storage unavailable: the choice still applies for this page view
    }
    sync();
  });
  prefersLight.addEventListener("change", sync);

  sync();
  button.hidden = false;
};

// Mobile menu: a disclosure button that moves focus into the menu when it opens, closes on
// Escape (returning focus to the button), and closes when focus or a click leaves it.
const setupNavigation = () => {
  const button = select("[data-nav-toggle]");
  const menu = select("[data-nav-menu]");
  if (!button || !menu) return;

  const links = [...menu.querySelectorAll("a")];
  const isOpen = () => button.getAttribute("aria-expanded") === "true";

  const setOpen = (open, { focusFirstLink = false } = {}) => {
    button.setAttribute("aria-expanded", String(open));
    menu.classList.toggle("is-open", open);
    if (open && focusFirstLink) links[0]?.focus();
  };

  button.hidden = false;
  button.addEventListener("click", () => setOpen(!isOpen(), { focusFirstLink: true }));

  links.forEach((link) => link.addEventListener("click", () => setOpen(false)));

  document.addEventListener("keydown", (event) => {
    if (event.key !== "Escape" || !isOpen()) return;
    setOpen(false);
    button.focus();
  });

  // focus moved somewhere other than the menu or its button: close the menu
  const closeIfOutside = (target) => {
    if (isOpen() && !menu.contains(target) && !button.contains(target)) setOpen(false);
  };
  document.addEventListener("focusin", (event) => closeIfOutside(event.target));
  document.addEventListener("pointerdown", (event) => closeIfOutside(event.target));
};

const setupActiveNavigation = () => {
  const links = selectAll("[data-nav-menu] a[href^='#']");
  const sections = links
    .map((link) => document.querySelector(link.getAttribute("href")))
    .filter(Boolean);

  if (!sections.length) return;

  const setActiveLink = (id) => {
    links.forEach((link) => {
      const isActive = link.getAttribute("href") === `#${id}`;
      link.classList.toggle("is-active", isActive);
      if (isActive) {
        link.setAttribute("aria-current", "true");
      } else {
        link.removeAttribute("aria-current");
      }
    });
  };

  const getHeaderHeight = () => select("[data-header]")?.offsetHeight ?? 0;

  const getActiveSection = () => {
    const viewportTop = getHeaderHeight();
    const viewportBottom = window.innerHeight;
    let activeSection = null;
    let largestVisibleArea = 0;

    for (const section of sections) {
      const rect = section.getBoundingClientRect();
      const visibleTop = Math.max(rect.top, viewportTop);
      const visibleBottom = Math.min(rect.bottom, viewportBottom);
      const visibleArea = Math.max(0, visibleBottom - visibleTop);

      if (visibleArea > largestVisibleArea) {
        activeSection = section;
        largestVisibleArea = visibleArea;
      }
    }

    return activeSection;
  };

  let updateQueued = false;
  const updateActiveSection = () => {
    updateQueued = false;
    const activeSection = getActiveSection();

    setActiveLink(activeSection?.id);
  };

  const queueActiveSectionUpdate = () => {
    if (updateQueued) return;
    updateQueued = true;
    window.requestAnimationFrame(updateActiveSection);
  };

  if ("IntersectionObserver" in window) {
    const observer = new IntersectionObserver(queueActiveSectionUpdate, {
      rootMargin: `-${getHeaderHeight()}px 0px -45% 0px`,
      threshold: [0, 0.3, 0.6]
    });

    sections.forEach((section) => observer.observe(section));
  }

  window.addEventListener("scroll", queueActiveSectionUpdate, { passive: true });
  window.addEventListener("resize", queueActiveSectionUpdate);
  queueActiveSectionUpdate();
};

// Sections that start below the fold fade up once as they scroll into view. Content is complete
// without JavaScript, and nothing moves for visitors who prefer reduced motion.
const setupReveal = () => {
  if (!("IntersectionObserver" in window)) return;
  if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

  const pending = selectAll("[data-reveal]").filter((section) => section.getBoundingClientRect().top > window.innerHeight);
  if (!pending.length) return;

  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (!entry.isIntersecting) return;
      entry.target.classList.remove("is-pending");
      observer.unobserve(entry.target);
    });
  });

  pending.forEach((section) => {
    section.classList.add("is-pending");
    observer.observe(section);
  });
};

// A soft accent glow follows the pointer across the hero and lights up the nearby grid. Only for
// devices with a precise hovering pointer, and never when reduced motion is requested; everyone
// else sees the static grid. Position updates are batched into one per animation frame.
const setupHeroGlow = () => {
  const hero = select("[data-pointer-glow]");
  if (!hero) return;

  const finePointer = window.matchMedia("(hover: hover) and (pointer: fine)");
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  let origin = null;
  let frame = 0;
  let x = 0;
  let y = 0;

  const measure = () => {
    const rect = hero.getBoundingClientRect();
    origin = { left: rect.left + window.scrollX, top: rect.top + window.scrollY };
  };

  const apply = () => {
    frame = 0;
    hero.style.setProperty("--pointer-x", `${x}px`);
    hero.style.setProperty("--pointer-y", `${y}px`);
  };

  hero.addEventListener("pointerenter", measure);
  window.addEventListener("resize", () => { origin = null; });

  hero.addEventListener("pointermove", (event) => {
    if (event.pointerType === "touch" || !finePointer.matches || reducedMotion.matches) return;
    if (!origin) measure();
    x = event.pageX - origin.left;
    y = event.pageY - origin.top;
    hero.classList.add("is-glowing");
    if (!frame) frame = window.requestAnimationFrame(apply);
  });

  hero.addEventListener("pointerleave", () => hero.classList.remove("is-glowing"));
};

setupThemeToggle();
setupNavigation();
setupActiveNavigation();
setupReveal();
setupHeroGlow();

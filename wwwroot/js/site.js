// All content is rendered on the server; this script only adds progressive enhancements
// (mobile navigation, active-section highlighting, image fitting).

const select = (selector) => document.querySelector(selector);
const selectAll = (selector) => [...document.querySelectorAll(selector)];

// Extreme aspect-ratio screenshots (very wide/short or very tall/narrow) crop badly under
// object-fit:cover. The server already decides this for images it could measure; images marked
// needs-fit-check are the ones it couldn't, so let their real dimensions decide at runtime.
const applyIntelligentFit = (image) => {
  const setFit = () => {
    const ratio = image.naturalWidth / image.naturalHeight;
    if (!Number.isFinite(ratio) || ratio === 0) return;
    if (ratio > 2.2 || ratio < 0.45) {
      image.classList.add("fit-contain");
    }
  };

  if (image.complete && image.naturalWidth) {
    setFit();
  } else {
    image.addEventListener("load", setFit, { once: true });
  }
};

const setupNavigation = () => {
  const button = select("[data-nav-toggle]");
  const menu = select("[data-nav-menu]");

  button?.addEventListener("click", () => {
    const expanded = button.getAttribute("aria-expanded") === "true";
    button.setAttribute("aria-expanded", String(!expanded));
    menu.classList.toggle("is-open", !expanded);
  });

  selectAll("[data-nav-menu] a").forEach((link) => {
    link.addEventListener("click", () => {
      button?.setAttribute("aria-expanded", "false");
      menu?.classList.remove("is-open");
    });
  });
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

setupNavigation();
setupActiveNavigation();
selectAll("img.needs-fit-check").forEach(applyIntelligentFit);

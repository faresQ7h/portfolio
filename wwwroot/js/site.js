// All content is rendered on the server; this script only adds progressive enhancements
// (theme toggle, mobile navigation, active-section highlighting, section reveal, hero pointer glow,
// scroll trace).

const select = (selector) => document.querySelector(selector);
const selectAll = (selector) => [...document.querySelectorAll(selector)];

// Dark is the default. A stored choice (applied before first paint by the inline script in the
// layout) wins; the toggle stores an explicit choice.
const setupThemeToggle = () => {
  const button = select("[data-theme-toggle]");
  if (!button) return;

  const root = document.documentElement;
  const currentTheme = () => (root.dataset.theme === "light" ? "light" : "dark");

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
      localStorage.setItem("faresm-theme", next);
    } catch (error) {
      // storage unavailable: the choice still applies for this page view
    }
    sync();
  });

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

// From the About section down, a faint gold trace grows behind the content as the page scrolls:
// straight down (|), a 45° step (\), and at every section divider a horizontal run (_) along the
// divider to a new column, like a routed circuit trace. It is drawn from the real section
// positions, sits behind the text at low opacity, and is fully drawn without motion when reduced
// motion is requested.
const setupScrollTrace = () => {
  const main = select("[data-trace]");
  const sections = selectAll("[data-trace] > section:not(.hero)");
  if (!main || sections.length < 2 || !("ResizeObserver" in window)) return;

  const SVG_NS = "http://www.w3.org/2000/svg";
  const make = (name, attributes = {}) => {
    const element = document.createElementNS(SVG_NS, name);
    Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
    return element;
  };

  const svg = make("svg", { class: "scroll-trace", "aria-hidden": "true", focusable: "false" });
  const path = make("path", { class: "trace-path" });
  const tip = make("circle", { class: "trace-tip", r: "2.5" });
  svg.append(path, tip);
  main.prepend(svg);

  // column for each section, as a fraction of the content width (alternating across the page)
  const COLUMNS = [0.06, 0.64, 0.24, 0.82, 0.4, 0.7];
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  let vias = [];
  let geometry = null;
  let frame = 0;

  const layout = () => {
    const width = main.offsetWidth;
    svg.setAttribute("viewBox", `0 0 ${width} ${main.offsetHeight}`);

    const content = select("[data-trace] > section:not(.hero) .container");
    const left = content.offsetLeft;
    const span = content.offsetWidth;
    const step = Math.round(Math.min(span * 0.1, 120));

    // Each segment knows the scroll "reach" range over which it draws. Vertical and diagonal runs
    // draw as the reach passes their own y-range; each horizontal run draws within a short band
    // around its divider, so the tip stays close to the reading position.
    const points = [];
    const segments = [];
    let x = Math.max(8, left - 24);
    let y = sections[0].offsetTop;
    points.push([x, y]);

    const lineTo = (nextX, nextY, reachFrom, reachTo) => {
      const length = Math.hypot(nextX - x, nextY - y);
      if (length < 0.5) return;
      segments.push({ length, reachFrom, reachTo });
      x = nextX;
      y = nextY;
      points.push([x, y]);
    };

    const viaPoints = [];
    sections.forEach((section, index) => {
      const top = section.offsetTop;
      const bottom = index < sections.length - 1 ? sections[index + 1].offsetTop : top + section.offsetHeight - 48;
      const column = Math.round(left + COLUMNS[index % COLUMNS.length] * span);
      const band = Math.min(140, Math.abs(column - x) * 0.4);

      // _ along the divider to this section's column
      lineTo(column, top, top - band / 2, top + band / 2);
      viaPoints.push({ x: column, y: top });

      // | then \ then | down to the next divider
      const height = bottom - top;
      const bend = top + Math.max(height * 0.32, 24);
      const diagonal = Math.min(step, Math.max(0, (height - 48) / 2));
      lineTo(column, bend, top + band / 2, bend);
      lineTo(column + diagonal, bend + diagonal, bend, bend + diagonal);
      lineTo(column + diagonal, bottom, bend + diagonal, bottom);
    });

    // keep each segment's reach range ordered even where bands overlap neighbours
    let floor = -Infinity;
    segments.forEach((segment) => {
      segment.reachFrom = Math.max(segment.reachFrom, floor);
      segment.reachTo = Math.max(segment.reachTo, segment.reachFrom + 1);
      floor = segment.reachTo;
    });

    path.setAttribute("d", points.map(([px, py], i) => `${i ? "L" : "M"}${px} ${py}`).join(" "));
    const total = segments.reduce((sum, segment) => sum + segment.length, 0);
    path.style.strokeDasharray = `${total} ${total}`;

    vias.forEach((via) => via.element.remove());
    vias = viaPoints.map((point) => {
      const via = make("circle", { class: "trace-via", cx: point.x, cy: point.y, r: "2.5" });
      svg.insertBefore(via, tip);
      return { element: via, y: point.y };
    });

    geometry = { segments, total, mainTop: main.getBoundingClientRect().top + window.scrollY };
  };

  const update = () => {
    frame = 0;
    if (!geometry) return;

    // The tip follows a point 60% of the way down the viewport; during the last screen of
    // scrolling that point slides to the bottom edge, so the trace is complete at the page end.
    const maxScroll = document.documentElement.scrollHeight - window.innerHeight;
    const nearEnd = Math.min(Math.max((window.scrollY - (maxScroll - window.innerHeight)) / window.innerHeight, 0), 1);
    const reach = reducedMotion.matches
      ? Infinity
      : window.scrollY + window.innerHeight * (0.6 + 0.4 * nearEnd) - geometry.mainTop;

    let drawn = 0;
    for (const segment of geometry.segments) {
      const t = Math.min(Math.max((reach - segment.reachFrom) / (segment.reachTo - segment.reachFrom), 0), 1);
      drawn += segment.length * t;
      if (t < 1) break;
    }

    path.style.strokeDashoffset = geometry.total - drawn;
    if (drawn > 0 && drawn < geometry.total) {
      const point = path.getPointAtLength(drawn);
      tip.setAttribute("cx", point.x);
      tip.setAttribute("cy", point.y);
      tip.classList.add("is-visible");
    } else {
      tip.classList.remove("is-visible");
    }
    vias.forEach((via) => via.element.classList.toggle("is-reached", reach >= via.y));
  };

  const queueUpdate = () => {
    if (!frame) frame = window.requestAnimationFrame(update);
  };

  const relayout = () => {
    layout();
    queueUpdate();
  };

  relayout();
  window.addEventListener("scroll", queueUpdate, { passive: true });
  new ResizeObserver(relayout).observe(main);
  document.fonts?.ready.then(relayout);
  reducedMotion.addEventListener?.("change", queueUpdate);
};

setupThemeToggle();
setupNavigation();
setupActiveNavigation();
setupReveal();
setupHeroGlow();
setupScrollTrace();

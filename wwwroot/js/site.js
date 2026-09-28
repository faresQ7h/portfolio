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
      localStorage.setItem("theme", next);
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

// From the About section down, a gold trace grows along the left margin as the page scrolls,
// like a `git log --graph` line: each section's kicker is a node that lights up (with a short
// branch toward the heading) once the trace reaches it. Decorative only, drawn from the real
// section positions, and fully drawn without motion when reduced motion is requested.
const setupScrollTrace = () => {
  const main = select("[data-trace]");
  const anchors = selectAll("[data-trace-node]");
  if (!main || anchors.length < 2 || !("ResizeObserver" in window)) return;

  const SVG_NS = "http://www.w3.org/2000/svg";
  const make = (name, attributes = {}) => {
    const element = document.createElementNS(SVG_NS, name);
    Object.entries(attributes).forEach(([key, value]) => element.setAttribute(key, value));
    return element;
  };

  const svg = make("svg", { class: "scroll-trace", "aria-hidden": "true", focusable: "false" });
  const track = make("line", { class: "trace-track" });
  const glow = make("line", { class: "trace-glow" });
  const progress = make("line", { class: "trace-progress" });
  const head = make("circle", { class: "trace-head", r: "3.5" });
  const nodes = anchors.map(() => ({
    branch: make("line", { class: "trace-branch" }),
    dot: make("circle", { class: "trace-node", r: "4.5" })
  }));
  svg.append(track, glow, progress, ...nodes.flatMap((node) => [node.branch, node.dot]), head);
  main.prepend(svg);

  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
  let geometry = null;
  let frame = 0;

  // offsetLeft/offsetTop ignore transforms, so sections still sliding in (reveal) don't skew the layout
  const offsetWithin = (element, ancestor) => {
    let x = 0;
    let y = 0;
    for (let node = element; node && node !== ancestor; node = node.offsetParent) {
      x += node.offsetLeft;
      y += node.offsetTop;
    }
    return { x, y };
  };

  const setLine = (line, x1, y1, x2, y2) => {
    line.setAttribute("x1", x1);
    line.setAttribute("y1", y1);
    line.setAttribute("x2", x2);
    line.setAttribute("y2", y2);
  };

  const layout = () => {
    svg.setAttribute("viewBox", `0 0 ${main.offsetWidth} ${main.offsetHeight}`);

    const points = anchors.map((anchor) => {
      const offset = offsetWithin(anchor, main);
      return { left: offset.x, y: offset.y + anchor.offsetHeight / 2 };
    });
    const contentLeft = points[0].left;
    const x = Math.round(Math.max(contentLeft - 36, contentLeft / 2));
    const top = points[0].y;
    const length = points[points.length - 1].y - top;

    [track, glow, progress].forEach((line) => setLine(line, x, top, x, top + length));
    [glow, progress].forEach((line) => { line.style.strokeDasharray = `${length} ${length}`; });

    points.forEach((point, index) => {
      const { branch, dot } = nodes[index];
      dot.setAttribute("cx", x);
      dot.setAttribute("cy", point.y);
      const branchLength = Math.max(0, point.left - 12 - (x + 8));
      setLine(branch, x + 8, point.y, x + 8 + branchLength, point.y);
      branch.style.setProperty("--branch-length", branchLength);
      branch.style.display = branchLength < 8 ? "none" : "";
    });
    head.setAttribute("cx", x);

    geometry = {
      top,
      length,
      mainTop: main.getBoundingClientRect().top + window.scrollY,
      nodeYs: points.map((point) => point.y)
    };
  };

  const update = () => {
    frame = 0;
    if (!geometry) return;

    // The trace tip follows a point 60% of the way down the viewport; during the last screen of
    // scrolling that point slides to the bottom edge, so the trace is complete at the page end.
    const maxScroll = document.documentElement.scrollHeight - window.innerHeight;
    const nearEnd = Math.min(Math.max((window.scrollY - (maxScroll - window.innerHeight)) / window.innerHeight, 0), 1);
    const viewportFraction = 0.6 + 0.4 * nearEnd;
    const reach = reducedMotion.matches
      ? Infinity
      : window.scrollY + window.innerHeight * viewportFraction - geometry.mainTop;
    const drawn = Math.min(Math.max(reach - geometry.top, 0), geometry.length);

    [glow, progress].forEach((line) => { line.style.strokeDashoffset = geometry.length - drawn; });
    head.setAttribute("cy", geometry.top + drawn);
    head.classList.toggle("is-visible", drawn > 0 && drawn < geometry.length);
    geometry.nodeYs.forEach((y, index) => {
      const reached = reach >= y - 1;
      nodes[index].dot.classList.toggle("is-reached", reached);
      nodes[index].branch.classList.toggle("is-reached", reached);
    });
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

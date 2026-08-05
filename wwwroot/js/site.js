const state = {
  profile: null,
  projects: []
};

const select = (selector) => document.querySelector(selector);
const selectAll = (selector) => [...document.querySelectorAll(selector)];

const createElement = (tag, className, text) => {
  const element = document.createElement(tag);
  if (className) element.className = className;
  if (text) element.textContent = text;
  return element;
};

const createLink = (href, text, className, external = false) => {
  const link = createElement("a", className, text);
  link.href = href;
  if (external) {
    link.target = "_blank";
    link.rel = "noopener";
  }
  return link;
};

const createIconLink = (href, iconSrc, label) => {
  const link = createElement("a", "icon-button");
  link.href = href;
  link.target = "_blank";
  link.rel = "noopener";
  link.setAttribute("aria-label", label);

  const icon = document.createElement("img");
  icon.src = iconSrc;
  icon.alt = "";
  icon.width = 20;
  icon.height = 20;
  icon.decoding = "async";
  link.append(icon);

  return link;
};

const renderProfile = (profile) => {
  const { personal } = profile;

  select("[data-profile-name]").textContent = personal.name;
  select("[data-profile-headline]").textContent = personal.headline;
  select("[data-profile-summary]").textContent = personal.summary;
  select("[data-profile-location]").textContent = personal.location;
  select(".avatar").src = personal.avatarUrl;

  const actions = select("[data-profile-actions]");
  actions.replaceChildren(
    createIconLink(personal.githubUrl, "/assets/icons/githupLogo.svg", "GitHub profile"),
    createIconLink(personal.linkedInUrl, "/assets/icons/linkedinLogo.svg", "LinkedIn profile")
  );

  select("[data-stats]").replaceChildren(...profile.stats.map((stat) => {
    const item = createElement("article", "stat-card");
    item.append(createElement("strong", "", stat.value), createElement("span", "", stat.label));
    return item;
  }));

  select("[data-about]").replaceChildren(...profile.about.map((paragraph) => createElement("p", "", paragraph)));

  renderSkills(profile.skills);
  renderTimeline("[data-education]", profile.education, "school", "program");
  renderTimeline("[data-experience]", profile.experience, "title", "organization");
  renderLearning(profile.currentlyLearning);
  renderContactLinks(personal);
};

// Maps a skill/tag label to its sprite symbol id (see the hidden <svg><defs> sprite in index.html).
// Only technologies with a real, recognizable brand mark get an icon; concepts stay text-only.
const SKILL_ICONS = {
  "C": "icon-c",
  "C++": "icon-cplusplus",
  "C#": "icon-csharp",
  "Python": "icon-python",
  "SQL": "icon-sql",
  "JavaScript": "icon-javascript",
  "HTML": "icon-html5",
  "CSS": "icon-css3",
  "Bash": "icon-bash",
  ".NET": "icon-dotnet",
  "ASP.NET": "icon-dotnet",
  "PostgreSQL": "icon-postgresql",
  "Linux Mint": "icon-linuxmint",
  "Linux/Unix": "icon-linux",
  "CCNA preparation": "icon-cisco",
  "Git": "icon-git",
  "GitHub": "icon-github",
  "Visual Studio": "icon-visualstudio",
  "Visual Studio Code": "icon-vscode"
};

const LEARNING_ICONS = {
  "Docker": "icon-docker",
  "Cisco CCNA": "icon-cisco",
  "Cloud Computing": "icon-cloud"
};

const SVG_NS = "http://www.w3.org/2000/svg";

const createSpriteIcon = (symbolId, className) => {
  const svg = document.createElementNS(SVG_NS, "svg");
  svg.setAttribute("class", className);
  svg.setAttribute("aria-hidden", "true");
  svg.setAttribute("focusable", "false");
  const use = document.createElementNS(SVG_NS, "use");
  use.setAttribute("href", `#${symbolId}`);
  svg.append(use);
  return svg;
};

const createSkillTag = (label) => {
  const tag = createElement("span", "tag");
  const symbolId = SKILL_ICONS[label];
  if (symbolId) tag.append(createSpriteIcon(symbolId, "tag-icon"));
  tag.append(document.createTextNode(label));
  return tag;
};

const renderSkills = (skills) => {
  const cards = skills.map((category) => {
    const card = createElement("article", "skill-card");
    const header = createElement("div", "skill-card-header");
    header.append(createElement("span", "skill-icon", category.icon), createElement("h3", "", category.name));
    const list = createElement("div", "tag-row");
    list.append(...category.items.map(createSkillTag));
    card.append(header, list);
    return card;
  });

  select("[data-skills]").replaceChildren(...cards);
};

const renderTimeline = (selector, items, titleKey, subtitleKey) => {
  const blocks = items.map((item) => {
    const article = createElement("article", "timeline-item");
    article.append(createElement("span", "timeline-period", item.period));
    article.append(createElement("h3", "", item[titleKey]));
    article.append(createElement("p", "timeline-subtitle", item[subtitleKey]));
    if (typeof item.details === "string") {
      article.append(createElement("p", "", item.details));
    }

    const source = item.highlights || item.details || [];
    if (Array.isArray(source)) {
      const list = createElement("ul", "clean-list");
      list.append(...source.map((entry) => createElement("li", "", entry)));
      article.append(list);
    }

    return article;
  });

  select(selector).replaceChildren(...blocks);
};

const renderLearning = (items) => {
  const cards = items.map((item) => {
    const card = createElement("article", "learning-card");
    const heading = createElement("h3", "", item.name);
    const symbolId = LEARNING_ICONS[item.name];
    if (symbolId) {
      heading.classList.add("learning-card-title");
      heading.prepend(createSpriteIcon(symbolId, "learning-icon"));
    }
    card.append(heading, createElement("p", "", item.focus));
    return card;
  });

  select("[data-learning]").replaceChildren(...cards);
};

const renderContactLinks = (personal) => {
  const container = select('[data-contact-links]');
  if (!container) return;

  const items = [
    { href: `mailto:${personal.email}`, label: personal.email, icon: '/assets/icons/gmailLogo.svg', external: false },
    { href: personal.githubUrl, label: 'GitHub', icon: '/assets/icons/githupLogo.svg', external: true },
    { href: personal.linkedInUrl, label: 'LinkedIn', icon: '/assets/icons/linkedinLogo.svg', external: true }
  ];

  const nodes = items.map((it) => {
    const a = createElement('a', 'connect-item');
    a.href = it.href;
    if (it.external) {
      a.target = '_blank';
      a.rel = 'noopener';
    }

    const img = document.createElement('img');
    img.src = it.icon;
    img.alt = it.label;
    img.className = 'connect-icon';
    img.width = 20;
    img.height = 20;
    img.decoding = 'async';

    const label = createElement('span', 'connect-label', it.label);
    a.append(img, label);
    return a;
  });

  container.replaceChildren(...nodes);
};

// Extreme aspect-ratio screenshots (very wide/short or very tall/narrow) crop badly
// under object-fit:cover, so let the real image dimensions decide the fit at runtime
// instead of hardcoding per-file exceptions.
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

const fetchAssetFolder = async (folderPath) => {
  // folderPath expected like '/assets/minishell/'
  if (!folderPath || !folderPath.startsWith('/assets/')) return [];
  const folder = folderPath.replace(/^\/assets\//, '').replace(/\/$/, '');
  try {
    const res = await fetch(`/api/assets/${encodeURIComponent(folder)}`);
    if (!res.ok) return [];
    const json = await res.json();
    return json.files || [];
  } catch (e) {
    return [];
  }
};

const renderProjects = async (projects) => {
  const featured = projects.filter((project) => project.featured);
  const secondary = projects.filter((project) => !project.featured);
  const orderedProjects = [...featured, ...secondary];

  const cards = await Promise.all(orderedProjects.map(async (project) => {
    const card = createElement("article", "project-card");
    const media = createElement("a", "project-media");
    media.href = `/projects/${project.slug}`;
    media.setAttribute("aria-label", `View ${project.name} details`);

    let thumb = project.screenshots && project.screenshots[0];
    if (thumb && thumb.endsWith('/')) {
      const files = await fetchAssetFolder(thumb);
      thumb = (files && files[0]) || "/assets/projects/placeholder.svg";
    }

    const image = document.createElement("img");
    image.className = "project-shot";
    image.src = thumb || "/assets/projects/placeholder.svg";
    image.alt = `${project.name} project preview`;
    image.loading = "lazy";
    image.decoding = "async";
    image.width = 720;
    image.height = 420;
    applyIntelligentFit(image);

    // if the real screenshot fails to load for any reason, fall back to the project's
    // illustration (or the generic placeholder) instead of a broken image
    image.addEventListener("error", () => {
      const fallback = project.fallbackImage || "/assets/projects/placeholder.svg";
      if (image.src.endsWith(fallback)) return;
      image.src = fallback;
      image.classList.add("fit-contain");
    }, { once: true });

    media.append(image);

    const body = createElement("div", "project-card-body");
    const meta = createElement("div", "project-meta");
    meta.append(createElement("span", "status", project.status), createElement("span", "", project.technologies.slice(0, 3).join(" / ")));

    const tags = createElement("div", "tag-row");
    tags.append(...(project.skillsDemonstrated || []).slice(0, 4).map((skill) => createElement("span", "tag", skill)));

    const actions = createElement("div", "card-actions");
    actions.append(createLink(`/projects/${project.slug}`, "Details", "button button-secondary"));
    if (project.githubUrl) {
      actions.append(createGithubButton(project.githubUrl, "button button-ghost", "GitHub", `View ${project.name} on GitHub`));
    }

    body.append(meta, createElement("h3", "", project.name), createElement("p", "", project.description), tags, actions);
    card.append(media, body);
    return card;
  }));

  select("[data-projects]").replaceChildren(...cards);
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

  if (!sections.length || !("IntersectionObserver" in window)) return;

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

  const observer = new IntersectionObserver((entries) => {
    const visible = entries
      .filter((entry) => entry.isIntersecting)
      .sort((a, b) => b.intersectionRatio - a.intersectionRatio)[0];

    if (visible?.target.id) {
      setActiveLink(visible.target.id);
    }
  }, {
    rootMargin: "-22% 0px -58% 0px",
    threshold: [0.12, 0.3, 0.6]
  });

  sections.forEach((section) => observer.observe(section));
};

const boot = async () => {
  setupNavigation();
  setupActiveNavigation();
  selectAll("[data-year]").forEach((element) => {
    element.textContent = new Date().getFullYear();
  });

  const [profileResponse, projectsResponse] = await Promise.all([
    fetch("/api/profile"),
    fetch("/api/projects")
  ]);

  state.profile = await profileResponse.json();
  state.projects = await projectsResponse.json();

  renderProfile(state.profile);
  await renderProjects(state.projects);

  // On initial load with a URL hash (e.g. arriving from a project page via
  // "Back to projects"), the browser tries to jump to that section before this
  // JS has rendered any content, so it lands on a much shorter page and ends up
  // in the wrong place. Re-run the scroll now that the page has its real height.
  if (window.location.hash) {
    select(window.location.hash)?.scrollIntoView({ behavior: "instant", block: "start" });
  }
};

boot().catch((error) => {
  console.error(error);
  const main = select("main");
  main?.prepend(createElement("p", "load-error", "Portfolio data could not be loaded."));
});

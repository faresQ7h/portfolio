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

const getSlug = () => {
  const parts = window.location.pathname.split("/").filter(Boolean);
  return parts[0] === "projects" ? parts[1] : new URLSearchParams(window.location.search).get("slug");
};

// Builds only the detail-page sections that actually have content, so an in-development
// project with a short writeup doesn't render empty "Key Features" / "Lessons Learned" cards.
const renderDetailSections = (project) => {
  const container = select("[data-detail-sections]");
  if (!container) return;

  const blocks = [
    ["Overview", project.overview],
    ["Architecture", project.architecture],
    ["Key Features", project.keyFeatures],
    ["Technical Challenges", project.challenges],
    ["Engineering Decisions", project.engineeringDecisions],
    ["Technologies Used", project.technologyNotes],
    ["Lessons Learned", project.lessonsLearned]
  ];

  const articles = blocks
    .filter(([, content]) => Array.isArray(content) ? content.length > 0 : Boolean(content && content.trim()))
    .map(([title, content]) => {
      const article = createElement("article", "detail-block");
      article.append(createElement("h2", "", title));
      if (Array.isArray(content)) {
        const list = createElement("ul", "clean-list");
        list.append(...content.map((item) => createElement("li", "", item)));
        article.append(list);
      } else {
        article.append(createElement("p", "", content));
      }
      return article;
    });

  container.replaceChildren(...articles);
};

const renderProject = (project) => {
  document.title = `${project.name} | Fares Mohamed`;
  select("meta[name='description']").setAttribute("content", project.description);
  select("[data-project-status]").textContent = project.status;
  select("[data-project-name]").textContent = project.name;
  select("[data-project-description]").textContent = project.description;
  renderDetailSections(project);

  const image = select("[data-project-image]");
  const gallery = select("[data-project-gallery]");
  const galleryHero = select(".project-gallery-hero");
  const prevButton = select("[data-gallery-prev]");
  const nextButton = select("[data-gallery-next]");

  const fallbackImage = project.fallbackImage || '/assets/projects/placeholder.svg';
  let gallerySources = [];
  let currentIndex = 0;

  const setMainImage = (src) => {
    // subtle fade: dip opacity, swap the source, fade back in once the new image is ready
    image.style.opacity = '0';

    image.addEventListener('load', () => {
      image.style.opacity = '1';
    }, { once: true });

    // if the real screenshot fails to load, fall back to the project's illustration
    // instead of leaving a broken image in the hero slot
    image.addEventListener('error', () => {
      if (image.src.endsWith(fallbackImage)) return;
      image.src = fallbackImage;
    }, { once: true });

    image.src = src;
    image.alt = `${project.name} project screenshot`;

    selectAll('.project-thumb').forEach((thumb) => {
      thumb.classList.toggle('is-active', thumb.src.endsWith(src));
      thumb.setAttribute('aria-current', thumb.src.endsWith(src) ? 'true' : 'false');
    });
  };

  const goToIndex = (index) => {
    if (gallerySources.length === 0) return;
    currentIndex = (index + gallerySources.length) % gallerySources.length;
    setMainImage(gallerySources[currentIndex]);
  };

  prevButton?.addEventListener('click', () => goToIndex(currentIndex - 1));
  nextButton?.addEventListener('click', () => goToIndex(currentIndex + 1));

  galleryHero?.addEventListener('keydown', (event) => {
    if (event.key === 'ArrowLeft') goToIndex(currentIndex - 1);
    if (event.key === 'ArrowRight') goToIndex(currentIndex + 1);
  });

  // touch swipe: left swipe -> next, right swipe -> previous
  let touchStartX = null;
  galleryHero?.addEventListener('touchstart', (event) => {
    touchStartX = event.touches[0]?.clientX ?? null;
  }, { passive: true });
  galleryHero?.addEventListener('touchend', (event) => {
    if (touchStartX === null) return;
    const touchEndX = event.changedTouches[0]?.clientX ?? touchStartX;
    const deltaX = touchEndX - touchStartX;
    const swipeThreshold = 40;
    if (deltaX > swipeThreshold) goToIndex(currentIndex - 1);
    else if (deltaX < -swipeThreshold) goToIndex(currentIndex + 1);
    touchStartX = null;
  });

  const renderGallery = (screenshots) => {
    if (!Array.isArray(screenshots) || screenshots.length === 0) return;

    gallerySources = screenshots;
    currentIndex = 0;

    // thumbnails row, placed directly under the hero image inside the gallery column
    let thumbRow = select('.project-thumb-row');
    if (!thumbRow) {
      thumbRow = createElement('div', 'project-thumb-row');
      thumbRow.setAttribute('role', 'listbox');
      thumbRow.setAttribute('aria-label', `${project.name} screenshots`);
      gallery.appendChild(thumbRow);
    } else {
      thumbRow.replaceChildren();
    }

    const multipleShots = screenshots.length > 1;
    prevButton?.toggleAttribute('hidden', !multipleShots);
    nextButton?.toggleAttribute('hidden', !multipleShots);
    if (galleryHero) {
      galleryHero.tabIndex = multipleShots ? 0 : -1;
      if (multipleShots) {
        galleryHero.setAttribute('aria-label', `${project.name} screenshot gallery, use the arrow buttons or left/right arrow keys to browse`);
      } else {
        galleryHero.removeAttribute('aria-label');
      }
    }

    // main image defaults to the first screenshot
    setMainImage(screenshots[0]);

    if (!multipleShots) return;

    screenshots.forEach((src, index) => {
      const thumb = document.createElement('img');
      thumb.className = 'project-thumb';
      thumb.src = src;
      thumb.alt = `${project.name} screenshot`;
      thumb.loading = 'lazy';
      thumb.decoding = 'async';
      thumb.width = 160;
      thumb.height = 94;
      thumb.setAttribute('role', 'button');
      thumb.setAttribute('tabindex', '0');
      thumb.addEventListener('error', () => {
        if (thumb.src.endsWith(fallbackImage)) return;
        thumb.src = fallbackImage;
      }, { once: true });
      thumb.addEventListener('click', () => goToIndex(index));
      thumb.addEventListener('keydown', (event) => {
        if (event.key === 'Enter' || event.key === ' ') {
          event.preventDefault();
          goToIndex(index);
        }
      });
      thumbRow.appendChild(thumb);
    });
  };

  select("[data-project-technologies]").replaceChildren(
    ...project.technologies.map((technology) => createElement("span", "tag", technology))
  );

  // handle screenshots: support folder references or explicit arrays
  (async () => {
    let screenshots = project.screenshots || [];
    if (screenshots.length && typeof screenshots[0] === 'string' && screenshots[0].endsWith('/')) {
      // request list from server
      const folder = screenshots[0].replace(/^\/assets\//, '').replace(/\/$/, '');
      try {
        const res = await fetch(`/api/assets/${encodeURIComponent(folder)}`);
        if (res.ok) {
          const json = await res.json();
          screenshots = json.files || [];
        }
      } catch (e) {
        screenshots = screenshots;
      }
    }

    if (screenshots.length === 0) {
      setMainImage('/assets/projects/placeholder.svg');
    }

    renderGallery(screenshots);
  })();

  const actions = [createLink("/#projects", "All projects", "button button-secondary")];
  if (project.githubUrl) {
    actions.unshift(createGithubButton(project.githubUrl, "button button-primary", "View GitHub"));
  }
  select("[data-project-actions]").replaceChildren(...actions);
};

const boot = async () => {
  document.querySelectorAll("[data-year]").forEach((element) => {
    element.textContent = new Date().getFullYear();
  });

  const slug = getSlug();
  if (!slug) throw new Error("Missing project slug.");

  const response = await fetch(`/api/projects/${slug}`);
  if (!response.ok) throw new Error("Project not found.");
  const project = await response.json();
  renderProject(project);
};

boot().catch((error) => {
  const container = select("[data-project-detail]");
  container.replaceChildren();
  const section = createElement("section", "section");
  const inner = createElement("div", "container");
  inner.append(createElement("h1", "", "Project not found"), createElement("p", "headline", error.message), createLink("/", "Return home", "button button-primary"));
  section.append(inner);
  container.append(section);
});

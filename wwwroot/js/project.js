// The project page is rendered on the server; this script only turns the screenshot thumbnails
// (plain links to the full images) into an in-page gallery with arrows, keyboard and swipe support.

const setupGallery = () => {
  const gallery = document.querySelector("[data-gallery]");
  const image = document.querySelector("[data-gallery-image]");
  const thumbs = [...document.querySelectorAll("[data-gallery-thumb]")];
  if (!gallery || !image || thumbs.length < 2) return;

  const prevButton = gallery.querySelector("[data-gallery-prev]");
  const nextButton = gallery.querySelector("[data-gallery-next]");
  let currentIndex = 0;

  const showImage = (index) => {
    const nextIndex = (index + thumbs.length) % thumbs.length;
    if (nextIndex === currentIndex) return;
    currentIndex = nextIndex;

    const thumb = thumbs[currentIndex];

    // subtle fade: dip opacity, swap the source, fade back in once the new image is ready
    image.style.opacity = "0";
    const reveal = () => {
      image.style.opacity = "1";
    };
    image.addEventListener("load", reveal, { once: true });
    image.addEventListener("error", reveal, { once: true });

    if (thumb.dataset.width && thumb.dataset.height) {
      image.width = Number(thumb.dataset.width);
      image.height = Number(thumb.dataset.height);
    }
    image.src = thumb.getAttribute("href");

    thumbs.forEach((item, itemIndex) => {
      const isActive = itemIndex === currentIndex;
      item.classList.toggle("is-active", isActive);
      if (isActive) {
        item.setAttribute("aria-current", "true");
      } else {
        item.removeAttribute("aria-current");
      }
    });
  };

  thumbs.forEach((thumb, index) => {
    thumb.addEventListener("click", (event) => {
      event.preventDefault();
      showImage(index);
    });
  });

  prevButton?.removeAttribute("hidden");
  nextButton?.removeAttribute("hidden");
  prevButton?.addEventListener("click", () => showImage(currentIndex - 1));
  nextButton?.addEventListener("click", () => showImage(currentIndex + 1));

  // arrow-key browsing once the gallery itself has focus
  gallery.tabIndex = 0;
  gallery.setAttribute("role", "group");
  gallery.setAttribute("aria-label", gallery.dataset.galleryLabel || "Screenshot gallery");
  gallery.addEventListener("keydown", (event) => {
    if (event.key === "ArrowLeft") showImage(currentIndex - 1);
    if (event.key === "ArrowRight") showImage(currentIndex + 1);
  });

  // touch swipe: left swipe -> next, right swipe -> previous
  let touchStartX = null;
  gallery.addEventListener("touchstart", (event) => {
    touchStartX = event.touches[0]?.clientX ?? null;
  }, { passive: true });
  gallery.addEventListener("touchend", (event) => {
    if (touchStartX === null) return;
    const touchEndX = event.changedTouches[0]?.clientX ?? touchStartX;
    const deltaX = touchEndX - touchStartX;
    const swipeThreshold = 40;
    if (deltaX > swipeThreshold) showImage(currentIndex - 1);
    else if (deltaX < -swipeThreshold) showImage(currentIndex + 1);
    touchStartX = null;
  });
};

setupGallery();

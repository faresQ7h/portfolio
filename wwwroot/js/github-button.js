// Shared GitHub button builder, used by both the project cards (site.js) and
// the project detail page (project.js) so the icon asset, size, and spacing
// can never drift between the two.
const GITHUB_ICON_SRC = "/assets/icons/githupLogo.svg";

const createGithubButton = (href, className, label, accessibleLabel) => {
  const link = document.createElement("a");
  link.className = className;
  link.href = href;
  link.target = "_blank";
  link.rel = "noopener";
  if (accessibleLabel) link.setAttribute("aria-label", accessibleLabel);

  const icon = document.createElement("img");
  icon.src = GITHUB_ICON_SRC;
  icon.alt = "";
  icon.setAttribute("aria-hidden", "true");
  icon.width = 18;
  icon.height = 18;
  icon.decoding = "async";

  link.append(icon, document.createTextNode(label));
  return link;
};

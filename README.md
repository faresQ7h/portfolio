# Fares Mohamed Portfolio

Production-ready personal portfolio built with ASP.NET Core 8, C#, HTML, CSS, and vanilla JavaScript.

## Structure

- `Controllers/` exposes REST endpoints for profile and projects.
- `Models/` contains typed DTOs used by the API.
- `Services/` reads JSON portfolio data from static files.
- `wwwroot/api/profile.json` stores editable profile, education, experience, learning, and skill data.
- `wwwroot/api/projects.json` stores editable project cards and detail pages.
- `wwwroot/assets/` is the single home for every image asset (project screenshots, icons, the profile photo,
  and the SVG illustrations used as fallbacks for projects with no screenshots). A project's screenshots live in
  their own subfolder, e.g. `wwwroot/assets/minishell/`.
- `wwwroot/css` and `wwwroot/js` contain the frontend.
- `wwwroot/files/Fares_Mohamed_CV.pdf` is used by the Download CV button.

## Local Development

```bash
DOTNET_CLI_HOME=/tmp/dotnet DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 dotnet restore --ignore-failed-sources
DOTNET_CLI_HOME=/tmp/dotnet DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 dotnet run
```

Open the URL printed by `dotnet run`, usually `http://localhost:5088`.

## Editing Projects

Add or update projects only in `wwwroot/api/projects.json`. Each project needs a stable `slug`; the detail page is available at `/projects/{slug}`.

To give a project a real screenshot gallery, drop images (`.png`, `.jpg`, `.jpeg`, `.webp`, or `.gif`) into a folder
under `wwwroot/assets/` (e.g. `wwwroot/assets/my-project/`) and set that project's `screenshots` field in
`projects.json` to the folder path with a trailing slash, e.g. `"screenshots": ["/assets/my-project/"]`. The
`/api/assets/{folder}` endpoint (`Program.cs`) lists every image file in that folder at request time, so the detail
page gallery always reflects whatever is currently in the folder — add or remove images later with no code changes.
Projects with no screenshots fall back to `/assets/projects/placeholder.svg`.

## Deployment: Linux + ASP.NET + Nginx + HTTPS

1. Publish the app:

```bash
dotnet publish -c Release -o /var/www/fares-portfolio
```

2. Create a systemd service at `/etc/systemd/system/fares-portfolio.service`:

```ini
[Unit]
Description=Fares Mohamed Portfolio
After=network.target

[Service]
WorkingDirectory=/var/www/fares-portfolio
ExecStart=/usr/bin/dotnet /var/www/fares-portfolio/FaresPortfolio.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=fares-portfolio
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

3. Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable fares-portfolio
sudo systemctl start fares-portfolio
```

4. Configure Nginx:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

5. Add HTTPS with Certbot:

```bash
sudo certbot --nginx -d your-domain.com
```

Before going live, replace `https://example.com` in `wwwroot/index.html`, `wwwroot/robots.txt`, and `wwwroot/sitemap.xml` with the production domain.

## Contact

The site has no contact form. Use the `Connect with me` section on the homepage to reach out via email, GitHub, or LinkedIn.

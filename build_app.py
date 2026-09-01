# -*- coding: utf-8 -*-
"""Build every shipping form of Huddle Board from one source file.

  HuddleBoard.html        standalone — works from file://, no server, no updates
  deploy/                 static site for Azure App Service — installs to the
                          home screen, caches offline, updates from the URL
  huddle_artifact.html    body-only form for publishing as a Claude artifact
"""
import datetime as _dt
import json
import os
import shutil
import pathlib
import zipfile

ROOT = pathlib.Path(__file__).resolve().parent
DIST = ROOT / "dist"
DEPLOY = DIST / "deploy"
DIST.mkdir(exist_ok=True)

VERSION = os.environ.get("HB_VERSION") or _dt.datetime.now().strftime("%Y.%m.%d-%H%M")
data = (DIST / "proto_data.json").read_text()
src = (ROOT / "huddle_src.html").read_text().replace("__DATA__", data).replace("__VERSION__", VERSION)

HEAD = """<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover,\
user-scalable=no">
<meta name="mobile-web-app-capable" content="yes">
<meta name="apple-mobile-web-app-capable" content="yes">
<meta name="theme-color" content="#1E2521">
<link rel="manifest" href="./manifest.webmanifest">
<link rel="icon" href="./icon-192.png">
<link rel="apple-touch-icon" href="./icon-192.png">
</head>
<body>
"""
page = HEAD + src + "\n</body>\n</html>\n"

(DIST / "huddle_artifact.html").write_text(src)
(DIST / "HuddleBoard.html").write_text(page)

DEPLOY.mkdir(exist_ok=True)
open(DEPLOY / "index.html", "w").write(page)

# ----------------------------------------------------------------- manifest
open(DEPLOY / "manifest.webmanifest", "w").write(json.dumps({
    "name": "Huddle Board",
    "short_name": "Huddle",
    "description": "Sideline play tool for 8U flag football.",
    "start_url": "./",
    "scope": "./",
    "display": "fullscreen",
    "display_override": ["fullscreen", "standalone", "minimal-ui"],
    "orientation": "landscape",
    "background_color": "#1E2521",
    "theme_color": "#1E2521",
    "icons": [
        {"src": "./icon-192.png", "sizes": "192x192", "type": "image/png"},
        {"src": "./icon-512.png", "sizes": "512x512", "type": "image/png"},
        {"src": "./icon-maskable-512.png", "sizes": "512x512", "type": "image/png",
         "purpose": "maskable"},
    ],
}, indent=2))

# ------------------------------------------------------------ service worker
open(DEPLOY / "sw.js", "w").write("""/* Huddle Board service worker — version %s
   Cache-first for our own files so the app opens with the radio off. A new
   build lands as a waiting worker; the page shows "Update ready" and only
   swaps when the coach taps it, so an update can never interrupt a live play. */
const VERSION = "%s";
const CACHE = "huddle-" + VERSION;
const ASSETS = ["./", "./index.html", "./manifest.webmanifest",
                "./icon-192.png", "./icon-512.png", "./icon-maskable-512.png"];

self.addEventListener("install", e => {
  e.waitUntil(caches.open(CACHE).then(c => c.addAll(ASSETS)));
});

self.addEventListener("activate", e => {
  e.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("message", e => {
  if (e.data && e.data.type === "SKIP_WAITING") self.skipWaiting();
});

self.addEventListener("fetch", e => {
  const req = e.request;
  if (req.method !== "GET") return;
  if (new URL(req.url).origin !== location.origin) return;   // fonts go to the network
  e.respondWith(
    caches.match(req).then(hit => hit || fetch(req).then(res => {
      const copy = res.clone();
      caches.open(CACHE).then(c => c.put(req, copy));
      return res;
    }).catch(() => caches.match("./index.html")))
  );
});
""" % (VERSION, VERSION))

# --------------------------------------------------------------- web.config
open(DEPLOY / "web.config", "w").write("""<?xml version="1.0" encoding="utf-8"?>
<!-- Azure App Service (Windows / IIS). Two things matter here: the manifest
     needs its MIME type, and index.html + sw.js must NOT be cached by the
     browser or tablets will never see a new build. -->
<configuration>
  <system.webServer>
    <staticContent>
      <remove fileExtension=".webmanifest" />
      <mimeMap fileExtension=".webmanifest" mimeType="application/manifest+json" />
      <remove fileExtension=".json" />
      <mimeMap fileExtension=".json" mimeType="application/json" />
    </staticContent>
    <defaultDocument>
      <files>
        <clear />
        <add value="index.html" />
      </files>
    </defaultDocument>
    <httpProtocol>
      <customHeaders>
        <add name="X-Content-Type-Options" value="nosniff" />
      </customHeaders>
    </httpProtocol>
  </system.webServer>
  <location path="index.html">
    <system.webServer><httpProtocol><customHeaders>
      <add name="Cache-Control" value="no-cache, must-revalidate" />
    </customHeaders></httpProtocol></system.webServer>
  </location>
  <location path="sw.js">
    <system.webServer><httpProtocol><customHeaders>
      <add name="Cache-Control" value="no-cache, must-revalidate" />
    </customHeaders></httpProtocol></system.webServer>
  </location>
  <location path="manifest.webmanifest">
    <system.webServer><httpProtocol><customHeaders>
      <add name="Cache-Control" value="no-cache, must-revalidate" />
    </customHeaders></httpProtocol></system.webServer>
  </location>
</configuration>
""")

# ------------------------------------- other hosts, same two rules as IIS
# Azure Static Web Apps
open(DEPLOY / "staticwebapp.config.json", "w").write(json.dumps({
    "mimeTypes": {".webmanifest": "application/manifest+json", ".json": "application/json"},
    "routes": [
        {"route": "/sw.js", "headers": {"Cache-Control": "no-cache, must-revalidate"}},
        {"route": "/index.html", "headers": {"Cache-Control": "no-cache, must-revalidate"}},
        {"route": "/manifest.webmanifest",
         "headers": {"Cache-Control": "no-cache, must-revalidate"}},
    ],
    "navigationFallback": {"rewrite": "/index.html"},
}, indent=2))

# Netlify / Cloudflare Pages
open(DEPLOY / "_headers", "w").write("""/sw.js
  Cache-Control: no-cache, must-revalidate
/index.html
  Cache-Control: no-cache, must-revalidate
/manifest.webmanifest
  Content-Type: application/manifest+json
  Cache-Control: no-cache, must-revalidate
""")

# ------------------------------------------------------------------- readme
open(DEPLOY / "README.md", "w").write("""# Huddle Board — deploy

Version **%s**. Static files only: no server code, no build step, no dependencies.

## Azure App Service

Drop the contents of this folder at the site root (`/site/wwwroot` on Windows,
`/home/site/wwwroot` on Linux). Any of these work:

* **Zip deploy** — `az webapp deploy --resource-group <rg> --name <app> --src-path deploy.zip --type zip`
* **FTPS** — upload the six files
* **GitHub Actions** — point `azure/webapps-deploy` at this folder

## Any static host works — the config for each is included

Whatever you use, only two things actually matter: serve `.webmanifest` as
`application/manifest+json`, and send `Cache-Control: no-cache` for
`index.html` and `sw.js`. Miss the second one and tablets will never see a new
build. The right file for your host is already in this folder:

| host | file it reads | notes |
|---|---|---|
| Azure App Service (Windows) | `web.config` | drop-in |
| Azure Static Web Apps | `staticwebapp.config.json` | free tier, good fit |
| Netlify / Cloudflare Pages | `_headers` | drag and drop this folder |
| GitHub Pages | none needed | headers are fine by default |
| Azure App Service (Linux) | neither | set the two headers in your container |

The extra config files are inert on hosts that do not read them.

## What will NOT work

**OneDrive, SharePoint, Dropbox, Google Drive.** They serve a viewer page and
then a tokenised redirect to a download host, so there is no stable origin,
relative paths like `./sw.js` never resolve, and a service worker cannot
register. Sharing `HuddleBoard.html` through them is fine — that is file
distribution, not hosting.

HTTPS is required for the service worker. App Service and every host above
give you that for free on their default hostname.

HTTPS is required for the service worker. App Service gives you that on the
default `*.azurewebsites.net` hostname.

## Getting it onto a coach's tablet — and losing the address bar

A web page cannot hide Chrome's address bar in an ordinary tab. Full screen
comes from **installing** it:

1. Open the URL in Chrome (must be **https**, not a local file).
2. Menu → **Install app** (or **Add to Home screen**).
3. Launch it from the new home-screen icon — *not* from a Chrome tab.

It then opens with no browser UI at all, locked to landscape, and works with
the radio off.

If the menu offers only a plain bookmark-style shortcut and not **Install
app**, the install criteria are not being met. Check, in order: the page is on
https, `manifest.webmanifest` returns HTTP 200 with type
`application/manifest+json`, and `sw.js` registered (DevTools → Application →
Service Workers). A local `file://` copy can never be installed.

**Meanwhile there is a Full screen button** on the deck screen whenever the app
is running in a tab. It uses the Fullscreen API, which works in a tab and even
from a local file, and it also locks the orientation — Android only permits
orientation lock in fullscreen or an installed app. The button hides itself
once the app is installed, because then it is already full screen.

## Shipping an update

Deploy new files. Next time a coach opens the app it notices the new build,
downloads it in the background, and shows **Update ready** on the deck screen.
It swaps only when they tap it — never mid-play. The version is shown at the
bottom of the Settings screen.

## Running it without a server

`HuddleBoard.html` is the whole app in one file. Copy it to a tablet and open
it from Files. No install, no network, no updates. Note that Android may block
storage on `file://` URLs, in which case deck and settings will not persist —
that is the main reason to prefer the hosted copy.

## Files

| file | what it is |
|---|---|
| `index.html` | the entire app — markup, styles, script, all 14 plays |
| `sw.js` | offline cache + update-on-tap |
| `manifest.webmanifest` | name, icons, landscape, standalone launch |
| `icon-*.png` | home screen icons |
| `web.config` | IIS MIME types and cache headers |
""" % VERSION)

with zipfile.ZipFile(DIST / "HuddleBoard-deploy.zip", "w", zipfile.ZIP_DEFLATED) as z:
    for f in sorted(os.listdir(DEPLOY)):
        z.write(DEPLOY / f, f)

print("version", VERSION)
print("standalone  HuddleBoard.html      %6.1f KB" % (os.path.getsize(DIST / "HuddleBoard.html")/1024))
print("deploy zip  HuddleBoard-deploy.zip %6.1f KB" % (os.path.getsize(DIST / "HuddleBoard-deploy.zip")/1024))
for f in sorted(os.listdir(DEPLOY)):
    print("   deploy/%-24s %7d bytes" % (f, os.path.getsize(DEPLOY / f)))

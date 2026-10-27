// Not the app's service worker: the one that takes it away.
//
// Before the website, the app was served at the root, so any browser that ever
// opened it there has a worker registered for "/" that answers every request on
// this host from its cache — the home page included — and a 404 on its update
// check only keeps it. Serving this file at the old address is what reaches it:
// the next update check installs this, and this unregisters itself.
//
// Cache names are per origin, not per scope, and the app's worker at /app/ uses
// the same huddle-<version> names, so only a cache holding the root's own
// /index.html is deleted. Deleting every huddle-* cache would take the offline
// copy away from the app at /app/ as well. The two never share one cache: the
// root never served the build /app/ runs, and the name is the version.
//
// Nothing on the site registers this file. It only ever runs as an update.
self.addEventListener("install", () => self.skipWaiting());

self.addEventListener("activate", e => {
  e.waitUntil((async () => {
    const root = new URL("/index.html", self.location).href;
    for (const key of await caches.keys()) {
      if (!key.startsWith("huddle-")) continue;
      const cache = await caches.open(key);
      if (await cache.match(root)) await caches.delete(key);
    }
    await self.registration.unregister();
    for (const client of await self.clients.matchAll({ type: "window" }))
      client.navigate(client.url);
  })());
});

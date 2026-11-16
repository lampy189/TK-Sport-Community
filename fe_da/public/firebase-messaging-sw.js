/* FCM data-only messages: this worker is the single display/click handler. */
let queue = Promise.resolve();
self.addEventListener('push', event => {
  queue = queue.catch(() => {}).then(async () => {
    const data = event.data?.json()?.data;
    if (!data?.title) return;
    const url = new URL(data.url || '/player/home?view=notifications', self.location.origin);
    if (url.origin !== self.location.origin) return;
    const cache = await caches.open('tksc-push-events-v1');
    const key = data.eventId ? new Request(`${self.location.origin}/__push_event/${encodeURIComponent(data.eventId)}`) : null;
    if (key && await cache.match(key)) return;
    await self.registration.showNotification(data.title, {
      body: data.body, tag: data.eventId ? `tksc-${data.eventId}` : undefined,
      data: { url: url.href },
    });
    if (key) {
      await cache.put(key, new Response('seen'));
      const keys = await cache.keys();
      for (const old of keys.slice(0, Math.max(0, keys.length - 500))) await cache.delete(old);
    }
  });
  event.waitUntil(queue);
});
self.addEventListener('notificationclick', event => {
  event.notification.close();
  event.waitUntil((async () => {
    const url = new URL(event.notification.data?.url || '/', self.location.origin);
    if (url.origin !== self.location.origin) return;
    const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    for (const client of windows) {
      if (new URL(client.url).origin === url.origin) {
        await client.navigate(url.href);
        await client.focus();
        return;
      }
    }
    await self.clients.openWindow(url.href);
  })());
});

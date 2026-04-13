self.addEventListener('install', event => {
    self.skipWaiting();
});

self.addEventListener('fetch', event => {
    // Si la request es para la API (turnos, negocios, etc), NO usamos caché
    if (event.request.url.includes('/api/')) {
        return;
    }

    // Para el resto (HTML, CSS, DLLs), tratamos de servirlo desde la caché
    event.respondWith(
        caches.match(event.request).then(response => {
            return response || fetch(event.request);
        })
    );
});
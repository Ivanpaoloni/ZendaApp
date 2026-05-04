// Zendy Service Worker
const APP_VERSION = 'v1.0.6'; // Subimos la versión para forzar el cambio

self.addEventListener('install', event => {
    console.log('Instalando Service Worker versión:', APP_VERSION);
});

self.addEventListener('fetch', event => {
    // Escucha básica
});

// NUEVO: Escuchamos el mensaje desde index.html para saltar la espera
self.addEventListener('message', event => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
});
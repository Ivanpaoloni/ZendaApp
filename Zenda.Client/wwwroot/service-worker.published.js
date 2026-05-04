// Este archivo es el que se compila y sube a Railway
self.importScripts('./service-worker-assets.js');

self.addEventListener('install', event => {
    self.skipWaiting(); // TRUCO 1: No esperamos a nadie
    event.waitUntil(onInstall(event));
});

self.addEventListener('activate', event => {
    event.waitUntil(clients.claim().then(() => onActivate(event))); // TRUCO 2: Tomamos el control ya
});

self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.wasm/, /\.html/, /\.js/, /\.json/, /\.css/, /\.woff/, /\.png/, /\.jpe?g/, /\.gif/, /\.ico/, /\.blat/, /\.dat/];
const offlineAssetsExclude = [/^service-worker\.js$/];

async function onInstall(event) {
    console.info('Instalando nueva versión de Zendy...');
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Activando nueva versión de Zendy...');
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        const request = event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }
    return cachedResponse || fetch(event.request);
}
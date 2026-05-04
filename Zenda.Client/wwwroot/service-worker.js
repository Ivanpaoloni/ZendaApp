const APP_VERSION = 'v1.0.5'; // Cambia esto en cada push

self.addEventListener('install', event => {
    console.log('Instalando Service Worker versión:', APP_VERSION);
    // TRUCO: Toma el control inmediatamente, sin esperar
    self.skipWaiting(); 
});

self.addEventListener('fetch', event => {
    // Escucha básica para PWA
});
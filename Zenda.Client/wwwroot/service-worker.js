// Zendy Service Worker
// IMPORTANTE: Cambia este número de versión cada vez que hagas un push 
// y quieras que los usuarios vean el cartel de "Actualización".
const APP_VERSION = 'v1.0.2';

self.addEventListener('install', event => {
    console.log('Instalando Service Worker versión:', APP_VERSION);
    // No usamos skipWaiting() para dejar que el usuario decida cuándo recargar con el cartel
});

self.addEventListener('fetch', event => {
    // Escucha básica para que los navegadores lo consideren una PWA válida
});
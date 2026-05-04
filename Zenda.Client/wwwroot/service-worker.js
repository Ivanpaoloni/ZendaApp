// Cambia este número para forzar la actualización en los celulares
const APP_VERSION = 'v1.0.9';

self.addEventListener('install', event => {
    console.log('Instalando SW:', APP_VERSION);
    // 1. Instalar y no esperar a que se cierre la app
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.log('Activando SW:', APP_VERSION);
    // 2. Tomar el control de la pestaña abierta INMEDIATAMENTE
    event.waitUntil(clients.claim());
});

self.addEventListener('fetch', event => {
    // Necesario para que Chrome lo reconozca como PWA válida
});
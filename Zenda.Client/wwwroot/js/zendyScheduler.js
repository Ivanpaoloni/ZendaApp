window.zendyUI = {
    scrollToElement: function (elementId) {
        const element = document.getElementById(elementId);
        if (element) {
            element.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
        }
    },
    scrollToTime: function (containerId, hour) {
        const container = document.getElementById(containerId);
        if (container) {
            // 1 hora equivale a 60px. Ajustamos restando la altura de las cabeceras.
            const targetScrollTop = (hour * 60) - 20;
            container.scrollTo({ top: targetScrollTop, behavior: 'smooth' });
        }
    }
};
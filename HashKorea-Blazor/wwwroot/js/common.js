// 1. image lazy loading

window.initLazyLoading = function () {
    const observer = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.classList.remove('lazy-image');
                observer.unobserve(img);
            }
        });
    });

    document.querySelectorAll('img.lazy-image').forEach(img => {
        observer.observe(img);
    });
}

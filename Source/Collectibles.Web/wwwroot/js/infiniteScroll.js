window.infiniteScroll = {
    observeLastElement: function (dotNetHelper, selector) {
        const options = {
            root: null,
            rootMargin: '0px',
            threshold: 0.1
        };

        const callback = (entries, observer) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    dotNetHelper.invokeMethodAsync('LoadMore');
                }
            });
        };

        const observer = new IntersectionObserver(callback, options);
        const targetElements = document.querySelectorAll(selector);
        
        if (targetElements.length > 0) {
            const lastElement = targetElements[targetElements.length - 1];
            observer.observe(lastElement);
        }

        return {
            dispose: () => {
                observer.disconnect();
            }
        };
    }
};
// Auto-scroll for [data-auto-scroll] containers: stick to the bottom while
// the user is already near it, force-scroll to the bottom when [data-agent]
// flips. Self-attaches by scanning the document, so no JS interop is
// required from the Blazor side.
(function () {
    var EPSILON = 32;

    function attach(el) {
        if (el.__autoScrollAttached) return;
        el.__autoScrollAttached = true;
        var lastAgent = el.dataset.agent || '';
        var stick = true;

        el.addEventListener('scroll', function () {
            stick = (el.scrollHeight - el.scrollTop - el.clientHeight) <= EPSILON;
        });

        new MutationObserver(function () {
            var current = el.dataset.agent || '';
            var agentChanged = current !== lastAgent;
            lastAgent = current;
            if (agentChanged || stick) {
                el.scrollTop = el.scrollHeight;
                stick = true;
            }
        }).observe(el, {
            childList: true,
            subtree: true,
            characterData: true,
            attributes: true,
            attributeFilter: ['data-agent'],
        });

        el.scrollTop = el.scrollHeight;
    }

    function scan() {
        document.querySelectorAll('[data-auto-scroll]').forEach(attach);
    }

    new MutationObserver(scan).observe(document.documentElement, {
        childList: true,
        subtree: true,
    });
    scan();
})();

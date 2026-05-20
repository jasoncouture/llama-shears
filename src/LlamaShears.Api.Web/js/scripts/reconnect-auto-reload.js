// When Blazor's reconnect machinery reaches a terminal state
// (failed or rejected), the SignalR runtime is gone and the only
// useful action is a full page reload — so we do it automatically
// instead of waiting for the user to hit a button.
(function () {
    var fired = false;

    function check() {
        if (fired) return;
        var modal = document.getElementById('components-reconnect-modal');
        if (!modal) return;
        if (modal.classList.contains('components-reconnect-failed') ||
            modal.classList.contains('components-reconnect-rejected')) {
            fired = true;
            location.reload();
        }
    }

    // Blazor swaps the ReconnectModal subtree during hydration, so a
    // direct observer on the SSR-rendered element wouldn't see later
    // class changes. Watch the whole document for class mutations and
    // re-check the live element each time.
    new MutationObserver(check).observe(document.documentElement, {
        attributes: true,
        subtree: true,
        attributeFilter: ['class'],
    });
    check();
})();

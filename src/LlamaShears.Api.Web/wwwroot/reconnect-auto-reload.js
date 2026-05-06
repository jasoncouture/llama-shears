// When Blazor's reconnect machinery reaches a terminal state
// (failed or rejected), the SignalR runtime is gone and the only
// useful action is a full page reload — so we do it automatically
// instead of waiting for the user to hit a button.
(function () {
    var modal = document.getElementById('components-reconnect-modal');
    if (!modal) return;
    var fired = false;
    var trigger = function () {
        if (fired) return;
        if (modal.classList.contains('components-reconnect-failed') ||
            modal.classList.contains('components-reconnect-rejected')) {
            fired = true;
            location.reload();
        }
    };
    new MutationObserver(trigger).observe(modal, { attributes: true, attributeFilter: ['class'] });
    trigger();
})();

document.addEventListener('keydown', function (e) {
    var t = e.target;
    if (!(t instanceof HTMLTextAreaElement)) return;
    if (!t.matches('[data-composer-submit]')) return;
    if (e.key !== 'Enter' || e.shiftKey || e.isComposing) return;
    e.preventDefault();
    var form = t.form;
    if (form && typeof form.requestSubmit === 'function') {
        form.requestSubmit();
    }
});

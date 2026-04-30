// Enter sends, Shift+Enter inserts newline, IME composition is left alone.
// Targets any <textarea data-composer-submit> inside a <form>.
document.addEventListener('keydown', function (e) {
    var t = e.target;
    if (!(t instanceof HTMLTextAreaElement)) return;
    if (!t.matches('[data-composer-submit]')) return;
    if (e.key !== 'Enter' || e.shiftKey || e.isComposing) return;
    e.preventDefault();
    if (t.form && typeof t.form.requestSubmit === 'function') {
        t.form.requestSubmit();
    }
});

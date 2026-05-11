// Bridges browser clipboard-paste of an image into a Blazor composer
// instance. Composers register a (id, DotNetObjectReference) pair via
// composerPaste.register on first render; when the user pastes an image
// into a textarea[data-composer-paste-id], we read the file as a
// base64 data URL, strip the prefix, and call OnPasteAsync(mime, b64)
// on the matching Composer.
(function () {
    var registry = new Map();

    window.composerPaste = {
        register: function (id, dotnetRef) {
            registry.set(id, dotnetRef);
        },
        unregister: function (id) {
            registry.delete(id);
        },
    };

    document.addEventListener('paste', function (e) {
        var t = e.target;
        if (!(t instanceof HTMLTextAreaElement)) return;
        var id = t.getAttribute('data-composer-paste-id');
        if (!id) return;
        var ref = registry.get(id);
        if (!ref) return;

        var items = e.clipboardData && e.clipboardData.items;
        if (!items) return;

        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            if (item.kind !== 'file') continue;
            var file = item.getAsFile();
            if (!file) continue;
            if (file.type.indexOf('image/') !== 0) continue;

            // Stop the textarea from receiving the (binary) paste as
            // garbled text; we're handling it.
            e.preventDefault();

            (function (f) {
                var reader = new FileReader();
                reader.onload = function () {
                    var url = reader.result || '';
                    var commaIdx = url.indexOf(',');
                    if (commaIdx < 0) return;
                    var b64 = url.substring(commaIdx + 1);
                    ref.invokeMethodAsync('OnPasteAsync', f.type, b64);
                };
                reader.readAsDataURL(f);
            })(file);
        }
    });
})();

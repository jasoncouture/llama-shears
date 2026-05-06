// Auto-attach syntax highlighting to any <pre><code> block that appears
// in the document. highlight.js stamps each element with
// data-highlighted="yes" after processing, so we filter on that to skip
// already-highlighted blocks. For streaming bubbles, Blazor replaces
// the rendered node when the markdown re-renders, so the new block
// arrives unstamped and gets picked up on the next mutation.
(function () {
    function highlightWithin(root) {
        if (!root || !root.querySelectorAll || !window.hljs) {
            return;
        }
        root.querySelectorAll('pre code:not([data-highlighted="yes"])').forEach(function (el) {
            try {
                window.hljs.highlightElement(el);
            } catch (e) {
                // Unknown language or hljs internal error — leave the
                // block as plain text rather than break the whole page.
            }
        });
    }

    new MutationObserver(function (mutations) {
        for (var i = 0; i < mutations.length; i++) {
            var added = mutations[i].addedNodes;
            for (var j = 0; j < added.length; j++) {
                var node = added[j];
                if (node.nodeType === 1) {
                    highlightWithin(node);
                }
            }
        }
    }).observe(document.documentElement, {
        childList: true,
        subtree: true,
    });

    highlightWithin(document.body);
})();

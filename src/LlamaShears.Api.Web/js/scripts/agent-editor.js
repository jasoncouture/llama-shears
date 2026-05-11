import { basicSetup } from "codemirror";
import { EditorView } from "@codemirror/view";
import { json, jsonParseLinter } from "@codemirror/lang-json";
import { linter } from "@codemirror/lint";

const instances = new WeakMap();

export function init(element, initialValue, dotnetRef) {
    if (instances.has(element)) {
        dispose(element);
    }
    const view = new EditorView({
        doc: initialValue ?? "",
        extensions: [
            basicSetup,
            json(),
            linter(jsonParseLinter()),
            EditorView.theme({ "&": { height: "100%" }, ".cm-scroller": { fontFamily: "ui-monospace, SFMono-Regular, 'Cascadia Mono', 'Roboto Mono', monospace" } }, { dark: true }),
            EditorView.updateListener.of((update) => {
                if (update.docChanged) {
                    dotnetRef.invokeMethodAsync("OnContentChanged", view.state.doc.toString());
                }
            }),
        ],
        parent: element,
    });
    instances.set(element, view);
}

export function setValue(element, value) {
    const view = instances.get(element);
    if (!view) return;
    const current = view.state.doc.toString();
    if (current === value) return;
    view.dispatch({ changes: { from: 0, to: view.state.doc.length, insert: value ?? "" } });
}

export function getValue(element) {
    return instances.get(element)?.state.doc.toString() ?? "";
}

export function dispose(element) {
    const view = instances.get(element);
    if (!view) return;
    view.destroy();
    instances.delete(element);
}

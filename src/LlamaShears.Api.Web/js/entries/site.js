// Side-effect bundle: everything that registers global listeners or
// runs at script-load time. Loaded via plain <script src="site.js"> in
// App.razor, not as a module. highlight-init pulls in highlight.min.js
// through its own import; we do NOT pre-import highlight.min.js here
// because esbuild bundles it as CommonJS and its `var hljs` never
// reaches window — we go through the bundle's module graph instead.
import "../scripts/composer.js";
import "../scripts/composer-paste.js";
import "../scripts/messages.js";
import "../scripts/highlight-init.js";
import "../scripts/reconnect-auto-reload.js";

// Side-effect bundle: everything that registers global listeners or
// runs at script-load time. Loaded via plain <script src="site.js"> in
// App.razor, not as a module. Order matters where one script depends on
// another's globals (highlight.js exposes `hljs` before highlight-init
// uses it).
import "../scripts/composer.js";
import "../scripts/composer-paste.js";
import "../scripts/messages.js";
import "../scripts/highlight.min.js";
import "../scripts/highlight-init.js";
import "../scripts/reconnect-auto-reload.js";

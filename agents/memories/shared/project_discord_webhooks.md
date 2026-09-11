---
name: Discord webhooks are named host config
description: discord_list/discord_send post to operator-named Incoming Webhooks; URLs stay on the host and must be discord.com webhook paths
type: project
---

Bundled MCP tools `discord_list` and `discord_send` (`LlamaShears.Api.Tools.ModelContextProtocol.Discord`). The model picks a **name** and message text. It does not pass a webhook URL.

- Bind `Discord:Webhooks` (`Discord__Webhooks__<name>=https://discord.com/api/webhooks/...` in `.local.env`). Secrets do not belong in agent JSON.
- `DiscordWebhookUrl` / `DiscordWebhookOptionsValidator` accept only `https` `discord.com` or `discordapp.com` `/api/webhooks/{id}/{token}` (optional `/api/vN`). No query, fragment, or other host — config is not a general HTTP POST tool.
- One configured webhook: `webhook` may be omitted. Several: name required. Unknown name / empty config / no authenticated agent → loud refuse. Errors and list output never include the URL.
- Send disables Discord `allowed_mentions.parse` (`@everyone` / `@here` / role / user). Content ≤ 2000; username ≤ 80. HTTP client does not follow redirects.
- MCP allow/deny of the tools is the security gate for *whether* an agent can send. There is no per-agent webhook allowlist yet.

Do not add a free-form URL parameter to `discord_send`.

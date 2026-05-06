# ADR-0011: Config root lives in the user profile

Accepted 2026-04-28.

## Context

The host needs a single, well-known directory for user-facing configuration. There are two reasonable cross-platform answers:

1. **Platform-blessed paths** via `Environment.GetFolderPath(SpecialFolder.LocalApplicationData)` — `%LOCALAPPDATA%\LlamaShears` on Windows, `$XDG_DATA_HOME/LlamaShears` (default `~/.local/share/LlamaShears`) on Linux and macOS. This is what `dotnet` itself does for tool installs.
2. **User-profile dotfile** — `~/.llama-shears` on Linux and macOS, `C:\Users\<user>\.llama-shears` on Windows. Resolved with a single `Environment.GetFolderPath(SpecialFolder.UserProfile)` + `.llama-shears`.

The platform-blessed path is the technically pure choice: it follows each OS's own convention, sits next to neighbours that look like it, and matches what other native tools on the platform do. Its cost is discoverability — `%LOCALAPPDATA%` lives under a hidden-by-default `AppData` directory in Explorer; `~/.local/share` is buried two levels deep and outside the muscle memory of users who reach for `~/.foo` first.

This project's primary users are people who already live in their shell and reach for `~` first. The friction the platform-blessed path imposes — "where did the host put my config?" — is the kind of friction [ADR-0009](0009-pragmatism-over-technical-purity.md) treats as worth weighing against an aesthetic-consistency mandate. The accommodation cost is zero: a single expression resolves the path on every platform without an OS branch.

The leading dot has no effect on Windows visibility (NTFS does not hide dotfiles) but keeps the directory name consistent with its Unix sibling. Windows users see `.llama-shears` literally in `C:\Users\<user>\` — out of step with native Windows convention, but the project explicitly accepts that cost. Windows is not the project's primary target.

## Decision

The host's config root is `<user-profile>/.llama-shears`, resolved as:

```csharp
var configRoot = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".llama-shears");
```

- Linux/macOS: `~/.llama-shears`.
- Windows: `C:\Users\<user>\.llama-shears`.

No `RuntimeInformation.IsOSPlatform` branch. No per-OS string. The single expression resolves to the right path on every supported platform.

## Consequences

- Users find the config directory by reflex — `cd ~/.llama-shears` works on every platform the project targets.
- Path resolution is one expression, callable from anywhere. There is no `IConfigPathResolver` indirection because there is no decision left to indirect.
- Windows users see a Unix-style dotfile directory in their user profile. The project accepts this as a deliberate consequence of treating discoverability as more load-bearing than per-platform aesthetic consistency.
- If a future Windows-targeted port becomes a priority, this ADR is revised, not patched around. ADR-0009 explicitly anticipates that revising an accepted ADR is normal.
- The decision is independent of *what* lives under the config root. Any subsystem that needs a writable per-user location uses this root and structures itself underneath it.

This ADR is an instance of [ADR-0009 (Pragmatism over technical purity)](0009-pragmatism-over-technical-purity.md): the technically pure default (platform-blessed paths) is set aside because the friction it imposes on the project's primary users outweighs the aesthetic consistency it preserves, and the accommodation cost is nil.

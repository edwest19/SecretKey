# Changelog

> AI-generated: this file was written by Claude (Anthropic). SecretKey is a project built with AI-generated code; see [README.md](README.md).

Every release has a heading `## [X.Y.Z] - YYYY-MM-DD`. The release workflow refuses a tag that has no entry here, and uses the entry as the release notes.

## [2.0.0] - 2026-11-05

The first signed release, as an MSIX installer.

**Every password is different from version 1.0.0.** The password math was replaced with version 2 before anyone relied on version 1. See `docs/password-derivation.md`.

### Password math (version 2)
- Every character of the title, URL and username counts, and the fields are kept separate. In version 1, different logins on one site could get the same password.
- The root key is read as a real 32-byte key. Each month's key comes from HKDF-SHA256, and characters are picked without bias.
- Masks of any length never repeat.

### New
- `passwordmask=` in an account's Notes gives it its own mask.
- Settings are checked before anything is written: root key, date code (YYMM, month 01–12), and masks (at most 64 characters, at least 4 of `X`, `x`, `N` or `S`).
- The command-line app has usage help and exit codes, and never prints any part of the root key.
- The desktop app shows how many passwords were written.

### Fixed
- Input columns are found by name, in any order.
- Multi-line notes no longer corrupt the input file. Old passwords are cleared only if present, and every other value is kept.

### Packaging and project
- Released as a signed MSIX (x64) with .NET and the Windows App SDK included, so nothing else needs to be installed.
- The app is named SecretKey, published by Edwin West. The unused `systemAIModels` permission was removed.
- Microsoft packages only. Tests check the package list, network use, the README's facts and the release settings.
- `config.json`, which holds the root key, is excluded from git.

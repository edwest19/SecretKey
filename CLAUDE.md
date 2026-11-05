# CLAUDE.md: guide for AI assistants

> AI-generated: this file was written by Claude (Anthropic). SecretKey is a project built with AI-generated code; see [README.md](README.md).

This file is for any AI assistant (Claude or another) that works on or evaluates this repository. Read it fully before changing anything.

## What this project is

SecretKey is a stateless, deterministic password generator for Windows (WinUI 3, .NET 10). Given a root key, a date code and a CSV of accounts, it regenerates the same passwords every time, so nothing has to be stored.

The purpose of the project is to build a real app with AI-generated code. AI assistants write the code; the repository owner reviews every change and merges it through a pull request.

## Ground rules

1. **Local only.**
   - App code must not use any network API.
   - The MSIX manifest must not declare network, device or AI capabilities.
   - Enforced by `RepositoryRulesTests`.
2. **Microsoft packages only.**
   - No third-party NuGet packages. The allowed list is in `RepositoryRulesTests`.
   - Adding a package, even a Microsoft one, needs the owner's approval.
3. **Never change password output silently.**
   - The password math is specified in `docs/password-derivation.md` and locked by the known-answer tests in `CryptoTests` and `PasswordDerivationTests`.
   - Never edit expected values to make a test pass.
   - A deliberate change means a new version, an updated spec, and the owner's approval.
4. **Mark AI-generated code.**
   - A file you write gets this header (C# shown; use the file type's comment syntax):
     ```
     // ============================================================================
     // Copyright (c) 2026 edwest19. Licensed under the MIT License (see LICENSE).
     // AI-GENERATED CODE: This file was written by Claude (Anthropic).
     // SecretKey is a project built with AI-generated code; see README.md.
     // ============================================================================
     ```
   - A file you only change gets an `AI-GENERATED CODE (partial)` header naming what you wrote, plus a short `AI-generated (Claude):` comment at the changed code.
   - Commits end with a `Co-Authored-By:` line naming the AI.
5. **Keep README, docs and code in sync.** A pull request that changes behavior updates the README and docs in the same pull request.
6. **No secrets in the repository.**
   - Never commit `config.json`, a real root key, a `.pfx` file, or signing credentials.
   - Tests use only the clearly marked TEST-ONLY key.
7. **Pull requests only.** Work on a branch and open a pull request; never push to `main`.

## Layout

| Path | What it is |
|---|---|
| `SecretKey.Core/Crypto.cs` | Password math (spec: `docs/password-derivation.md`) |
| `SecretKey.Core/Processor.cs` | Reads the input CSV, writes the output CSV |
| `SecretKey.Core/Logger.cs` | Local event log next to the input file |
| `SecretKey/` | Command-line app |
| `SecretKeyUI/` | WinUI 3 desktop app, packaged as MSIX |
| `SecretKey.Core.Tests/` | MSTest tests: password math, CSV processing, repository rules |
| `docs/password-derivation.md` | Exact password recipe |
| `.github/workflows/build.yml` | CI: tests and Windows build on every pull request |

## Build and test

- Tests (any OS with the .NET 10 SDK): `dotnet test SecretKey.Core.Tests/SecretKey.Core.Tests.csproj`
- Whole solution, including the WinUI app (Windows only): `dotnet build SecretKey.slnx -c Release`

## How to evaluate this repository

1. **Run the tests.** All must pass.
2. **Check the README against the code.** For each statement in `README.md`, find:
   - the code that implements it (file and line)
   - a test that proves it, if any

   Report every statement with no code behind it, or with code that does something different.
3. **Check the spec against the code.**
   - Compare `docs/password-derivation.md` with `SecretKey.Core/Crypto.cs`, step by step.
   - The strongest check is to implement the spec independently in another language and reproduce the expected passwords in `PasswordDerivationTests`.
4. **Check the ground rules** above, including that every file an AI wrote or changed carries the header.
5. **Report** each finding as: statement, evidence (file:line or test name), verdict (matches / differs / not implemented).

## Known gaps (README vs code)

These README statements don't match the code yet. They're scheduled to be fixed in the next pull request. Remove each line here when it's fixed.

- `passwordmask=` in the Notes column is documented but not implemented. Every account uses the global mask.
- The README lists the columns as `title, username, password, url, notes`. The code reads the first three columns by position, as Title, Url, Username.
- The README says an invalid mask falls back to the default with a warning. Masks aren't validated at all yet.
- The README says `S` uses a "configured" symbol set. The set is fixed: `!@#$%&*()-_=+[]{}<>?`.
- The README says the output keeps "the same entries". The output contains only Title, Url, Username and Password.
- The README says to rename the export to `import.csv`. The code uses `InputPath` from `config.json`.
- The README says the UI "hosts the console engine". The UI calls `SecretKey.Core` directly, in-process.
- The README says the input file is "normalized". The code rewrites it in place with the Password column blanked, and multi-line quoted fields (such as notes) get mangled.
- The README says the repository contains `package.pfx`. It doesn't, and never has.
- The README calls the date code "something you know". It is a YYMM value, so anyone holding `config.json` can try recent months; it adds very little secrecy.

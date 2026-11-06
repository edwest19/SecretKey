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
8. **Releases only through the release workflow.**
   - Never sign or publish by hand.
   - The version lives in `Directory.Build.props`, `SecretKeyUI/Package.appxmanifest` (`X.Y.Z.0`) and `CHANGELOG.md`.
   - The manifest's `Publisher` must equal `EXPECTED_SIGNER` in `release.yml`.
   - Workflow actions are pinned to full commit SHAs.
   - Enforced by `ReleaseRulesTests`.

## Layout

| Path | What it is |
|---|---|
| `SecretKey.Core/Crypto.cs` | Password math (spec: `docs/password-derivation.md`) |
| `SecretKey.Core/Processor.cs` | `Run` and `Process`: input CSV to output CSV, `passwordmask=`, clearing old passwords |
| `SecretKey.Core/Csv.cs` | CSV reader and writer (RFC 4180, multi-line fields) |
| `SecretKey.Core/Validation.cs` | Checks for root key, date code and masks |
| `SecretKey.Core/SecretKeyConfig.cs` | `config.json` settings, shared by both apps |
| `SecretKey.Core/Logger.cs` | Local event log next to the input file |
| `SecretKey/` | Command-line app |
| `SecretKeyUI/` | WinUI 3 desktop app, packaged as MSIX (full trust) |
| `SecretKey.Core.Tests/` | MSTest tests (see the claims map below) |
| `docs/password-derivation.md` | Exact password recipe |
| `.github/workflows/build.yml` | CI on every pull request: tests, Windows build, unsigned MSIX packaging |
| `.github/workflows/release.yml` | Release on a `v*` tag: check, test, package, sign (after approval), publish |
| `Directory.Build.props` | The version, shared by all projects |
| `scripts/Test-Msix.ps1` | Checks a packaged MSIX: identity, self-contained, permissions (used by both workflows) |
| `CHANGELOG.md` | Release notes; the release workflow requires an entry for the version |

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

## README claims map

Where each README section is implemented and which tests prove it. `ReadmeClaimsTests` also checks the README's exact facts (symbol set, limits, column names, settings, default mask, spec table) against the code.

| README section | Code | Tests |
|---|---|---|
| About this project: local only, Microsoft packages | `SecretKeyUI/Package.appxmanifest`, all `*.csproj` | `RepositoryRulesTests` |
| Security: `.gitignore` | `.gitignore` | `ReadmeClaimsTests.GitIgnore_ExcludesTheSecretsTheReadmeSaysItDoes` |
| Desktop app | `SecretKeyUI/MainWindow.xaml.cs` (calls `Validation`, `SecretKeyConfig`, `Processor.Run`) | Built by CI; same core code as the tests |
| Command-line app | `SecretKey/Program.cs` (calls `Processor.Run`) | Built by CI; same core code as the tests |
| Configuration | `SecretKeyConfig.cs`, `Processor.Run` | `ProcessorTests.Run_*`, `ProcessorTests.Config*`, `ReadmeClaimsTests.ConfigExample_*` |
| Input file | `Csv.cs`, `Processor.Process` | `CsvTests`, `ProcessorTests` |
| What happens to the input file | `Processor.Process` step 3, `Logger.cs` | `ProcessorTests.OldPasswords_*`, `ProcessorTests.InputWithoutPasswords_IsNotTouched` |
| Output file | `Processor.Process` step 2, `Crypto.NormalizeField/NormalizeUrl` | `PasswordDerivationTests.Csv_*`, `ProcessorTests.Columns_*` |
| Masks | `Validation.MaskError`, `Crypto.MapStreamToMask` | `ValidationTests`, `CryptoTests.MapStreamToMask_*` |
| Per-account masks | `Processor.FindMaskInNotes` | `ProcessorTests.PasswordMask*`, `ProcessorTests.InvalidPasswordMaskInNotes_*` |
| Date code | `Validation.DateCodeError`, `Crypto.DeriveMonthlyMasterKey` | `ValidationTests.*DateCodes*` |
| What changes a password | `Crypto.GeneratePassword` | `PasswordDerivationTests` |
| How passwords are made | `Crypto.cs` | `CryptoTests`, `PasswordDerivationTests`, `ReadmeClaimsTests.Spec_*` |
| Install | `release.yml` (packaging and signing), `Package.appxmanifest` | `ReleaseRulesTests`; the packaged MSIX is built on every pull request |
| Releases | `release.yml`, `Directory.Build.props`, `CHANGELOG.md` | `ReleaseRulesTests` |

## Known gaps (README vs code)

None known. If you find one, list it here and fix it in the same pull request, or in the next one.

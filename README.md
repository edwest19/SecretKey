# SecretKey

<!-- AI-generated: this README was written by Claude (Anthropic). Every statement in it is checked against the code; see CLAUDE.md. -->

SecretKey makes a password for every account in a CSV file, such as an export from 1Password. Each password comes from:

- your **root key**, a random 32-byte secret kept in `config.json`
- a **date code**, the month as `YYMM` (`2611` is November 2026)
- the account's **Title**, **Url** and **Username**, and its **mask** (the password's pattern)

The same inputs always give the same passwords, on any PC, so nothing has to be stored. To recover a password, run SecretKey again. To change every password, use a new date code.

## About this project

- **Built with AI-generated code.** The purpose of SecretKey is to build a real, useful app with AI-generated code. The code is written by AI coding assistants (Claude by Anthropic, and earlier GitHub Copilot). A human, the repository owner, reviews every change and merges it through a pull request.
  - Files Claude wrote or changed say so in a comment at the top of the file.
  - Every commit Claude made ends with a `Co-Authored-By: Claude` line, so `git log` shows exactly which changes were AI-generated.
- **Local only.** SecretKey runs on your PC. Its own code makes no network connections, and its app package asks for no network permissions. Tests check both (`RepositoryRulesTests`). GitHub is used only to store the code and to build releases.
- **Pure .NET, Microsoft packages only.** No third-party packages. The only packages are Microsoft's own:
  - the Windows App SDK and Windows SDK build tools, for the WinUI 3 window and MSIX packaging
  - MSTest, Microsoft's test framework, used by the tests only

  A test fails if any other package is added.
- **Checkable by an AI.** [CLAUDE.md](CLAUDE.md) tells an AI assistant how to build, test, and check this README against the code. `ReadmeClaimsTests` checks the README's facts automatically.

## Install

1. Open the [latest release](https://github.com/edwest19/SecretKey/releases/latest) and download `SecretKey-<version>-x64.msix`.
2. Open the file. Windows installs SecretKey and shows the publisher, Edwin West. The installer is signed, so Windows can confirm who made it and that it hasn't been changed.
3. Start **SecretKey** from the Start menu.

To uninstall, go to **Settings → Apps → Installed apps → SecretKey → Uninstall**.

- The MSIX includes .NET and the Windows App SDK, so nothing else needs to be installed. It runs on 64-bit (x64) Windows.
- Each release also has a `.sha256` file. To check your download, compare it with the output of `Get-FileHash .\SecretKey-<version>-x64.msix` in PowerShell.
- A pre-release (a `-rc` version) has the same package version as the final release. Uninstall it before installing the final one.

## Security: what to protect

- **`config.json` holds your root key.** With `config.json` and your input file, anyone can regenerate every password, for any month. The date code is not a real secret: it's just a month, so it's easy to guess.
  - Keep `config.json` private, for example on an encrypted drive.
  - Back it up. If the root key is lost, the passwords can't be regenerated.
- **Sharing.** If you give someone `config.json` and the input file, for example on a mailed flash drive, you are giving them every password. Encrypt the drive.
- **The output file holds your passwords in plain text.** Import it into your password manager, then delete it.
- **Git.** `.gitignore` excludes `config.json` and `*.pfx`. Never commit a real root key or a signing certificate.

## Desktop app (SecretKeyUI)

1. **Select Folder...** picks your workspace folder. `config.json` is saved there.
2. Use the two **Browse...** buttons to pick the input file and choose where the output file goes.
3. Enter the **Global Password Pattern Mask**. The default is `XxxxxNSxxxNN`.
4. Click **Generate New Key** for a new root key, or paste yours.
5. Click **Save Config**. The root key and mask are checked before anything is saved.
6. Enter the **date code** and click **Run SecretKey**. The status bar shows how many passwords were written.

## Command-line app (SecretKey)

```
SecretKey <YYMM> [workspace folder]
```

It reads `config.json` from the workspace folder. Without a folder, it uses the folder `SecretKey.exe` is in. It prints a summary, never the root key, and exits with code 0 on success or 1 on any error.

## Configuration (`config.json`)

| Setting | Meaning |
|---|---|
| `RootKey` | Your root key: exactly 64 hexadecimal characters (32 bytes). **The secret.** |
| `InputPath` | The input CSV file. |
| `OutputPath` | Where to write the output CSV. Must be a different file from `InputPath`. |
| `PasswordMask` | The mask for accounts without their own `passwordmask=` note. |

A relative path is taken relative to the folder that holds `config.json`. `config.example.json` shows the layout; its placeholder root key is rejected until you replace it.

## Input file

- A CSV file in UTF-8 (with or without a byte-order mark), with a header row.
- Columns are found **by name**, in any order and any letter case:
  - `Title`, `Url` and `Username` are required.
  - `Notes` is optional and is where a `passwordmask=` setting goes.
  - `Password` is optional. Old passwords in it are cleared (see below).
  - Other columns are ignored.
- Quoted fields may contain commas, doubled quotes (`""`) and line breaks, so multi-line notes are fine.
- Blank lines are skipped.
- The run stops, naming the line, for a row with more fields than the header or a row with no Title, Url or Username.
- `sample_input.csv` is an example.

**Nothing is written until the whole input file has been checked.** If anything is wrong, no file is created or changed.

### What happens to the input file

If the `Password` column has any values, SecretKey clears them from the input file after writing the output, so old passwords don't linger. Every other value is kept, including multi-line notes. The new file is written to a temporary file and then swapped in. A line is added to `secretkey_events.log` in the same folder with the time, the output file name and the number of rows, but no passwords. If the `Password` column is empty, the input file is not touched.

## Output file

The output CSV has exactly four columns: `Title,Url,Username,Password`.

- Title, Url and Username are written as used for the password: spaces trimmed, and `https://` added to a URL without `http://` or `https://`.
- The passwords are in plain text. Open the file in your password manager, not a spreadsheet: a spreadsheet may treat a password that starts with `=`, `+`, `-` or `@` as a formula.

## Masks

| Mask character | Gives |
|---|---|
| `X` | an uppercase letter, A–Z |
| `x` | a lowercase letter, a–z |
| `N` | a digit, 0–9 |
| `S` | a symbol from `!@#$%&*()-_=+[]{}<>?` |

Any other visible ASCII character, such as `-` or `_`, is copied into the password as-is. That helps when a site only allows certain symbols.

A mask may have at most 64 characters and needs at least 4 of `X`, `x`, `N` or `S`. Spaces and non-ASCII characters aren't allowed. The default mask `XxxxxNSxxxNN` gives passwords like `Fiipc3]jpv38`.

### Per-account masks (`passwordmask=`)

To give one account its own mask, put `passwordmask=` followed by the mask anywhere in its **Notes**. For example, use `passwordmask=xxxxxxxxNNNN` for a site that allows no symbols.

- The mask ends at the first space or line break.
- `passwordmask` can be in any letter case.
- One per account.
- If it's invalid, the run stops with an error naming the line and the account, and nothing is written.

## Date code

Four digits, `YYMM`, with a month from 01 to 12. A new date code gives every account a new password. Old passwords can always be regenerated with their old date code.

## What changes a password

These change a password: the root key, the date code, the account's Title, Url or Username, and its mask (global or `passwordmask=`). Nothing else does, including Notes text other than `passwordmask=`.

Editing an account's title or URL in your password manager changes its SecretKey password, so keep your input file as the reference.

## How passwords are made

[docs/password-derivation.md](docs/password-derivation.md) gives the exact recipe (version 2), so the passwords can be regenerated without this app. In short: HKDF-SHA256 derives the month's key from the root key, and HMAC-SHA256 turns each account into an unbiased stream of characters. Known-answer tests lock the results.

## Projects

| Project | What it is |
|---|---|
| `SecretKey.Core` | The engine: password math (`Crypto.cs`), CSV reading and writing, settings and checks. |
| `SecretKey` | The command-line app. |
| `SecretKeyUI` | The WinUI 3 desktop app. It calls `SecretKey.Core` directly. |
| `SecretKey.Core.Tests` | MSTest tests: password math, CSV, settings, repository rules and README checks. |

## Building and testing

- Windows, Visual Studio 2026 and the .NET 10 SDK. Open `SecretKey.slnx`.
- Run the tests with **Test → Test Explorer** in Visual Studio, or:

  ```
  dotnet test SecretKey.Core.Tests/SecretKey.Core.Tests.csproj
  ```

- `SecretKeyUI` is packaged as an MSIX (full trust) for a clean install and uninstall. Every pull request packages it, unsigned, and `scripts/Test-Msix.ps1` checks the package: publisher, version, self-contained (no other packages needed), and no permission except `runFullTrust`.

## Releases

Releases are built, signed and published by `.github/workflows/release.yml`, only when a version tag is pushed.

1. Set the version in **three places**. `ReleaseRulesTests` fails if they disagree.
   - `Directory.Build.props`: `X.Y.Z`
   - `SecretKeyUI/Package.appxmanifest`: `X.Y.Z.0`
   - `CHANGELOG.md`: a heading `## [X.Y.Z] - YYYY-MM-DD`, whose text becomes the release notes
2. Push a tag `vX.Y.Z`, or `vX.Y.Z-rc.N` for a pre-release.
3. The workflow then:
   - checks the tag against the version
   - runs the tests
   - packages the MSIX and checks it with `scripts/Test-Msix.ps1`
   - **waits for the owner's approval** in the GitHub environment `release`
   - signs the MSIX with Azure Artifact Signing (account `edwest19`, profile `CodeSigning-Apps`, East US)
   - checks that the signature is valid, timestamped and made by `CN=Edwin West, O=Edwin West, L=Huntington, S=ny, C=US`
   - publishes the GitHub Release with the MSIX and its `.sha256`

Signing uses OpenID Connect, so no password or key is stored in GitHub. Every action is pinned to an exact commit. The workflows turn off the .NET CLI's and winapp's usage telemetry.

## License

MIT. See `LICENSE`.

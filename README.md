# SecretKey (PLP-Evolution)

A deterministic, zero-database password generation utility for Windows built on modern **.NET 10**. 

Unlike traditional cloud-dependent credential platforms, **SecretKey saves no operational context** and maintains no centralized vault database. Instead, the application functions as a high-security batch generator designed to refresh your entire credential suite on a rolling monthly schedule. 

By calculating robust, cryptographically secure passwords on the fly using pure mathematics, it outputs a standardized, session-specific file ready for immediate bulk import into password managers (such as Bitdefender). The core application retains absolutely no persistent data, internal history, or tracking logs once the generation cycle completes.

---

## 🛡️ The Split-Delivery Distribution & Security Model

SecretKey is explicitly optimized for safe deployment, secure credential sharing, and air-gapped protection using a strict dual-channel distribution framework:

1. **The Physical Channel (Something You Have):** A 32-byte cryptographic `RootKey` and the standalone, portable compiled utility are stored entirely offline on a physical USB flash drive, safely distributed via physical transit (such as the USPS).
2. **The Digital Channel (Something You Know):** A shifting, 4-digit numeric `DateCode` (YYMM) alongside the plain-text account configuration metadata input file (`.csv`) is delivered out-of-band via standard digital communication lines (such as email or text).

Because the underlying mathematical engine strictly requires **both** independent components simultaneously to calculate keys, intercepting a digital file transfer or losing a physical flash drive in transit results in **zero risk**. An attacker cannot generate or reconstruct passwords without matching both pieces of the puzzle.

---

## 🚀 Key Features

* **Rolling Monthly Rotations:** Explicitly built to generate distinct, fresh password batches every month. Easily keep your security posture dynamic without manually inventing new keys for every individual profile.
* **Bulk Import Compatibility:** Generates standard, clean output files formatted specifically for rapid import into local credential managers like Bitdefender—bypassing the friction of passkeys or complex multi-factor setups.
* **Frictionless "Forgot Password" Resiliency:** Since credentials are dynamically rotated in mass batches, any individual service or account variance is cleanly managed through native platform password reset loops without breaking global generator synchronization.
* **Historical Time-Travel Recovery:** Need to recover a legacy credential utilized six months ago? Simply pass that specific historical month's `DateCode` (e.g., `2511` for November 2025) as the primary runtime execution argument to instantly recalculate past keys without keeping an active history database.
* **Dynamic Override Masks:** Accommodates restrictive, picky corporate password complexity rules on a per-account basis directly within the input file without altering global mathematical generation constraints.
* **Single-File USB Portability:** Compiles into a solitary, sandboxed executable that runs flawlessly directly from an offline thumb drive without requiring the target host machine to install the .NET SDK or run persistent background processes.

## 📊 Password Generation Architecture

The application operates in a completely predictable, deterministic pipeline:

```text
[ RootKey (USB) ] + [ DateCode (YYMM) ]
       │
       ▼
[ SHA-256 Derivation ]
       │
       ▼
[ Monthly Master Key (32-bytes) ] ──┐
                                    ▼
[ Title + URL + Username ] ──► [ HMAC-SHA256 ] ──► [ Dynamic Pattern Mask ] ──► [ Final Password ]

1. **Monthly Master Key:** A unique 32-byte key is derived using `SHA256(RootKey + DateCode)`.
2. **Fixed Account Block:** The account `Title`, normalized `URL`, and `Username` are concatenated and padded deterministically with a `0xA5` byte pattern to form a fixed data block.
3. **Cryptographic Hashing:** The block is hashed via `HMAC-SHA256` using the Monthly Master Key.
4. **Pattern Mapping:** The resulting hash bytes are mapped sequentially using modulo math against designated character pools guided by a structural mask pattern.

---

## 📂 CSV Input Layout & Custom Overrides

The application accepts an input CSV file containing account details. To allow ultimate formatting freedom, the file natively supports an optional **OverrideMask** column. 

### Expected Header Columns:
* `Title` (Required)
* `URL` or `Website` (Required)
* `Username` (Required)
* `OverrideMask` (Optional - Leave blank to default to the standard 12-character format)

### Example `sample_input.csv`:
```csv
Title,Url,Username,OverrideMask
Verizon,www.verizon.com,mail@jask.com
Secure Bank,https://secure.bank.com,mail@jask.com,xxxxxxxxxxNNNN


### Mask Character Legend:

X = Uppercase Alphabet (A-Z)
x = Lowercase Alphabet (a-z)
N = Numeric Digits (0-9)
S or s = Special Characters (!@#$%\&\*()-\_=+\[]{}<>?)

## Any other character placed in the mask will be treated as a literal character and passed through verbatim.


### 💻 Local Execution Guide

##### 1\. Project Navigation

##### Open your PowerShell terminal and navigate directly to your local project root repository directory where the source code is located:

##### 

##### PowerShell

##### cd C:\\Users\\Ed\\source\\repos\\SecretKey

##### 2\. Standard Execution

##### Run the compiler and pass the targeting 4-digit DateCode (YYMM) as the primary execution argument. For example, to generate passwords for June 2026:

##### 

##### PowerShell

##### dotnet run -- 2606

### 💾 Compilation for Portable USB Deployment

##### To package this tool so it runs perfectly from a portable flash drive on a target machine without requiring any prerequisite software installations, publish it as a self-contained, trimmed, single-file executable:

##### 

##### PowerShell

##### dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true

##### Copy the compiled SecretKey.exe file located inside your output bin\\Release\\net10.0\\win-x64\\publish\\ folder directly onto the root directory of your flash drive for an entirely modular, portable workstation utility.



### 🛡️ Security Disclaimer

##### Treat your production RootKey configuration with absolute secrecy. If the repository is public, ensure that your true production credentials remain completely decoupled from the source tree. This repository utilizes a rigorous .gitignore structure to guarantee development keys never leak into the open-source history.



## 🤖 AI Assistant Acknowledgement

This README and foundational blocks of the modular core architecture were successfully implemented and updated with cooperative programming assistance from an AI programming partner (**GitHub Copilot**). All cryptographic handling, CSV parsing pipelines, and dynamic output adjustments have been fully reviewed, refined, and verified locally.



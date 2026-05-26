# SecretKey (PLP-Evolution)

A deterministic, zero-database password generation utility for Windows built on modern **.NET 10**. 

Unlike traditional password managers that store encrypted vaults in the cloud or maintain a persistent local history database—which can be leaked, intercepted, or brute-forced—**SecretKey saves no operational context**. The application architecture is completely memory-centric: it calculates robust, cryptographically secure passwords on the fly using pure mathematics, writes them cleanly to a local output file for immediate session use, and retains absolutely no persistent data, history, or background configuration logs. 

Once the process finishes and the output file is handled, the application leaves no footprint. Passwords can be replicated or recovered at any time simply by recalling a 4-digit datecode (YYMM).

---

## 🛡️ The Two-Factor Operational Security Model

SecretKey is explicitly engineered for maximum operational security (OpSec) using an air-gapped, split-delivery model combining **something you have** with **something you know**:

1. **Something You Have (Physical Component):** A 32-byte cryptographic `RootKey` stored entirely offline on an encrypted physical flash drive.
2. **Something You Know (Digital Component):** A shifting 4-digit `DateCode` (YYMM) delivered via an out-of-band communication channel (such as a text message, secure email, or phone call).

Because the mathematical engine requires *both* components simultaneously to derive keys, losing a flash drive or having an email intercepted results in **zero risk**. An attacker cannot generate passwords without both pieces of the puzzle.

---

## 🚀 Key Features

* **Zero-Context Footprint:** No background processes, no internal databases, no tracking, and zero cloud dependencies. It only interacts with the files you explicitly provide.
* **Ephemeral Output Workflow:** Designed for a "generate-use-and-delete" model. Users generate their required passwords for the session, copy what they need, and securely delete the output file—returning the host machine to a state of zero exposure.
* **Dynamic Override Masks:** Accommodates restrictive, picky corporate password rules on a per-account basis directly within the input file without breaking global generation logic.
* **Time-Travel Password Recovery:** Need to recover a password used six months ago? Simply pass that past month's `DateCode` into the execution argument to instantly calculate historical credentials without maintaining a history log.
* **Single-File Portability:** Can be compiled into a solitary, standalone executable that runs flawlessly from an offline USB drive without requiring the target machine to install the .NET SDK.

# 

### \## 📊 Password Generation Architecture

# 

##### The application operates in a completely predictable, deterministic pipeline:

##### \[ RootKey (USB) ] + \[ DateCode (YYMM) ]
##### │
##### ▼

##### \[ SHA-256 Derivation ]
##### │
##### ▼

##### \[ Monthly Master Key (32-bytes) ] ──┐
##### ▼

##### \[ Title + URL + Username ] ──► \[ HMAC-SHA256 ] ──► \[ Dynamic Pattern Mask ] ──► \[ Final Password ]

##### 
##### 

##### 1\. \*\*Monthly Master Key:\*\* A unique 32-byte key is derived using `SHA256(RootKey + DateCode)`.

##### 2\. \*\*Fixed Account Block:\*\* The account `Title`, normalized `URL`, and `Username` are concatenated and padded deterministically with a `0xA5` byte pattern to form a fixed 32-byte data block.

##### 3\. \*\*Cryptographic Hashing:\*\* The block is hashed via `HMAC-SHA256` using the Monthly Master Key.

##### 4\. \*\*Pattern Mapping:\*\* The resulting hash bytes are mapped sequentially using modulo math against designated character pools guided by a structural mask pattern.

##### 

##### \---



### \## 📂 CSV Input Layout \& Custom Overrides



##### The application accepts an input CSV file containing account details. To allow ultimate formatting freedom, the file natively supports an optional \*\*OverrideMask\*\* column. 

##### 

#### \### Expected Header Columns:

##### \* `Title` (Required)

##### \* `URL` or `Website` (Required)

##### \* `Username` (Required)

##### \* `OverrideMask` (Optional - Leave blank to default to the standard 12-character format)

##### 

#### \### Example `sample\_input.csv`:

##### ```csv

##### Title,Url,Username,OverrideMask

##### Verizon,\[https://www.verizon.com](https://www.verizon.com),mail@jask.com

##### HardcoreBank,\[https://secure.bank.com](https://secure.bank.com),mail@jask.com,xxxxxxxxxxNNNN



##### Mask Character Legend:

##### X = Uppercase Alphabet (A-Z)

##### 

##### x = Lowercase Alphabet (a-z)

##### 

##### N = Numeric Digits (0-9)

##### 

##### S or s = Secure Special Characters (!@#$%\&\*()-\_=+\[]{}<>?)

##### 

##### Any other character placed in the mask will be treated as a literal character and passed through verbatim.



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



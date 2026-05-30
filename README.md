# SecretKey

A deterministic, stateless password generator utility designed to securely process account credentials using cryptographic derivation. By linking unique account traits with a protected master key, `SecretKey` eliminates the need to store passwords in a traditional database.

## 🏛️ Solution Architecture

The application is structured as a modular, enterprise-ready solution inside Microsoft Visual Studio to separate core engine logic from the delivery interfaces:

* **SecretKey.Core**: A reusable C# Class Library (.NET 10) containing the cryptographic transformation engines and the robust CSV processing state machine.
* **SecretKey.Cli**: A standalone Command Line Interface Console Application (.NET 10) that consumes the Core engine to process bulk operations.

---

## 🔒 Cryptographic Design

The core processing logic utilizes an architectural configuration tailored for highly secure, low-level transaction safety:

1.  **Input Derivation**: The system extracts the `Title`, `Url`, and `Username` from a record and standardizes them into a fixed data block.
2.  **HMAC Processing**: The fixed block is passed through an HMAC-SHA256 pipeline fueled by a high-entropy monthly master key.
3.  **Pattern Mapping**: The resulting 32-byte cryptographic blob is mapped deterministically against an alphanumeric structure dictated by a custom validation mask.

---

## ⚙️ Configuration (config.json)

The application avoids default hardcoded rules, instead retrieving operational settings directly from a local configuration file. 

Example configuration layout:

```json

{
  "RootKey": "your 32 btye hex key here",
  "InputPath": "C:\\Users\\Ed\\Documents\\SecretKey\\input.csv",
  "OutputPath": "C:\\Users\\Ed\\Documents\\SecretKey\\output.csv",
  "PasswordMask": "XxxxxNSxxxNN"
}

```

⚠️ Security Notice: Ensure config.json is added to your local .gitignore ruleset to prevent exposing sensitive environment paths or cryptographic previews to cloud repository histories.

---

## 🚀 Standard Operational Workflow

To execute the stateless generation loop on fresh vault data, follow these steps sequentially:

1. **Export from 1Password**: Open 1Password and export your active vault items as a `.csv` file.
2. **Stage the File**: Save the exported file directly into your local database workspace:
   `C:\Users\you\Documents\SecretKey\`
3. **Rename the Input**: Rename the exported file explicitly to **`import.csv`** to match the application engine routing.
4. **Execute via CLI**: Open your terminal window (or run natively via your Visual Studio profile launch configurations) and fire the utility with your active 4-digit processing DateCode argument:
   ```shell
   SecretKey.exe 2606

## 🏎️ Features & Robustness

* **State Machine CSV Parser**: Built to handle complex real-world exports from 1Password. The parser tracks character states natively, meaning multi-line notes wrapped in double quotes (such as two-step authentication codes or custom comments) are preserved horizontally without corrupting adjacent database records.
* **Dynamic Mask Targeting**: The engine automatically checks account notes for overrides matching the pattern `passwordmask=[CustomMask]`. If found, it drops the default mask to apply the account's specific rule dynamically.
* **In-Place Validation**: Safeguards your run pipeline by gracefully reporting malformed data loops instead of crashing due to out-of-bounds column fragments.

---

## 🛠️ Development & Execution Natively in MS Visual Studio

### Passing Runtime Arguments
To run the processing execution loop inside Visual Studio without using an external terminal profile:
1. Right-click the **SecretKey.Cli** project and select **Properties**.
2. Navigate to **Debug** -> **Open debug launch profiles UI**.
3. Enter your active `DateCode` argument (e.g., `2606`) inside the **Command line arguments** field.
4. Press **F5** to compile and run the process natively under the interactive debugger.

### Build Pipeline Status
* **Target Framework**: .NET 10.0 (Current)
* **IDE Support**: Microsoft Visual Studio 2022+
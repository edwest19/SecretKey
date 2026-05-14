# SecretKey (PLP-Evolution)

Brief deterministic password generator for Windows (.NET 10) that derives a Monthly Master Key from a RootKey + DateCode and produces per-account passwords from an input CSV.

Quick start
- Edit config.json in the project folder (it is loaded from AppDomain.CurrentDomain.BaseDirectory). Replace paths as needed. Example values are provided in config.example.json.
- Set RootKey in config.json (the project currently tracks config.json unless you add it to .gitignore).
- Prepare input.csv (see CSV format below) and set InputPath in config.json.
- From the project folder run:
  dotnet build
  dotnet run -- 2605
  Replace 2605 with the YYMM DateCode you want to generate for.

CSV format
- Header required. Expected columns in order: Title,Website,Username,Password Regex
- The app appends a Password column to the output CSV and writes it to OutputPath.

Behavior notes
- Key derivation: Monthly Master Key = SHA256(RootKey + DateCode) (32 bytes).
- For each CSV row the app concatenates Title+Website+Username, creates a deterministic 32-byte block (UTF-8, padded with 0xA5), runs HMAC-SHA256 with the Monthly Master Key, and converts the hash to a password string.
- If the password does not match the provided Password Regex, the app deterministically re-hashes (HMAC of previous hash) until the regex matches or a limit of attempts is reached.

Security
- Storing a RootKey in config.json makes generated passwords recoverable by anyone with access to that file. If you do not want the RootKey published, remove it before committing or add config.json to .gitignore and use config.example.json as a template.

Development
- Target: .NET 10 (net10.0). The project is a console application and intended to be portable and store-ready.

If you want me to commit and push this README for you, say so.


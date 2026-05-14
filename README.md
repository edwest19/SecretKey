# SecretKey (PLP-Evolution)

A stateless, deterministic password generator for Windows (.NET 10). The app derives a Monthly Master Key from a RootKey + DateCode and produces per-account passwords from an input CSV. Passwords are reproducible when run with the same RootKey and DateCode.

Contents
- Program.cs — entrypoint and orchestration
- Crypto.cs — key derivation and HMAC utilities
- Processor.cs — CSV parsing, password generation, and output writer
- config.json / config.example.json — runtime configuration

Quick start
1. Edit config.json in the project folder (loaded from AppDomain.CurrentDomain.BaseDirectory). A template is included as config.example.json.
2. Ensure InputPath points to your input CSV and OutputPath points to where output.csv should be written.
3. Set RootKey in config.json (or keep the placeholder in config.example.json).
4. Build and run, passing the DateCode (YYMM) as the first argument. Example:

   dotnet build
   dotnet run -- 2605

   Replace 2605 with the YYMM code for the month you want to generate.

CSV format
- The input CSV must include a header row. Expected columns (in order):
  1) Title
  2) Website
  3) Username
  4) Password Regex

- Example input.csv row:

  Title,Website,Username,Password Regex
  "Verizon","https://verizon.com","edwest@jask.com","^(?=.*[A-Z])(?=.*[a-z])(?=.*\d).{12,}$"

- The app appends a Password column to each row and writes the result to OutputPath.

Password generation algorithm (summary)
- Monthly Master Key: SHA256(RootKey + DateCode) (32 bytes).
- Per row:
  1) Concatenate Title + Website + Username (UTF-8).
  2) Produce a deterministic 32-byte block: copy bytes; if shorter, pad with 0xA5.
  3) Compute HMAC-SHA256(block) using Monthly Master Key.
  4) Map the hash to a password string (default length 16) using a deterministic alphabet mapping.
  5) If the password does not match the Password Regex, deterministically re-hash (HMAC of previous hash) and repeat until the regex matches or a maximum attempt count is reached.

Notes on regex matching
- The program uses Regex.IsMatch(password) by default. If you need the regex to match the entire password, anchor it with ^...$ in the CSV.

Configuration tips
- If you plan to publish the repository, remove or exclude config.json because it contains the RootKey. Use config.example.json as the public template.
- The code currently ignores config.json via .gitignore; keep a local copy with your real RootKey and use the example for public docs.

Extensibility
- Password length and alphabet can be changed in Crypto.HashToPassword and Processor.Process.
- The CSV parser is intentionally simple; replace with a robust CSV library (CsvHelper) if you need full CSV dialect support.

Development
- Target framework: net10.0
- To run tests or CI, add a GitHub Actions workflow (I can add one on request).

Security reminder
- Publishing the RootKey or config.json makes generated passwords recoverable. Treat the RootKey as a secret in production use.

Contributing
- Fork, create a feature branch, and open a pull request. If you want help adding unit tests or CI, I can add a starter workflow.

License
- Add a LICENSE file as needed for your project.


# How SecretKey derives passwords (version 2)

This is the exact recipe SecretKey uses. With it, anyone holding the root key and date code can regenerate the passwords, even without this app. The known-answer tests in `SecretKey.Core.Tests` lock it; a change to anything here must become a new version, never an edit to this one.

## Inputs

- **Root key**: 64 hexadecimal characters (upper or lower case), decoded to 32 bytes. Surrounding whitespace is ignored. Anything else is rejected.
- **Date code**: a string such as `2611`, used exactly as given.
- **Per account**: Title, URL and Username from the input CSV.
- **Mask**: the password pattern, e.g. `XxxxxNSxxxNN`.

## Step 1: monthly key

```
monthlyKey = HKDF-SHA256(IKM  = rootKey (32 bytes),
                         salt = empty,
                         info = UTF-8("SecretKey v2 monthly key " + dateCode),
                         L    = 32)
```

HKDF is defined in RFC 5869. An empty salt means a salt of 32 zero bytes.

## Step 2: normalize the account fields

For Title, URL and Username:

1. Remove leading and trailing whitespace.
2. Apply Unicode normalization form C (NFC), so "é" typed as one character or as "e" plus an accent gives the same result.

Then, if the URL is not empty and does not start with `http://` or `https://` (case-insensitive), prefix it with `https://`. Letter case is otherwise kept as typed.

Changing any of Title, URL, Username or the mask changes that account's password.

## Step 3: account input

Write these five strings, in order, each as a 4-byte big-endian length followed by its UTF-8 bytes:

1. `SecretKey v2 password`
2. Title
3. URL
4. Username
5. Mask

Call the result `accountInput`.

## Step 4: byte stream

```
stream = HMAC-SHA256(monthlyKey, accountInput || BE32(0))
      || HMAC-SHA256(monthlyKey, accountInput || BE32(1))
      || ...
```

`BE32(n)` is `n` as a 4-byte big-endian number. Use as many blocks as needed.

## Step 5: build the password from the mask

Go through the mask one character at a time:

| Mask character | Set (in this order) | Set size | Accept bytes below |
|---|---|---|---|
| `X` | `ABCDEFGHIJKLMNOPQRSTUVWXYZ` | 26 | 234 |
| `x` | `abcdefghijklmnopqrstuvwxyz` | 26 | 234 |
| `N` | `0123456789` | 10 | 250 |
| `S` | `!@#$%&*()-_=+[]{}<>?` | 20 | 240 |

For `X`, `x`, `N` or `S`, take the next byte `b` from the stream. If `b` is below the limit in the table, output `set[b mod size]`; otherwise discard it and take the next byte. This makes every character in a set equally likely.

Any other mask character is copied into the password as-is and uses no stream bytes.

## What changed from version 1 (v1.0.0)

Version 1 hashed only the first 32 bytes of Title+URL+Username, joined with no separator. Different logins on the same site could therefore get the same password, and so could accounts whose fields ran together. Version 1 also favored some characters slightly and repeated masks longer than 32 characters. Version 2 fixes all of these. Every password differs from version 1.

# Security Baseline

- Passwords are never reversibly encrypted. Local/fallback passwords use PBKDF2-HMAC-SHA256 with per-user random salts and 120,000 iterations.
- The administrator can reset a password to a temporary value but cannot view the existing password.
- Password length, complexity, history, expiry, and mandatory first-login changes are disabled. Five failed login attempts still trigger a 15-minute lockout.
- Application access is checked again on direct URL requests, not only when rendering portal icons.
- SQL commands use parameters. Running-number generation uses a transaction and update locks.
- Authentication/session cookies must use HTTPS and `requireSSL=true` in UAT/Production.
- Use a dedicated IIS application-pool identity and grant only the required database permissions.
- Store uploaded files outside the public web root in Production. Validate file signatures, content type, size and malware status before release.
- Complete Microsoft Entra ID/OpenID Connect integration before disabling local sign-in. Store client secrets in IIS/environment secret storage, not source control.

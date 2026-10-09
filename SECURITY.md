# Security Policy

## Supported versions

Security fixes are made for the latest release only. If you run an older version, upgrade first; [MIGRATION.md](MIGRATION.md) describes the upgrade to the .NET 10 release.

| Version | Supported |
|---|---|
| Latest release (.NET 10) | Yes |
| Earlier releases (.NET 9 and older) | No |

## Reporting a vulnerability

**Please do not report security vulnerabilities in public issues, discussions or pull requests.**

Report them privately through GitHub: open the [Security tab](https://github.com/ertugrulozcan/ErtisAuth/security) of the repository and choose **Report a vulnerability**. The report is visible only to the maintainers.

Please include:

- The affected version or commit.
- A description of the vulnerability and its impact: what an attacker can do, and under which conditions (for example which role or which kind of token they need).
- Steps to reproduce, ideally with the requests that demonstrate it.
- Any known workaround.

## What to expect

- Your report is acknowledged, and you are kept informed while it is investigated and fixed.
- Once a fix is released, the vulnerability is published as a GitHub security advisory. You are credited in the advisory unless you prefer to stay anonymous.
- Please give us a reasonable time to release a fix before you disclose the vulnerability publicly.

## Scope

In scope:

- The ErtisAuth server (`ErtisAuth.WebAPI` and the libraries it is built from).
- The client packages `ErtisAuth.Sdk` and `ErtisAuth.Sdk.AspNetCore`.

Out of scope:

- Issues that require an attacker who already controls the server, the database or an administrator account.
- Weaknesses of a particular deployment, such as a missing TLS configuration, an exposed MongoDB or the lack of rate limiting at the gateway; see the recommendations below.
- Denial of service through request volume.

## Deployment recommendations

ErtisAuth leaves a few protections to the infrastructure it runs in:

- **TLS:** terminate HTTPS at the ingress or reverse proxy; ErtisAuth itself listens on plain HTTP.
- **Rate limiting:** limit the anonymous endpoints, such as `generate-token`, the OTP and password reset endpoints and the device token endpoint, at the gateway.
- **MongoDB:** keep the database on a private network, and give ErtisAuth a user with access to its own database only.
- **Secrets:** use a membership secret key of at least 32 bytes, rotate application secrets when someone who knew them leaves, and keep the one-time setup token out of logs and scripts.

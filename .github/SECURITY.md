# Security Policy

## Reporting a Vulnerability

**DO NOT open a public GitHub issue for security vulnerabilities.**

Please report security issues to the maintainers privately:

1. **Email:** Report to the repository maintainers via private email
2. **GitHub Security Advisory:** Use GitHub's private vulnerability reporting feature
   - Visit: https://github.com/RKY2023/OpenMultiSeat/security/advisories
   - Click "Report a vulnerability" to submit privately

## What to Include

When reporting a vulnerability, please include:

- Description of the vulnerability
- Affected versions (e.g., v0.1.0-alpha or main)
- Steps to reproduce
- Potential impact
- Suggested fix (if available)

## Response Timeline

We aim to:
- Acknowledge receipt within 48 hours
- Provide an initial assessment within 5 days
- Release a patch or mitigation within 30 days (when possible)
- Credit the reporter (with permission)

## Security Considerations

### Phase 0-4 (Current Development)

Currently in early development phases. Security review is planned for Phase 9 before v1.0 release.

**Known Limitations:**
- Input isolation (Phase 5) not yet implemented
- No kernel driver signing (Phase 8)
- No privileged IPC security hardening yet (Phase 3+)
- Code review security gates under development

### Critical Security Areas

1. **Kernel Drivers** (Phase 5+)
   - Will require code signing
   - Subject to Windows driver signing requirements
   - Thorough security review before release

2. **Privileged Service** (Phase 3+)
   - IPC ACL security critical
   - Input validation required for all service commands
   - No arbitrary command execution

3. **Session Management** (Phase 3)
   - Secure token handling
   - Proper user impersonation checks
   - Session isolation validation

## Supported Versions

| Version | Supported          | End of Life |
|---------|-------------------|-------------|
| 0.1.x   | Development       | Not released|
| 0.x.x   | Not released yet  | TBD        |
| 1.0.x   | When released     | TBD        |

## Dependencies

We use:
- .NET 9 SDK (Microsoft supported)
- Windows 11 APIs (Microsoft supported)
- Open-source libraries (reviewed for vulnerabilities)

All dependencies are tracked in `.csproj` files and scanned for vulnerabilities via:
- GitHub Dependabot
- NuGet vulnerability scanner
- Regular security audits

## Development Security Practices

1. **Code Review:** All PRs require review before merge
2. **Signed Commits:** All commits must be GPG/SSH signed
3. **Branch Protection:** Main branch requires PR + review
4. **CI/CD:** GitHub Actions verify code on every push
5. **Dependency Scanning:** Automated vulnerability detection
6. **Secret Scanning:** Credentials detection enabled

## Responsible Disclosure

We follow responsible disclosure principles:

- Vulnerabilities will be patched before public disclosure
- Reporters credited (unless they request anonymity)
- Coordinated disclosure timeline respected
- No public details until patch is available

## Questions?

For security-related questions (non-vulnerability), open a GitHub Discussion or contact maintainers privately.

---

**Last updated:** 2026-08-14  
**Next security review planned:** Phase 9 (v1.0 release)

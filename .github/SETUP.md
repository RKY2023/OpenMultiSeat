# GitHub Repository Setup Guide

**Repository:** https://github.com/RKY2023/OpenMultiSeat

## ✓ Automatically Configured

The following has been automatically set up via git and GitHub:

### Repository Content
- ✓ All source code pushed (master + worktree-phase0-device-enum branches)
- ✓ All documentation files
- ✓ GitHub Actions workflows (CI/CD, Release)
- ✓ Issue templates (Bug report, Feature request)
- ✓ Pull request template
- ✓ Security policy (SECURITY.md)
- ✓ Code owners file (CODEOWNERS)
- ✓ Editor configuration (.editorconfig)
- ✓ Changelog (CHANGELOG.md)

### Version Tags (GPG-Signed)
- ✓ v0.1.0-dev (development version)
- ✓ v0.1.0-alpha (first alpha release)

### Git Configuration
- ✓ All commits GPG/SSH signed with SSH key
- ✓ Conventional commit messages enforced locally
- ✓ Pre-push hook prevents direct main/master pushes

### GitHub Actions
- ✓ CI pipeline (build, test, code quality)
- ✓ Security scanning (Trufflehog, NuGet vulnerabilities)
- ✓ Dependency review
- ✓ Documentation checks
- ✓ Release workflow

---

## ⚙️ Manual GitHub Web Setup Required

The following requires admin access via GitHub's web interface.

### 1. Branch Protection Rules

**URL:** https://github.com/RKY2023/OpenMultiSeat/settings/branches

Create a branch protection rule for `master`:

```
Branch name pattern: master

Protection settings:
☑ Require a pull request before merging
    ☑ Dismiss stale pull request approvals when new commits are pushed
    ☑ Require code owner reviews
    ○ Required number of approvals to merge: 1

☑ Require status checks to pass before merging
    ☑ Require branches to be up to date before merging
    Status checks required (add these as they appear in CI):
    - build (Windows)
    - code-quality
    - security
    - docs

☑ Require code owner reviews before merging

☑ Require signed commits

☑ Restrict who can push to matching branches
    (Optional - allows admins/designated users only)

Do not allow bypassing the above settings
```

### 2. Repository Settings

**URL:** https://github.com/RKY2023/OpenMultiSeat/settings

Configure:

```
Basic Settings:
- Description: "Open-source Windows multi-seat system - ASTER alternative"
- Homepage: https://github.com/RKY2023/OpenMultiSeat
- Repository topics: windows, multiseat, aster, open-source

Features:
☑ Issues (Enable)
☑ Discussions (Enable)
☑ Projects (Enable) 
☑ Wiki (Disable)
☑ Sponsorships (Optional)

Pull Requests:
☑ Allow auto-merge (Optional)
☑ Allow squash merging
☑ Allow rebase merging  
☑ Allow merge commits
☑ Automatically delete head branches (Recommended)
```

### 3. Secrets and Credentials

**URL:** https://github.com/RKY2023/OpenMultiSeat/settings/secrets

No secrets configured yet. If needed later, add:
- `NUGET_AUTH_TOKEN` (for package publishing)
- `CODESIGN_CERTIFICATE` (for driver signing in Phase 8)

### 4. Deploy Keys

**URL:** https://github.com/RKY2023/OpenMultiSeat/settings/keys

The SSH key used (`id_ed25519_raj`) is already associated with your GitHub account. No additional setup needed.

### 5. Code Security

**URL:** https://github.com/RKY2023/OpenMultiSeat/security

Enable these security features:

```
Security Scanning:
☑ Enable Dependabot (for dependency updates)
☑ Enable secret scanning
☑ Enable push protection (blocks commits with secrets)
☑ Enable security advisories

Code Scanning (optional):
- GitHub Advanced Security (if available on your plan)
- Custom CodeQL queries via Actions
```

### 6. GitHub Project Board (Phase Tracking)

**URL:** https://github.com/RKY2023/OpenMultiSeat/projects

Create a new project board:

```
Project name: OpenMultiSeat Development Phases
Template: (Custom - Kanban or Table view)

Columns/Status:
1. Not Started (Phase 0-9)
2. In Progress (Current development)
3. In Review (PR review stage)
4. Done (Completed phases)
5. Blocked (Needs discussion)

Add issues/cards for each phase:
- Phase 0: Hardware Discovery ✓ DONE
- Phase 1: Device Discovery
- Phase 2: Seat Configuration
- Phase 3: Session Management
- Phase 4: Display Management
- Phase 5: Input Isolation (CRITICAL)
- Phase 6: Audio
- Phase 7: Admin GUI
- Phase 8: Installer
- Phase 9: Reliability & Security
```

### 7. Issue Templates Configuration

**URL:** https://github.com/RKY2023/OpenMultiSeat/settings/issue_templates

The templates are already in `.github/ISSUE_TEMPLATE/`:
- `bug_report.md` ✓
- `feature_request.md` ✓

Verify they appear in the "New Issue" dialog. You can:
- Customize the prompts
- Add additional templates (e.g., "Enhancement", "Documentation")
- Set a default template

### 8. Pull Request Template

The PR template is already configured at `.github/pull_request_template.md`.

It will appear when creating PRs. You can:
- Customize the questions
- Add additional checkboxes
- Link to issue templates

---

## 🔄 Release Process

### Creating a Release

Use the automated release workflow:

1. Go to **Actions** → **Release** workflow
2. Click "Run workflow"
3. Enter version (e.g., `0.1.0-alpha`, `0.2.0-beta`, `1.0.0`)
4. Set pre-release flag (true for alpha/beta/rc, false for stable)
5. Workflow will:
   - Validate version format
   - Build and test on Windows
   - Create GPG-signed git tag
   - Generate release notes
   - Create GitHub Release
   - Post to Discussions (optional)

### Manual Release (if workflow fails)

```bash
# On master branch
VERSION="0.1.0-alpha"

# Create signed tag
git tag -s -m "Release v$VERSION" v$VERSION

# Push tag (triggers release creation)
git push origin v$VERSION

# Create release on GitHub manually if needed
gh release create v$VERSION \
  --title "Release v$VERSION" \
  --generate-notes \
  --draft=false
```

---

## 🎯 Quality Gates Summary

### Continuous Integration

Every push and PR runs:

- **Build:** Debug + Release configurations on Windows
- **Tests:** All unit and integration tests
- **Code Quality:** Format checking, style validation
- **Security:** NuGet vulnerability scan, secret detection
- **Documentation:** Verify required docs exist
- **Linting:** Check for TODO/FIXME comments

### Before Merging to Master

Requirements enforced:
1. ✓ All GitHub Actions pass
2. ✓ Signed commit (git -S)
3. ✓ Code review approval (1 required)
4. ✓ Code owner approval (CODEOWNERS)
5. ✓ Branch up-to-date with master
6. ✓ No unresolved conversations

---

## 📊 Monitoring

### GitHub Insights
- **Insights → Network:** View branch relationships
- **Insights → Traffic:** Monitor clone/visit rates  
- **Insights → Community:** Track contributions

### Actions Dashboard
- **Actions:** View all workflow runs and logs
- **Security → Security tab:** View security alerts

### Issues & Discussions
- **Issues:** Track bugs and feature requests
- **Discussions:** Community questions and announcements

---

## 🔐 Security Checklist

Before v1.0.0 release, verify:

- [ ] All commits are GPG-signed
- [ ] Branch protection enforced on master
- [ ] Required reviews enabled
- [ ] Dependabot alerts reviewed
- [ ] Security policy published (SECURITY.md)
- [ ] CHANGELOG.md up-to-date
- [ ] License file present (LICENSE)
- [ ] CODEOWNERS defined
- [ ] Contributing guidelines clear (CONTRIBUTING.md)
- [ ] No secrets in repository (pre-commit hooks work)

---

## 📝 Phase 1 Next Steps

When starting Phase 1:

1. Create GitHub Issues for all Phase 1 tasks
2. Create a GitHub Project card for Phase 1
3. Branch from master: `git checkout -b feature/phase-1-device-discovery`
4. Implement Phase 1 features
5. Push to GitHub: `git push -u origin feature/phase-1-device-discovery`
6. Create PR to master
7. Wait for reviews and all checks
8. Merge when approved
9. Create release tag: `git tag -s v0.2.0-beta -m "Phase 1 complete"`

---

## 🆘 Troubleshooting

### CI Workflow Failing

Check **Actions → [workflow name]** for detailed logs.

Common issues:
- Missing .NET SDK (should auto-install)
- Tests failing on Windows-specific APIs (expected on Linux)
- Secret scanning false positives (mark as dismissed)

### Branch Protection Not Enforcing

Verify at **Settings → Branches → master**:
- Status checks are entered correctly
- All required checks are actually running in CI

### GPG Signature Verification

If commits don't show as verified:
- Verify SSH key is added to GitHub: **Settings → SSH keys**
- Ensure commits are signed: `git config --global commit.gpgsign true`
- Check signing key ID: `git config --global user.signingkey`

---

## 📚 Additional Resources

- [GitHub Docs - Branch Protection](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches)
- [GitHub Actions Docs](https://docs.github.com/en/actions)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)

---

**Setup Date:** 2026-08-14  
**Repository:** https://github.com/RKY2023/OpenMultiSeat  
**Status:** Phase 0 complete, ready for Phase 1

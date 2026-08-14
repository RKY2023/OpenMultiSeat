# Contributing to OpenMultiSeat

## Code Style
- C# 12+ with nullable reference types enabled
- `nullable: enable` in all projects
- Consistent naming: PascalCase for public members, camelCase for locals
- Use `required` keyword for mandatory properties
- Sealed classes by default

## Commits
- Conventional Commits: `feat:`, `fix:`, `docs:`, `test:`, `refactor:`, `chore:`
- Clear subject lines (50 chars max)
- Detailed description if needed
- Reference issues/PRs

## Testing
- Unit tests in `tests/OpenMultiSeat.Tests/`
- MSTest framework
- Test coverage for core logic

## Architecture Guidelines
1. Keep service layer (SYSTEM) minimal
2. Prefer user-mode solutions over kernel drivers (Phase 5+)
3. Modular design: each component replaceable
4. Always validate IPC input
5. Use interfaces for testability
6. Log structured events

## Pull Requests
- One feature per branch
- Tests passing
- Documentation updated
- Code review before merge

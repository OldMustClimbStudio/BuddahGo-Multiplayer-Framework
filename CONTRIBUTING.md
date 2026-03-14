# Contributing

## Branch Naming

- `feature/<topic>`
- `fix/<topic>`
- `chore/<topic>`
- `docs/<topic>`

## Commit Message Convention

Use conventional prefixes:

- `feat:` new functionality
- `fix:` bug fix
- `refactor:` internal code change without behavior change
- `chore:` maintenance updates
- `docs:` documentation updates

Examples:

- `feat: add steam lobby invite flow`
- `fix: avoid null transport during startup`

## Pull Request Expectations

- Explain what changed and why
- List testing steps
- Mention impacted scenes or scripts
- Keep PR focused and reasonably small

## Unity-Specific Rules

- Always commit related `.meta` files
- Do not commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`
- Avoid editing auto-generated `.csproj`/`.sln`

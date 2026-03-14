# Steam Multiplayer Framework (Unity)

A Unity-based multiplayer framework using FishNet networking and Steam P2P integration (FishyFacepunch + Facepunch.Steamworks).

## Tech Stack

- Unity: `2022.3.55f1c1`
- Networking: `FishNet`
- Transport: `FishyFacepunch`
- Steam SDK wrapper: `Facepunch.Steamworks`
- Render Pipeline: `URP`

## Repository Layout

- `Assets/Scripts/`: game and networking scripts
- `Assets/FishNet/`: FishNet framework source/assets
- `Assets/Plugins/FishyFacepunch/`: FishyFacepunch transport plugin
- `Assets/Plugins/Facepunch.Steamworks/`: Steamworks wrapper binaries
- `Packages/`: Unity package manifest
- `ProjectSettings/`: Unity project settings

## What Is Versioned

This repository should include:

- `Assets/`
- `Packages/manifest.json`
- `ProjectSettings/`
- `.meta` files
- source files and required third-party runtime/plugin files

This repository should not include:

- `Library/`, `Temp/`, `Logs/`, `UserSettings/`
- generated solution/project files (`*.sln`, `*.csproj`)
- local caches and machine-specific config

## Third-Party Libraries Policy

### Should FishNet be committed?

Yes, for this project structure.

Reason:

- FishNet currently exists under `Assets/FishNet` as project files.
- Teammates need the exact same package content to open/build immediately.

### Should FishyFacepunch be committed?

Yes.

Reason:

- It is under `Assets/Plugins/FishyFacepunch` and used directly by transport code.

### Should Facepunch.Steamworks be committed?

Yes, but keep only required runtime/plugin files.

Reason:

- Your Steam integration code depends on it directly.
- Not committing it causes missing references for teammates.

Recommendation:

- Keep required `.dll`/native runtime files and `.meta` files.
- Ignore optional docs/symbols (`*.pdb`, `*.xml`) unless your team needs them.
- Verify license terms before publishing publicly.

## Team Workflow

- Main branch: `main`
- Feature branches: `feature/<short-topic>`
- Fix branches: `fix/<short-topic>`
- Use Pull Requests for every merge to `main`
- Require at least one reviewer before merge

Suggested commit style:

- `feat: add lobby join timeout handling`
- `fix: prevent duplicate host startup`
- `chore: update fishyfacepunch plugin`
- `docs: add setup instructions`

## Getting Started

1. Install Unity Hub and Unity Editor `2022.3.55f1c1`.
2. Clone this repository.
3. Open project folder with Unity Hub.
4. Let Unity import assets and compile.
5. Confirm Steam integration settings and test scene.

## First-Time Git Setup

Run from project root after adding these files:

```bash
git init
git branch -M main
git add .
git commit -m "chore: initialize unity repo"
git remote add origin <your-github-repo-url>
git push -u origin main
```

If your GitHub repo already has a README commit:

```bash
git remote add origin <your-github-repo-url>
git fetch origin
git pull origin main --allow-unrelated-histories
# resolve conflicts if any
git push -u origin main
```

## Collaboration Checklist

- Pull latest `main` before starting work
- Create a dedicated branch per task
- Keep commits small and focused
- Open PR with testing notes
- Rebase/merge latest `main` before final merge

## Recommended Next Improvements

- Enable Git LFS for large binary assets
- Add CI for Unity test/build checks
- Add CODEOWNERS for critical networking folders

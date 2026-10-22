# Persistence and lifecycle verification — 1.0.0.2

Verified locally on 2026-10-23. This is an independently developed plugin; classification is unchanged.

## Behavior

- One monotonic 60-second scheduler saves changed settings and character snapshots. UI edits apply immediately. No per-frame serialization, immediate text-input saves, or repeated full saves after incomplete capture.
- Logout, normal game shutdown and Dispose flush pending changes; a stopped or unrecoverable store remains stopped. Dispose stops new work, detaches events, cancels/drains workers, applies queued results and flushes before releasing resources.
- A validated temporary file atomically replaces the primary. Three previous valid generations use `.bak-<20-digit sequence>`. Invalid main files recover from the newest valid backup; `.corrupt-<id>` preserves the damaged primary. No valid candidate means writes are blocked rather than overwriting data with defaults. Future configuration versions are rejected.
- Save failures stop after three attempts with one notification; Settings provides explicit retry. Incomplete character/job snapshots are rejected before mutation; previous known positive job levels cannot disappear or regress. Three capture failures stop acquisition until Refresh Current Character. Automatic playtime execution attempts are rate-limited and stop after three local failures.
- Per-Content-ID single-flight lookups and cancellable batch work avoid overlapping searches. Image replacement uses a bounded download and temporary file, reuses unchanged cached images, and retains the old image if acquisition fails.
- Existing configuration schema version 4 and location are retained; legacy Newtonsoft object metadata is accepted without resolving CLR types. User language values and unrelated settings are preserved.
- The character collection stores current snapshots, not growing transaction history. Daily partitions and the shared History files tab are therefore inapplicable; no empty tab was added. Shared templates and specifications were not changed by this patch.

## Verification

Commands from the plugin root:

```powershell
dotnet restore CharacterArchive.slnx --locked-mode
dotnet build CharacterArchive.slnx -c Release --no-restore -t:Rebuild
dotnet run --project CharacterArchive.Tests/CharacterArchive.Tests.csproj -c Release --no-build
./scripts/pack-release.ps1 -NoBuild
```

Release rebuild: **0 warnings, 0 errors**. Existing core tests and **43 persistence/lifecycle checks** passed. Tests use small synthetic data in temporary directories, not production configurations.

| Scenario | Measured result |
| --- | --- |
| Eight unchanged 60-second periods | 0 committed saves, 0 logical bytes |
| 600 input changes within one period | 0 immediate saves; 1 checkpoint save, 238 logical bytes |
| Changed then reverted before checkpoint | 0 additional saves |
| Write fault | 3 failed attempts, 1 notification; manual retry succeeds; 236 logical bytes including retry |
| Validation / replacement fault (each) | 3 failed attempts, 1 notification; manual retry succeeds; 944 logical bytes including failed temporary payloads and retry |
| Twenty concurrent flush requests | One commit for the changed snapshot |
| After Dispose | No additional writes or accepted workers |

Also covered: actual Windows exclusive file lock and recovery after release; three valid backup contents; broken main plus newest backup fallback; healthy primary precedence; malformed version type fallback; no valid backup blocking; damaged temporary-file rejection; changes arriving during save; legacy metadata; incomplete capture and explicit resume; midnight and wall-clock rollback; 100 same-key requests sharing one task; cancellation/drain; and a result queued just before shutdown being applied before the final flush.

Counts are successful payload installations and logical temporary payload bytes, **not physical SSD writes** or measured live-game throughput. Fixture sizes are intentionally small.

## Package and privacy

The versioned ZIP contains only `CharacterArchive.dll`, `CharacterArchive.json`, `LICENSE` and `THIRD_PARTY_NOTICES.md`. Runtime dependencies, debug files, tests, real character data and local configurations are excluded. Backups and corrupt-file evidence stay beside the user's configuration and contain the same private data; deletion instructions are in both READMEs.

## Remaining manual checks

- Actual game login, command readiness, all-job completeness and playtime response handling.
- Actual logout, game shutdown, plugin reload/unload during Lodestone traffic, and saved-language persistence.
- Seven-language glyph coverage, long text layouts and error/retry controls in the game UI.
- Live Lodestone HTML/image compatibility and behavior during maintenance.

These live-game checks and physical SSD measurements have not been performed. Forced termination can lose the unsaved interval (normally up to about 60 seconds); backups cannot recreate it. No installed DLL or live configuration was replaced during local verification.

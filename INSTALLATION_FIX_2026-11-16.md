# Installation/startup fix — 1.1.0.1

## Cause

The user's Dalamud log shows successful extraction/staging of 1.1.0.0 followed by a constructor failure at the mandatory playtime hook. Reloaded's inline reverse-wrapper allocation failed with BufferAllocationException / VirtualAlloc2 error 8. A later missing-DLL error followed installation rollback and is not the original failure. No personal log data is copied into this repository.

## Change

- Use the supported IGameInteropProvider.HookFromFunctionPointerVariable API on the UIModule HandlePacket virtual-table slot. This avoids the failing nearby inline reverse-wrapper allocation route and does not force deprecated hook backend selection.
- Treat playtime observation as an optional resource. Creation/activation failure is logged once, a partially initialized hook is disposed, and the rest of startup continues. A localized warning explains plugin-reload retry.
- Do not issue playtime commands when observation is unavailable. Retain existing stored data and leave all other acquisition, command routes and packet behavior unchanged.
- Capture the original delegate before enabling the hook so an already-entered callback can forward safely during disposal. Failed cleanup/logging cannot abort startup or remaining resource cleanup.

## Verification

Nine optional-resource checks cover allocation failure, bounded single attempt/reporting, activation failure cleanup, failed cleanup/logging isolation, original-resource availability before activation, successful lifetime and idempotent disposal. Existing 43 persistence/lifecycle and 57 review checks, seven-language resources/UI key coverage, and core tests passed. Release build: zero warnings/errors.

Actual native hook activation and successful installation in the running game remain unverified. Tests do not call game functions, send commands or overwrite installed DLLs/configuration. The fix deliberately allows startup even if native playtime initialization remains unavailable on this machine.

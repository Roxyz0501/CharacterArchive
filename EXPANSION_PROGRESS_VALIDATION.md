# Expansion percentages — 1.2.0.0

## Definition

Expansion comes from the recorded quest's ExVersion row. Expansion total covers that expansion's required main-scenario prerequisite graph; .0 covers its first main-scenario journal genre. The installed data has a unique endpoint for each of these 12 scopes. Do not use SortKey as a global chronological order: it resets between chapters.

An AND join includes all required parallel branches. An OR join selects an already completed prerequisite, or the uniquely evidenced in-progress route. Mutually exclusive starting-city/class routes are not all placed in the denominator. Unknown/tied evidence or ambiguous endpoints produces an unavailable percentage. A completed endpoint independently proves 100%, including after game-data rewrites or story-skip changes; no quest-count pair is fabricated when relying on this milestone proof.

Partial percentages are completed-required-count / route-required-count, truncated to one decimal so unfinished progress cannot round to 100. These are quest counts, not playtime estimates or quest sequence values. Future installed patches can increase the overall denominator. Older saved records without percentage fields remain valid and are populated on login/refresh. Schema 4 and CSV columns are unchanged; new ratios are validated before config recovery/saves.

## Cache / cost

The existing 10-second reader compares the received 751-byte completion mask with its cached copy. This mask is only a change detector; completion meanings still use the runtime IsQuestComplete API. The calculator cache is keyed by Content ID, guide quest ID and exact mask. Metadata/scopes are retained for the installed catalogue. Only changed observations recalculate; full and .0 share a per-calculation memo so completion API reads never exceed the current expansion's catalogue size. No HTTP, game command, server request or immediate settings save is introduced.

## Executed verification

- 18 new managed checks: expansion selection; OR route exclusion; AND branch inclusion; launch/patch boundaries; 0/partial/100%; milestone/rewritten flags; unresolved data; rounding; per-character cache isolation; same-mask cache reuse; changes without guide changes; config roundtrip/legacy migration; invalid ratios; unchanged dirty marks.
- Synthetic initial calculation: eight completion reads, full 3/7 = 42.8%, .0 3/5 = 60%; unchanged repeat: zero completion reads. Unchanged persisted observation: zero dirty marks. Existing 600-edit probe remains one save / 238 logical bytes; eight idle periods zero writes. Logical bytes are not SSD physical writes.
- Offline installed data, Japanese/English/German/French: each has 1,050 MSQs, six expansions, six launch boundaries and six full boundaries. All four languages produce matching scope counts and endpoint identities. 108 offline checks passed (12 existing + 96 expansion checks).
- Fully completed synthetic flags: both scopes reach 100% for every installed expansion. Synthetic incomplete flags: starting-route-normalized overall/.0 totals are ARR 241/161, Heavensward 138/94, Stormblood 162/122, Shadowbringers 157/106, Endwalker 155/108, Dawntrail 143/100. ARR's alternate starting routes can have different lengths; these are fixture-selected totals, not a universal denominator for every player.
- Per-expansion completion reads in those fixtures: 246, 138, 162, 157, 155, 143 respectively. Unchanged repeats: zero, with no game-data catalogue reload. All counts are local reads, not network requests.
- Existing nine optional-resource, 43 persistence and 57 review checks, core/UI key and seven-language resource checks retained.

No player records, installed DLLs or live game state were modified during verification. Native guide/flag readiness and actual in-game layouts remain unverified. The implementation preserves the previous saved observation on unavailable capture and retains the optional playtime-hook initialization fix.

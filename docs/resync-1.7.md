# RimWorld Multiplayer Resync — 1.7 development

This fork targets **RimWorld 1.7**. The `dev/1.7-runtime` branch starts at
`RimWorld-mods-patches/Multiplayer` PR #1000 commit
`cf163a2e5464777038107ce8ef5550720230ccf2`. It is experimental and
not yet validated with large modpacks.

## First diagnostic increment

`feature/diagnostics-01` adds indexed comparison of existing synchronized
RNG sample sequences and a `sync_sequences.txt` entry in saved desync ZIPs.
The report records window start ticks, map ordering, the first mismatching
sample index, sample values, and sample counts for local and remote peers.

The feature **does not**:
- add object-state checksums or identify the source mod automatically;
- claim that a sample index is an exact game tick;
- modify the networking protocol, command ordering, or recovery behavior;
- prevent or repair desyncs.

Its purpose is to make existing synchronization failures reproducible and
easier to investigate without adding stack walking to the normal tick path.

## Development sequence

1. Run automated unit tests on the .NET 10 / RimWorld 1.7 reference build.
2. Exercise a host and one client in the same unmodded 1.7 save.
3. Introduce controlled mismatches to verify the report without corrupting saves.
4. Review PR #996 fixes by subsystem before porting them; its changes overlap
   the 1.7 runtime work in PR #1000.
5. Add opt-in, budgeted deterministic fingerprints for game state, followed
   by guarded resync and automated mod-compatibility tests.

No merge to `master` or public release should occur before in-game tests.

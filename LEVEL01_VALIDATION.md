# Level 01 validation — 29 October 2026

Unity 6000.3.6f1 / URP. Windows Development players built using the already-running editor and the inspected, allowlisted file bridge. No second editor was started for this work. No new dependencies, ads, payments, Git commits or pushes.

## Passed

- C# compilation and level/practice Windows builds succeeded. The 16 existing shared combat-rule checks passed.
- ShoppingDistrict full integration: **35 checks passed at each of 1280×720, 1600×1000 and 1920×810**. Reports: `artifacts/level-verified-{1280,1600,wide}/level-runtime-report.json`.
- Tests use actual production scheduling and player screen rays for target hits, physical cover, blade interception, empty-rickshaw destruction, emergency ammo and the boss. They cover all three distinct street sections, both automatic camera moves, locked combat during travel/breach, natural civilian/Bhabi exits, actor caps, occupied sockets, restrained attack scheduling, runner escape without deadlock, seeded spawn replay, AI civilian protection, unchanged precision dimensions, timed breach, skip-preview action, balcony support, results, defeat and same-seed restarts.
- Seven rendered screenshots per aspect ratio: three outdoor sections, mock-banner breach, indoor atrium, results and defeat. Outdoor/indoor and breach images were inspected; target nameplates, atrium signage and pickup label orientation were corrected for readability.
- Practice harness regression report: `artifacts/practice-verified/runtime-report.json` (six existing checks). Previous successful run also preserved in `artifacts/practice-final/`.
- Python CLI syntax, tool detection and existing-editor guard verified with the installed Python 3.11.15. `build-level` uses the existing bridge when Unity is open. `smoke-level` runs the opt-in standalone verifier.

## Fixed during validation

The partial level reused one street viewpoint, allowed failed socket searches to consume spawns, and could suppress emergency ammo when unrelated loot existed. The first standalone build also stripped the dynamically referenced URP shader. The level now uses three spatially distinct sections, free-socket retries, an independently reachable emergency supply and a serialized material reference. Pedestrians and old encounter loot are cleared between sections; dead/escaped runners resolve the wave. Editor commands wait during Play rather than silently stopping it, report busy state and refuse builds over unsaved scene changes.

## Limits

Greybox visuals, debug-style HUD, prototype tuning and boss identity remain intentional. The progression sweep replenishes player health/reserve while exercising real shots and reloads, so its success does not establish difficulty balance. Separate checks cover damage/defeat and total ammo depletion. Desktop landscape screen-ray targeting was exercised; physical touch devices, Android performance, audio/voice recordings and production assets remain unverified. The street is a three-viewpoint shooting level, not a free-roaming character controller. Procedural scenery is constructed when Play starts.

Earlier unsuccessful/interrupted run logs are retained in other ignored artifact folders; use the `level-verified-*` reports for the final build. The original staged Git index, including starter files/deletions, was preserved.

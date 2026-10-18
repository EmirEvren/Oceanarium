# ASTRA 0.8 work in progress

Latest user scope: finish 0.8 completely, then implement the attached 0.9 specification. Do not mark 0.8 complete based on the initial playable slice.

Implemented: 12 resident definitions and modular appearances; shared shop/dialogue/gift UI; versioned town save embedded in FishCollection; atomic purchases and exact-instance fish sales; friendship daily limits; one-time rewards; existing aquarium decoration purchases; rod/line/hook upgrades; 24 quest definitions and accept/claim transactions; journal quests/residents; finite coastal pickups; temporary clock and grid navigation; authored greetings; tailor appearance integration; tea bite-wait buff; camera capture and arcade first implementation; procedural merchant counters and profession props.

Verified: 0.7 four-trip preservation test plus physical tank restoration and fresh Play Mode restart passed. 0.8 isolated transaction tests passed. Town runtime test passed navigation connectivity for 11 daytime residents, dialogue input locking, real earned-money line purchase and effect, persistence, both Oliver quests, exact and non-repeating quest rewards. These tests do not establish full 0.8 completion.

Additional verified: tailored hat purchased with earned money, persisted in AppearanceStore and applied to avatar; owned tea consumed and starts 300-second buff; pocket camera purchase and PNG file written under persistent Photos with harbor subject flag; NPC walked along 19-node route at configured break time. Quest transactions test validates delivery consumption and prevents early/repeated claims. Added camera quest, bulletin text, profession counter props and Unity-generated portrait badges. Arcade implementation still needs input/reward QA.

Remaining validation/content: all 24 quests end-to-end; richer friendships and rewards (configured differentiated resources, several still need usable item actions); complete product lists; night merchant sign/counter visibility; food assistance/luck buffs; full environment art/interiors/bridges/viewpoints; portrait and journal visual QA; localization coverage; restart and boat regression after Town changes; measured performance. 0.9 specification read fully, implementation not started.

Important: do not run travel-build after town-build without rerunning town-build, because the 0.7 builder regenerates Town. Use stop, import, town-build, play, town-runtime-checks in order. Command inbox Temp/OF_ArtCommand.txt consumes one command at a time. Latest new scripts need Unity refresh if auto-refresh is idle.

Ownership: town clock/schedules and stock are candidate shared host state; local inventory/wallet/friendship/quests are currently one player's FishCollection. TownTransactions validates owner/revision/replay and atomic saving; TownWorld currently validates physical proximity. No networking is implemented.

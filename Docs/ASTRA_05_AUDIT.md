# ASTRA 0.5 integration audit

Baseline is the committed 0.4 project. Existing FishCollection v2 in fish-collection-v1.json owns exact specimens, GUID owner identity, Stored/Displayed transitions, catch history, journal records and difficulty preferences. FishingController owns the live local collection. FishCollectionStore already stages atomic file replacement and blocks unreadable/future saves. AppearanceStore is independent.

Reuse PlayerInteractor/IInteractable for E interactions, FoundationHUD modal input gating, CollectionHUD and CozyUI, OF_UI localization tables and CharacterAppearance for tourists. Eight existing FishSpecies remain canonical. Current aquarium plot is Future aquarium garden at x=8,z=3. Existing IslandGeometry.Height supplies terrain surface. Home scene and existing player prefab will be extended in place.

Plan: schema v3 adds world aquarium data to the same atomic save envelope, avoiding cross-file transactions. Fish placement remains on owned FishInstance IDs; tank assignments are not duplicated into a competing inventory. A local authority interface validates owner, habitat, capacity, transactions and upgrade spending. Multiplayer transport remains future work. Existing v1/v2 migration and corrupt-save protection remain intact.

# Gameplay Scene Audit

Audit target: `Assets/Scenes/Gameplay.unity`

## Fixed in this pass

- Restored the `NoteViewerUI` meta GUID expected by the open scene. This removes
  the repeated `Internal error - unexpected guid mismatch` messages.
- Changed the microwave model's negative BoxCollider Y size from `-0.035` to
  `0.035`.
- Made `MicrowaveController` compatible with an URP/Lit-converted material. It
  checks for `_LightColor` before accessing it and creates a warm point-light
  fallback when the old shader property is unavailable.
- Added runtime recovery for the disabled `NoteCanvas`.
- Added runtime recovery for the existing five-slot Hotbar when the scene has no
  `InventoryHotbarUI` component saved on it.
- Verified C# compilation with 0 errors and 0 warnings.
- Verified zero unresolved custom-script GUIDs and zero missing-script records in
  the Gameplay scene.

## Manual configuration still required

Complete these in this order before evaluating visuals and balance.

### 1. Choose the authoritative gameplay clock

The scene contains two active `ClockController` instances: `ClockMicro` and
`ClockMicro (1)`. `GameManager.Game Clock` is currently empty and therefore uses
the first clock Unity happens to find.

1. Decide which clock controls the monster's six-hour feeding checks.
2. Drag that clock's `ClockController` into `GameManager > Game Clock`.
3. On the scene's microwave instance, assign the same clock to
   `Burger Microwave Station > Game Clock`.
4. Keep both clocks at `Real Seconds Per Game Hour = 5`, unless the second clock
   will later use a different puzzle-specific controller.

### 2. Assign inventory icons

All five ItemDefinition assets currently have `Icon = None`, so the Hotbar and
microwave panel can show quantities/text but not item artwork.

Assign a Sprite to `Icon` on:

- `Assets/Items/Bread.asset`
- `Assets/Items/Patty.asset`
- `Assets/Items/Lettuce.asset`
- `Assets/Items/Tomato.asset`
- `Assets/Items/Burger.asset`

If a texture cannot be selected, set its Texture Type to `Sprite (2D and UI)` in
the texture Import Settings first.

### 3. Tune held-item transforms

All ItemDefinition held positions and rotations are still zero and their scales
are one. Enter Play Mode, select each item, and tune its `Held Local Position`,
`Held Local Euler Angles`, and `Held Local Scale`. The player's
`HeldItemAnchor` is already assigned.

### 4. Reduce and test the initial ingredient count

`GameManager` currently requests 75 Bread, 75 Patty, 75 Lettuce, and 75 Tomato
instances: 300 physics/world objects total. This can hurt performance and makes
the intended limited-resource puzzle ineffective.

For a first playable balance pass, start with roughly 3–6 of each ingredient,
then adjust `Burger Part Spawn Center`, `Size`, surface layers, and the cyan gizmo
so parts do not appear outside the room or on top of props.

### 5. Tune DiningArea output

`DiningAreaBurgerReceiver` is configured and functional. Inspect its orange
gizmo and adjust `Local Spawn Center`/`Size` so normal burgers land on the intended
surface. `Burger Mountain Prefab` is empty; the code currently creates a working
12-burger fallback pile. Assign final art here when it exists.

### 6. Place and configure the three notes

The UI and three Sprite assets exist, but the Gameplay scene contains zero
`ReadableNote` components. For each note, create/place a world object with:

1. A Collider.
2. Tag `Note`.
3. Component `ReadableNote`.
4. The matching `Content Image`:
   - `Assets/Art/Notes/note_escape.png`
   - `Assets/Art/Notes/note_clock_anchor.png`
   - `Assets/Art/Notes/note_feeding_loop.png`

Place the escape note in the opening area. Place the other two only after their
safe interiors exist.

### 7. Run one focused Play Mode test

After Unity finishes importing, clear Console and test in this order:

1. Player movement and camera.
2. E pickup at the crosshair and nearest-item fallback.
3. Mouse-wheel Hotbar selection and held model.
4. Microwave E interaction, four material slots, five-second work effect.
5. Confirm the launched burger cannot be collected.
6. Confirm a separate DiningArea burger can be collected.
7. Trigger `GameManager > Debug > Run Monster Check Now` once with and once
   without a DiningArea burger.
8. Set microwave `Starting Mode = Infinite` for one run and verify the two-second
   output interval and infinite Burger Mountain pickup.
9. Aim at the placed escape note and press X to close it.

If the two old GUID messages remain visible after import, clear Console first.
If they return with a new timestamp, restart Unity once so its AssetDatabase drops
the stale in-memory mapping. Do not delete `Library` unless they still return
after that restart.

## Gameplay systems not implemented yet

These are missing code/gameplay, not missing Inspector references:

1. A reusable burger-payment safe system with `0/2` and `0/10` counters, opening
   animation, and reward spawning.
2. Placement of two safe/cabinet objects in Gameplay. The imported Old Cabinet
   prefab is not currently instantiated in the scene.
3. The production-clock anchor interaction that detects placing a burger at the
   microwave-completion moment and calls
   `BurgerMicrowaveStation.EnableInfiniteProductionMode()`.
4. A collectible monster-feeding clock hand item and its inventory definition.
5. The clock-hand installation interaction on the second clock.
6. The six-o'clock burger anchor on the monster-feeding clock.
7. Repeating monster-eating sequence, pain/death timing, and related audio or
   animation.
8. Monster representation in Gameplay. Monster assets are imported, but no URP
   monster prefab is currently instantiated in the scene.
9. Door unlock/open behavior after the monster dies.
10. Victory UI/game-complete state.
11. Feeding success/failure sound and monster/door impact presentation. The
    current GameManager result is visual text/fade only.

## Already present and connected

- First-person controller, camera, crosshair, and EventSystem.
- Five-type stackable inventory, E pickup, mouse-wheel selection, held-item
  anchor, and five hand-built Hotbar slots.
- Two visible clock instances with five seconds per hour.
- Six-hour/30-second feeding checks, DiningArea burger detection, success resume,
  failure Enter-to-reload flow, and debug switches.
- Random ingredient spawning.
- Microwave recipe panel, normal production, URP-compatible working effect,
  non-collectible launched burger, and collectible DiningArea reward.
- Infinite microwave production and infinite-supply Burger Mountain fallback.
- Note viewing UI and three generated note images.
- Gameplay is the enabled Build Settings scene.

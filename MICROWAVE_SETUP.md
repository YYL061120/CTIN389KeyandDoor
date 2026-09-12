# Microwave Burger Crafting

The current `Gameplay` scene is already wired through the two prefabs used by the scene:

- `FirstPersonController.prefab` contains `PlayerMicrowaveInteractor`.
- `Microwave Model.prefab` contains `BurgerMicrowaveStation`, the asset's
  `MicrowaveController`, and a `Burger Output Point` child.
- The configured recipe is Bread + Patty + Lettuce + Tomato.
- Cooking duration reads `ClockController.RealSecondsPerGameHour` (currently 5 seconds).

## Play test

1. Collect one Bread, Patty, Lettuce, and Tomato.
2. Stand within `Interaction Distance` of the microwave and press **E**.
3. Click each of the four ingredient slots. Green/`READY` means the item has moved
   from the inventory into that slot.
4. Click **ENTER** (or press Return) to cook.
5. The microwave controller runs its plate, fan, internal light, display, and audio.
   On completion, a visual-only burger is launched upward and destroyed after two
   seconds. A separate collectible burger is spawned randomly in `DiningArea`.

Press **X** in the panel or Escape before starting to close it. All deposited
ingredients are returned to the inventory.

## Inspector tuning

Select `Assets/00Laboratories/Microwave/Prefabs/Microwave Model.prefab` and edit
`Burger Microwave Station`:

- `Interaction Distance`: how close the player must be.
- `Required Ingredients`: recipe and number of UI slots.
- `Burger Output Prefab`: produced object.
- `Burger Output Point`: launch position.
- `Upward Impulse` / `Forward Impulse`: launch direction and strength.
- `Output Lifetime`: seconds before the produced burger disappears.

## Infinite production mode

`BurgerMicrowaveStation` exposes the UnityEvent-friendly methods
`EnableInfiniteProductionMode()` and `DisableInfiniteProductionMode()`. The
`Starting Mode` Inspector field can also start a Play Mode test directly in
`Infinite` mode. The component context menu contains matching debug commands.

While infinite mode is active:

- The microwave stays in its supplied working animation/audio state.
- It launches one visual-only burger every `Infinite Output Interval` (2 seconds).
- Normal DiningArea burgers are removed and replaced by an infinite-supply burger
  mountain. Pressing E on the mountain adds one burger without destroying it.

`DiningAreaBurgerReceiver` is attached to the scene's `DiningArea`. Its orange
gizmo controls the random normal-burger region. Assign custom art to `Burger
Mountain Prefab` when available; while this field is empty, the receiver builds a
functional temporary pile from twelve copies of the current burger prefab.

The crafting UI is auto-created because `PlayerMicrowaveInteractor.Auto Create UI`
is enabled. To replace it with hand-authored Unity UI, add one
`MicrowaveCraftingPanelUI` to the scene, assign its optional UI fields, and assign
that component to the player's `Crafting UI` field (or leave it as the only such
component so it is found automatically).

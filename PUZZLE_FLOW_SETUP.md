# Puzzle Flow Inspector Setup

The runtime code is complete, but scene-specific positions and art references must be assigned in `Gameplay`.

## 1. Two burger safes

1. Drag `Assets/Vintage_Safe/Prefabs/Safe_Dirty.prefab` into `Gameplay` twice.
2. Place them where the player can reach them and add `BurgerSafe` to each root.
3. Assign `Assets/Items/Burger.asset` to **Burger Item**.
4. Safe 1: set **Required Burgers** to `2`. Add the first-safe note to **Reward Objects**.
5. Safe 2: set **Required Burgers** to `10`. Add the clock-hand pickup to **Reward Objects**.
6. Assign the model's `Safe_Door` child to **Fallback Door**. Start with Euler `(0, -105, 0)` and adjust the Y sign if it opens inward.
7. If you make an Animator instead, give it an `Open` trigger and assign it; the fallback rotation will then be ignored.

The shared bottom prompt displays `0/2` or `0/10` whenever E is pressed. One selected burger is removed per press.

## 2. Clock-hand pickup in Safe 2

1. Make a small scene object using the clock's hour-hand mesh or another visible hand model.
2. Add a collider and `WorldItem`.
3. Assign `Assets/Items/ClockHand.asset` to **Item**, quantity `1`, collectible enabled.
4. Put this object inside Safe 2 and assign it to that safe's **Reward Objects** array.

## 3. Production clock

1. Add `BurgerClockPuzzle` to the production clock root.
2. Set **Purpose** to `Production`.
3. Assign its `ClockController`, the scene microwave's `BurgerMicrowaveStation`, `Burger.asset`, and `Burger Detailed Variant.prefab`.
4. Create an empty child named `InteractionCameraPose`; move/rotate it until the clock fills the desired camera view, then assign it.
5. Create twelve empty children around the clock face. Assign them to **Burger Slot Anchors** in this exact order: `12, 1, 2, ... 11`.
6. Rotate/scale each anchor so a burger attached as its child looks wedged against the clock face.
7. Leave **Requires Hour Hand** off.

While the microwave is cooking, the panel tells you its completion hour. Put the selected burger into that slot before the hand arrives. When the hand reaches it, the clock stays stuck and the microwave enters infinite mode.

## 4. Monster-feeding clock

1. Add `BurgerClockPuzzle` to the second clock root and set **Purpose** to `Monster Feeding`.
2. Assign `ClockController`, `GameManager`, `Burger.asset`, and the twelve anchors as above.
3. Enable **Requires Hour Hand** and **Stop Clock Until Hand Installed**.
4. Disable **Hour Hand Installed**, assign `ClockHand.asset`, and assign the model's visible hour-hand child to **Hour Hand Visual**.
5. Set **Victory Dial Hour** to `6` and choose a **Victory Delay** (default `5` seconds).
6. Assign a separate `InteractionCameraPose` for this clock.

Select the clock hand in the hotbar and press E to install it. After installation, E opens the twelve-slot panel. A burger reaching the 6 o'clock slot starts the final victory sequence.

## 5. Full-rotation monster check

`ClockController > Monster Check Interval Hours` is now `12`. In `GameManager`, assign the clock that should control regular feeding checks to **Game Clock**. If this clock should continue before the removable hand is installed, turn off **Stop Clock Until Hand Installed** on that clock's puzzle component.

## 6. Monster outside the door

1. Drag `Assets/Brutal Warrior Monster/prefab/upr/base_mesh_set_1.prefab` into `Gameplay`.
2. Place it outside the closed door and rotate it to face into the room.
3. Keep its existing `Monster Animator Controller 1` Animator Controller.
4. Add `MonsterIdleOnly` to the root. Its default state name is `anim_idle_1` and root motion is disabled automatically.
5. Remove or disable any unnecessary colliders if they block the door or player raycasts.

## 7. Audio

1. Add `MonsterAmbientAudio` to the monster root and assign `Assets/Sound/MonsterRoaring.wav` to **Roar Loop**. Start with volume `0.65`, minimum distance `2`, maximum distance `24`.
2. On each audible `ClockController`, assign the quiet mechanical loop to **Ambient Ticking Loop** and `Assets/Sound/One.wav` to **New Hour Tick**.
3. Start with ambient volume `0.08`, new-hour volume `0.35`, spatial blend `0.25`. Assign these clips to only the main audible clock first, so two synchronized clocks do not double the same tick.
4. `Assets/Sound/Oneminute.m4a` is currently imported as a generic file, not an AudioClip. Convert it to WAV or OGG before assigning it as the ambient loop.

## 8. Interaction and UI behavior

`PlayerItemCollector` adds `PlayerInteractionController` automatically at runtime. Approaching a safe, either clock, or the microwave shows a flashing **Press E to interact** prompt. Clock interaction freezes both clocks, disables player/camera movement, moves the camera to the assigned pose, and releases the mouse. Left-click an empty hour slot to place a burger; right-click an occupied slot to take it back. Removing the burger that stopped the hand lets that clock continue from the same time. Press X or Escape to close.

The final victory uses the existing GameManager black overlay, freezes the player and clocks, and displays the GameManager **Victory Message**.

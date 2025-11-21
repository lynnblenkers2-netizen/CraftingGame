# Tooltip Prefab Setup

Quick checklist to create and hook up an item hover tooltip prefab using the provided scripts.

1) Create Tooltip Prefab
   - Create a UI `GameObject` under a Canvas (or create temporarily and later make it a prefab): name it `ItemTooltip`.
   - Add an `Image` component on the root to serve as background (optional: add 9-sliced sprite).
   - Add a child `Image` named `Icon` for the item icon.
   - Add a child `Text - TextMeshPro` named `Title` for the item title.
   - Add a child `Text - TextMeshPro` named `Description` for the item description.

2) Add `TooltipAutoSize` on the root GameObject
   - Drag the child `Icon` into the `Icon` field on the `TooltipAutoSize` component.
   - Drag the child `Title` into the `Title` field.
   - Drag the child `Description` into the `Description` field.
   - Optionally set `Max Width`, `Padding` and `Background Rect` (default is root).

3) Make the prefab
   - Drag the configured `ItemTooltip` GameObject into a `Prefabs` folder to create a prefab asset.

4) Add manager to scene
   - Create an empty GameObject (e.g. `UIManagers`) and add the `ItemTooltipUI` component.
   - Drag the `ItemTooltip` prefab into the `Tooltip Prefab` slot on `ItemTooltipUI`.
   - Optionally drag your main `Canvas` into `Root Canvas` on `ItemTooltipUI` (if left empty, first Canvas will be used).

5) Add handler to slot prefab
   - Open the `Slot` prefab (the one used by `ItemSlotUI`) and add the `SlotHoverTooltip` component.
   - Make sure the slot has an `Image` (or other `Graphic`) so pointer events work.

6) Test
   - Enter Play mode and hover over a populated slot. The tooltip should appear and size to the content.

Notes
   - The scripts use reflection to read your `ItemStack`/`Item` properties (`Item`, `Icon`, `Name`, `Description`) if available. If you prefer a typed approach, the scripts can be adjusted to reference your concrete types.
   - If the tooltip appears off-screen, adjust the `ItemTooltipUI` offsets or pivot on the prefab.

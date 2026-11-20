# Dead End

[![Support on Ko-fi](https://raw.githubusercontent.com/KingdomIncModding/.github/main/assets/kofi-button.png)](https://ko-fi.com/urakkaamyx/tip)

**Dead End** makes unconnected building input and output ports immediately visible in dense Kingdom Inc. factory layouts by highlighting them in crisp pixel-art **orange** (or any color you choose), while leaving every existing port color, glow effect, and game mechanic completely unchanged.

## Features

- **Clear Unconnected Port Highlight**: Every building input and output port that has no pipe connected is framed with a pixel-accurate **orange** outline (and optional open-socket marker) that follows the exact shape of the port.
- **Preserves All Native Visuals**: Existing green input, blue output, glowing active, glowing red invalid-item, hover states, tooltips, and pipe visuals remain completely untouched.
- **Works With Mirrored & Rotated Ports**: Highlights automatically follow mirrored buildings (`Alt + Click`) and custom port layouts.
- **Instant Updates**: Highlights appear as soon as a port is created or disconnected and clear immediately when a pipe is connected.
- **Independent Input & Output Configuration (`Options -> Mods -> Dead End`)**:
  - Independently enable or disable highlights for **Input Ports**, **Output Ports**, and **Junction Ports** (**Collectors**, **Distributors**, and **Manifolds**).
  - Dedicated interactive **Color Picker** for **Input Ports** and **Output Ports** (each with one-click quick swatches, a 2D Saturation/Brightness + Hue color picker popup, and `#RRGGBB` hex code input; both default to **Orange `#FF8C1A`**).
  - Toggle highlights on or off anytime via the **Mod Bar** button or an optional custom hotkey.
  - Choose between **Outline**, **Outline + Marker**, or **Marker Only**, and adjust **Outline Thickness** (`Thin 1px` or `Bold 2px`), **Opacity**, and an optional **Gentle Pulse Effect**.

## Installation

1. Install **BepInEx 5** for Kingdom Inc.
2. Install **Foundation** (0.14.4 or later) into `KingdomInc/BepInEx/plugins/Foundation/`.
3. Extract **Dead End** into `KingdomInc/BepInEx/plugins/DeadEnd/` so `DeadEnd.dll` and `mod.json` sit inside that folder.
4. Launch Kingdom Inc. Highlights are active automatically and can be configured under **Options -> Mods -> Dead End**.

## Compatibility

| Class.Method | Hook Type | Impact |
|---|---|---|
| WorldFlowIO.SetIO | Postfix | Attaches the Dead End highlight overlay to input and output ports without modifying native port visuals or behavior. |
| WorldFlowElement.SetElement | Postfix | Ensures ports on newly bound buildings receive Dead End overlays. |
| WorldFlowElement.UpdateIO | Postfix | Reattaches and refreshes port overlays after a building rebuilds or mirrors its ports. |
| WorldFlowPipe.SetFlowPipe | Postfix | Updates input and output port highlight states immediately when a pipe is connected. |
| WorldFlowIO.OnDragDropEnd | Postfix | Refreshes port highlight state after finishing a pipe connection drag. |
| WorldFlowIO.OnDragDropCancel | Postfix | Refreshes port highlight state after cancelling a pipe drag. |
| WorldFlowManager.ShowFlow | Postfix | Refreshes port overlays when switching between workspaces. |

## Support

If you enjoy these Kingdom Inc mods and tools, consider supporting development on [**Ko-fi**](https://ko-fi.com/urakkaamyx/tip)!

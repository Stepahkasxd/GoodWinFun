# Debuff Improvements Summary

This document records the changes made across the debuff system to ensure stable operation and easier auditing.

## Key-toggle debuffs
- **FpsLockDebuff**, **HideHUDDebuff**, **HideHealthbarsDebuff**, **MinimapShiftDebuff**
- *Method*: Each debuff now queries the current game setting on apply, stores it, and restores the original value on remove so manual player changes are preserved.

## Input hooks and lag
- **InputHookHost** now tracks feature flags with reference counts, supports system-wide input blocking, and inverts the Y axis by resending mouse deltas instead of absolute coordinates.
- *Method*: Replaced boolean flags with counters, added BlockInput calls, and computed movement deltas before dispatching inverted input.

## Toxic chat
- **ToxicChatDebuff** types messages faster and can be cancelled.
- *Method*: Reduced per-key and message delays and introduced a cancellation token checked in the send loop.

## Audio debuffs
- **DisableKeyboardDebuff** and **PudgeHookSoundDebuff** play WAV files found in the `Sounds` folder (files are not included in the repository).
- *Method*: Added `Sounds` folder support, used `SoundPlayer` in `using` blocks, and invoked `BlockInput` to disable keyboard input in the game.

## Overlay and visual effects
- **NoirDebuff** overlays a full-screen grayscale filter and cleans up with a handle instead of clearing all overlays.
- *Method*: Sized `OverlayWindow` to the primary screen, returned overlay handles, and removed only the debuff's overlay on finish.

## Random sensitivity
- **RandomSensitivityDebuff** randomizes system cursor speed and restores the original value when finished.
- *Method*: Used `SystemParametersInfo` to read and set mouse speed at the OS level, storing the original speed for restoration.

## Auto skill
- **AutoSkillDebuff** safely parses key codes.
- *Method*: Switched to `Enum.TryParse` and skipped invalid entries instead of throwing.

## Removed PingDebuff
- *Method*: Deleted unusable Ping debuff and its references to avoid dead code.

These updates stabilize debuff interactions, ensure resources are cleaned up, and make each debuff reversible.

## UI roadmap
The current WPF interface is a minimal skeleton intended for testing. When the project grows, plan for a full-stack UI revamp with custom controls, animations and design polish.

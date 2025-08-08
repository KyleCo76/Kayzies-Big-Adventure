
# Changelog

## [1.0.2] - 2025-04-07
- Updated Reactional Engine.
- Set default lookahead set to 100,000.
- New OSC message "/audio/play".
- Fixed "/scale" & "/root" OSC events to be able to play when only theme is playing.
- Theme.IsPlaying property.
- GetThemeMetadata method added to API.
- GetThemeInfo method added to API.
- SubscribeToQuantizationEvent method added to API.
- SetControl 'value' parameter default value changed from 1.0f to 0.5f.
- Quant enum extended to include triplets and dotted notes.
- QuantToFloat & QuantToFloat methods added to API.
- Bugfix: Trigger stringer quant value was converted wrongly.
- Added editor widget tool with basic controls to be able to test theme controls and playback.
- Added controls (parts, macros, stingers, overridable instruments & performer routings) to new separated ThemeInfo struct (previously shared TrackInfo struct).
- Improved comments for some methods.
- Ensure theme part does not change by default when switching tracks.
- Added multiple example scripts.
- Fixed backwards compatability with engine version 2018 (.NET 4.x)
- Added an included free bundle.

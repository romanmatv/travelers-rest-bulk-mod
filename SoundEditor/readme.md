# SoundEditor

This mod will allow changing Audio volume for any audio in game

# Note "SINGLE" is a technical term for "has a few decimal places" so for this mod use 0.00, 0.01, 0.10, ..., 1.00, etc are all expected values

```yaml
## Settings file was created by plugin rbk-tr-SoundEditor v0.0.0
## Plugin GUID: rbk-tr-SoundEditor

[SoundTest]

## Flag to enable or disable this mod
# Setting type: Boolean
# Default value: true
isEnabled = true

### THIS ONE IS THE "GATE"
## Game sound 'MetalDoorCreak' volume level from 0.00 (0%) to 1.00 (100%).
# Setting type: Single
# Default value: 1
SoundLevel-MetalDoorCreak = .4
```
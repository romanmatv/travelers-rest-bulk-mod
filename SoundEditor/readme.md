# SoundEditor

With this mod you can adjust the sound of the AudioClips in the game from 0.00 (off) to 1.00 (max volume).

This mod will make a rather large cfg file as there are many sounds in the game.

NOTE: The "gate" noise is called "MetalDoorCreak" in the code, I suggest around 0.4 if you still want to hear it but it not being nails on a chalkboard.

# Note "SINGLE" is a technical term for "has a few decimal places" so for this mod use two decimal places like 0.00, 0.01, 0.10, ..., 0.98, 0.99 1.00, etc are all expected values

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
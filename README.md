# Sunless Skies Korean Patch

Sunless Skies Korean translation patch using BepInEx IL2CPP and XUnity AutoTranslator.

## Install

1. Close Sunless Skies.
2. Extract this package anywhere.
3. Right-click `install.ps1` and run with PowerShell, or run:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

If the script cannot find the game folder, pass it manually:

```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -GameDir "C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies"
```

## What It Installs

- BepInEx IL2CPP loader files
- XUnity AutoTranslator plugin files
- Korean AutoTranslator configuration
- Korean static translation files

The installer backs up replaced files to a `_backup_korean_patch_YYYYMMDD_HHMMSS` folder inside the game directory.

## Notes

- Launching the game for the first time after install may take longer while BepInEx prepares files.
- If some text remains English, it usually means that string has not been translated yet or the game combines several text fragments into a new runtime key.
- If Korean glyphs appear broken, check `BepInEx/config/AutoTranslatorConfig.ini` and confirm `FallbackFontTextMeshPro=arialuni_sdf_u2019`.

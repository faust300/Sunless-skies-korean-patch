# Static Asset Patcher Notes

This project is moving away from the BepInEx runtime translation path toward an install-time Unity asset patcher.

## Current patch targets

- `Sunless Skies_Data/resources.assets` (experimental, disabled by default)
  - Unity serialized strings are patched in place by scanning object data for `int32 length + UTF-8 bytes + 4-byte alignment`.
  - This can cover menu, pause, loading, and many Unity UI MonoBehaviour strings that were not present in `.bytes` data.
  - This is not release-safe yet. The broad scan can also alter Unity object, prefab, or asset names, which can break runtime lookups and cause loading/spawn failures.
- `Sunless Skies_Data/StreamingAssets/BuildInStorage/data/*.bytes`
  - Story and game database files are patched by the existing varint string patcher.
- `%USERPROFILE%/AppData/LocalLow/Failbetter Games/Sunless Skies/storage/data/*.bytes`
  - Local cached data is patched when present.

## Font status

The old BepInEx setup relied on XUnity AutoTranslator:

```ini
FallbackFontTextMeshPro=font
```

Without BepInEx, copying the root `font` bundle is not enough. The game does not automatically use it.

The root `font` bundle has been confirmed to contain a TextMeshPro SDF font:

- `NanumMyeongjoBold SDF`
- `NanumMyeongjoBold SDF Material`
- `NanumMyeongjoBold SDF Atlas`

The game `resources.assets` contains built-in TMP fonts such as:

- `EBGaramond-Regular SDF`
- `EBGaramond-Medium SDF`
- `LibreBaskerville-Regular SDF`
- `LibreBaskerville-Bold SDF`
- `NotoSymbols`

The next static patching step is to redirect TMP text components or font fallback references to a Korean-capable font asset while preserving valid Unity object references.

Do not use the first brute-force experiment that replaced the built-in TMP font, material, and atlas objects with duplicated `NanumMyeongjoBold` data. It produced a much larger `resources.assets` and caused a native Unity crash on the main menu.

## Known remaining work

- `Reconnect a controller` still appears once in the static patched `resources.assets`; it likely uses a different storage pattern or object path.
- `Continue` and `Loading` still appear in raw scans after patching, but may be prefab/object names rather than user-facing strings. Verify in-game.
- Font patching is the main blocker for BepInEx-free Korean display. Until TMP font references or fallbacks are patched correctly, Korean text can render as square boxes.
- The installer's TMP font patch is disabled by default. The experimental `--patch-font` path is not release-safe.
- The installer's `resources.assets` string patch is also disabled by default. The experimental `--patch-resources` path is not release-safe.

## Probe tools

- `tools/AssetProbe`
  - Finds raw string hits in `resources.assets`.
- `tools/StaticAssetPatcher`
  - Experimental standalone asset string patcher.
- `tools/BundleProbe`
  - Opens the root `font` bundle and confirms the bundled TMP SDF font assets.

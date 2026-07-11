using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

try
{
    var options = Options.Parse(args);
    var appDir = AppContext.BaseDirectory;
    var translationDir = ResolveTranslationDir(appDir, options.TranslationDir);
    var gameDir = ResolveGameDir(options.GameDir);
    var gameDataDir = Path.Combine(gameDir, "Sunless Skies_Data");
    var resourcesAssetsPath = Path.Combine(gameDataDir, "resources.assets");
    var packagedAssetsDir = ResolvePackagedAssetsDir(appDir);
    var fontPath = Path.Combine(gameDir, "font");
    var packagedFontPath = ResolvePackagedFont(appDir);
    var fontSourcePath = options.PatchFont ? packagedFontPath ?? (File.Exists(fontPath) ? fontPath : null) : null;
    var installDataDir = Path.Combine(gameDir, "Sunless Skies_Data", "StreamingAssets", "BuildInStorage", "data");
    var localDataDirs = ResolveLocalDataDirs();

    if (!File.Exists(resourcesAssetsPath))
    {
        throw new InvalidOperationException($"Unity resources file was not found: {resourcesAssetsPath}");
    }

    if (!Directory.Exists(installDataDir))
    {
        throw new InvalidOperationException($"Game data folder was not found: {installDataDir}");
    }

    var translations = LoadTranslations(translationDir);
    if (translations.Count == 0)
    {
        throw new InvalidOperationException($"No translation txt files were found: {translationDir}");
    }

    Console.WriteLine("Sunless Skies Korean direct patch installer");
    Console.WriteLine("------------------------------------------");
    Console.WriteLine($"Game folder: {gameDir}");
    Console.WriteLine($"Translation folder: {translationDir}");
    Console.WriteLine($"Translation keys: {translations.Count:N0}");
    Console.WriteLine(options.DryRun ? "Mode: dry run" : "Mode: patch files with backup");
    Console.WriteLine(options.PatchResources ? "Resources asset patch: experimental enabled" : "Resources asset patch: disabled");
    Console.WriteLine(options.PatchFont ? "TMP font patch: experimental enabled" : "TMP font patch: disabled");
    Console.WriteLine();

    var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    var backupRoot = Path.Combine(gameDir, $"_backup_korean_direct_patch_{stamp}");
    var total = 0;

    var packagedAssetNames = new[]
    {
        "resources.assets",
        "sharedassets0.assets",
        "level4",
        "level5",
        "level16",
        "level24",
        Path.Combine("il2cpp_data", "Metadata", "global-metadata.dat")
    };
    var packagedResourcesInstalled = false;
    foreach (var assetName in packagedAssetNames)
    {
        var packagedPath = packagedAssetsDir is null ? null : Path.Combine(packagedAssetsDir, assetName);
        if (packagedPath is not null && !File.Exists(packagedPath))
        {
            packagedPath = null;
        }

        total += InstallPackagedFile(
            "StaticAssets",
            Path.Combine(gameDataDir, assetName),
            packagedPath,
            backupRoot,
            options.DryRun);
        packagedResourcesInstalled |= assetName == "resources.assets" && packagedPath is not null;
    }

    if (options.PatchResources && !packagedResourcesInstalled)
    {
        total += PatchResourcesAssets("ResourcesAssets", resourcesAssetsPath, backupRoot, options.DryRun, translations, fontSourcePath, options.PatchFont);
    }
    else if (!packagedResourcesInstalled)
    {
        Console.WriteLine($"ResourcesAssets: {resourcesAssetsPath}");
        Console.WriteLine("  resources asset patch skipped");
        Console.WriteLine();
    }
    total += InstallPackagedFile("FontAssetBundle", fontPath, packagedFontPath, backupRoot, options.DryRun);
    total += PatchDataDirectory("InstallData", installDataDir, backupRoot, options.DryRun, translations);

    if (localDataDirs.Count > 0)
    {
        foreach (var localDataDir in localDataDirs)
        {
            total += PatchDataDirectory("LocalLowStorage", localDataDir, backupRoot, options.DryRun, translations);
        }
    }
    else
    {
        Console.WriteLine("LocalLowStorage: not found, skipped. The game may recreate it on next launch.");
        Console.WriteLine();
    }

    Console.WriteLine($"Total replacements: {total:N0}");
    if (options.DryRun)
    {
        Console.WriteLine("Dry run only. No files were changed.");
    }
    else
    {
        Console.WriteLine("Patch complete.");
        Console.WriteLine($"Backup folder: {backupRoot}");
    }
}
catch (UnauthorizedAccessException ex)
{
    Console.Error.WriteLine("No permission to write a file. Run the installer as administrator if needed.");
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Install failed.");
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
finally
{
    if (Environment.UserInteractive)
    {
        Console.WriteLine();
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
    }
}

static int PatchResourcesAssets(
    string label,
    string resourcesAssetsPath,
    string backupRoot,
    bool dryRun,
    IReadOnlyDictionary<string, string> translations,
    string? fontBundlePath,
    bool patchFont)
{
    Console.WriteLine($"{label}: {resourcesAssetsPath}");

    var fileBytes = File.ReadAllBytes(resourcesAssetsPath);
    using var sourceStream = new MemoryStream(fileBytes, writable: false);
    var manager = new AssetsManager();
    var instance = manager.LoadAssetsFile(sourceStream, Path.GetFileName(resourcesAssetsPath), loadDeps: false, bunInst: null);
    var file = instance.file;
    var touchedObjects = 0;
    var replacements = 0;

    foreach (var info in file.AssetInfos)
    {
        var offset = checked((int)info.GetAbsoluteByteOffset(file));
        var size = checked((int)info.ByteSize);
        if (offset < 0 || size <= 0 || offset + size > fileBytes.Length)
        {
            continue;
        }

        var objectBytes = new byte[size];
        Buffer.BlockCopy(fileBytes, offset, objectBytes, 0, size);

        var result = PatchUnityStrings(objectBytes, translations);
        if (result.Replacements == 0)
        {
            continue;
        }

        info.SetNewData(result.Bytes);
        touchedObjects++;
        replacements += result.Replacements;
    }

    var fontPatches = 0;
    if (!patchFont)
    {
        Console.WriteLine("  TMP font patch: skipped");
    }
    else if (!string.IsNullOrWhiteSpace(fontBundlePath) && File.Exists(fontBundlePath))
    {
        fontPatches = PatchTmpFontAssets(manager, file, fontBundlePath);
    }
    else
    {
        Console.WriteLine("  font bundle: not found, TMP font patch skipped");
    }

    Console.WriteLine($"  objects touched: {touchedObjects:N0}");
    Console.WriteLine($"  TMP font objects patched: {fontPatches:N0}");
    Console.WriteLine($"{label} text replacements: {replacements:N0}");
    Console.WriteLine($"{label} total operations: {replacements + fontPatches:N0}");
    Console.WriteLine();

    if (dryRun || (replacements == 0 && fontPatches == 0))
    {
        return replacements + fontPatches;
    }

    var backupPath = Path.Combine(backupRoot, label, Path.GetFileName(resourcesAssetsPath));
    Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
    File.Copy(resourcesAssetsPath, backupPath, overwrite: false);

    var tempPath = Path.Combine(Path.GetDirectoryName(resourcesAssetsPath)!, $"resources.assets.korean-patch-{Guid.NewGuid():N}.tmp");
    try
    {
        using (var stream = File.Create(tempPath))
        {
            var writer = new AssetsFileWriter(stream);
            file.Write(writer);
        }

        File.Copy(tempPath, resourcesAssetsPath, overwrite: true);
    }
    finally
    {
        if (File.Exists(tempPath))
        {
            File.Delete(tempPath);
        }
    }

    return replacements + fontPatches;
}

static int PatchTmpFontAssets(AssetsManager manager, AssetsFile resourcesFile, string fontBundlePath)
{
    var bundle = manager.LoadBundleFile(fontBundlePath, unpackIfPacked: true);
    var bundleInstance = manager.LoadAssetsFileFromBundle(bundle, 0, loadDeps: false);
    var bundleFile = bundleInstance.file;

    var nanumMaterial = ReadObjectBytes(bundleFile, FindAsset(bundleFile, -6577409021134209645));
    var nanumFont = ReadObjectBytes(bundleFile, FindAsset(bundleFile, 1796097383894433397));
    var nanumAtlas = ReadObjectBytes(bundleFile, FindAsset(bundleFile, 5131270961047409378));

    var targets = new[]
    {
        new FontTarget("EBGaramond-Regular", MaterialPathId: 161, AtlasPathId: 1160, FontPathId: 37337),
        new FontTarget("EBGaramond-Medium", MaterialPathId: 173, AtlasPathId: 1170, FontPathId: 37336),
        new FontTarget("LibreBaskerville-Regular", MaterialPathId: 165, AtlasPathId: 1165, FontPathId: 37340),
        new FontTarget("LibreBaskerville-Bold", MaterialPathId: 167, AtlasPathId: 1166, FontPathId: 36800),
    };

    var patchedObjects = 0;
    foreach (var target in targets)
    {
        var materialBytes = ReplacePathId(nanumMaterial, 5131270961047409378, target.AtlasPathId);
        var fontBytes = ReplacePathId(nanumFont, -6577409021134209645, target.MaterialPathId);
        fontBytes = ReplacePathId(fontBytes, 5131270961047409378, target.AtlasPathId);

        FindAsset(resourcesFile, target.MaterialPathId).SetNewData(materialBytes);
        FindAsset(resourcesFile, target.AtlasPathId).SetNewData(nanumAtlas);
        FindAsset(resourcesFile, target.FontPathId).SetNewData(fontBytes);
        patchedObjects += 3;
    }

    return patchedObjects;
}

static AssetFileInfo FindAsset(AssetsFile file, long pathId)
{
    return file.AssetInfos.FirstOrDefault(info => info.PathId == pathId)
        ?? throw new InvalidOperationException($"Asset pathId not found: {pathId}");
}

static byte[] ReadObjectBytes(AssetsFile file, AssetFileInfo info)
{
    var offset = checked((long)info.ByteOffset);
    var size = checked((int)info.ByteSize);
    var reader = file.Reader;
    reader.Position = offset;
    return reader.ReadBytes(size);
}

static byte[] ReplacePathId(byte[] source, long from, long to)
{
    var result = (byte[])source.Clone();
    var fromBytes = BitConverter.GetBytes(from);
    var toBytes = BitConverter.GetBytes(to);

    for (var i = 0; i <= result.Length - fromBytes.Length; i++)
    {
        var matched = true;
        for (var j = 0; j < fromBytes.Length; j++)
        {
            if (result[i + j] != fromBytes[j])
            {
                matched = false;
                break;
            }
        }

        if (!matched)
        {
            continue;
        }

        Buffer.BlockCopy(toBytes, 0, result, i, toBytes.Length);
        i += fromBytes.Length - 1;
    }

    return result;
}

static int PatchDataDirectory(
    string label,
    string dataDir,
    string backupRoot,
    bool dryRun,
    IReadOnlyDictionary<string, string> translations)
{
    Console.WriteLine($"{label}: {dataDir}");
    var total = 0;

    foreach (var sourcePath in Directory.EnumerateFiles(dataDir, "*.bytes").OrderBy(static p => p))
    {
        var sourceBytes = File.ReadAllBytes(sourcePath);
        var result = PatchDataFile(sourceBytes, translations);
        total += result.Replacements;

        Console.WriteLine($"  {Path.GetFileName(sourcePath),-16} {result.Replacements,5:N0} replacements");

        if (dryRun || result.Replacements == 0)
        {
            continue;
        }

        var backupPath = Path.Combine(backupRoot, label, Path.GetFileName(sourcePath));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        File.Copy(sourcePath, backupPath, overwrite: false);
        File.WriteAllBytes(sourcePath, result.Bytes);
    }

    Console.WriteLine($"{label} total: {total:N0}");
    Console.WriteLine();
    return total;
}

static int InstallPackagedFile(
    string label,
    string targetPath,
    string? packagedPath,
    string backupRoot,
    bool dryRun)
{
    Console.WriteLine($"{label}: {targetPath}");

    if (packagedPath is null)
    {
        Console.WriteLine($"  packaged {Path.GetFileName(targetPath)} not found, skipped");
        Console.WriteLine($"{label} total: 0");
        Console.WriteLine();
        return 0;
    }

    var targetExists = File.Exists(targetPath);
    var sameFile = false;
    var packagedInfo = new FileInfo(packagedPath);
    if (targetExists)
    {
        var targetInfo = new FileInfo(targetPath);
        sameFile = targetInfo.Length == packagedInfo.Length &&
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(targetPath))) ==
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(packagedPath)));
    }

    if (sameFile)
    {
        Console.WriteLine($"  {Path.GetFileName(targetPath),-16} already installed");
        Console.WriteLine($"{label} total: 0");
        Console.WriteLine();
        return 0;
    }

    var action = targetExists ? "replace" : "install";
    Console.WriteLine($"  {Path.GetFileName(targetPath),-16} {(dryRun ? $"would {action}" : $"{action}ed")}");
    Console.WriteLine($"{label} total: 1");
    Console.WriteLine();

    if (dryRun)
    {
        return 1;
    }

    if (targetExists)
    {
        var backupPath = Path.Combine(backupRoot, label, Path.GetFileName(targetPath));
        Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
        File.Copy(targetPath, backupPath, overwrite: false);
    }

    File.Copy(packagedPath, targetPath, overwrite: true);

    return 1;
}

static string ResolveTranslationDir(string appDir, string? explicitPath)
{
    var candidates = new List<string>();
    if (!string.IsNullOrWhiteSpace(explicitPath))
    {
        candidates.Add(explicitPath);
    }

    candidates.Add(Path.Combine(appDir, "translations"));
    candidates.Add(Path.Combine(appDir, "payload", "BepInEx", "Translation", "ko", "Text"));
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "translations"));
    candidates.Add(Path.Combine(Directory.GetCurrentDirectory(), "payload", "BepInEx", "Translation", "ko", "Text"));

    foreach (var candidate in candidates)
    {
        if (Directory.Exists(candidate) && Directory.EnumerateFiles(candidate, "*.txt").Any())
        {
            return Path.GetFullPath(candidate);
        }
    }

    throw new InvalidOperationException("Translation folder was not found. Put the translations folder next to the installer.");
}

static string? ResolvePackagedFont(string appDir)
{
    var candidates = new[]
    {
        Path.Combine(appDir, "font"),
        Path.Combine(appDir, "payload", "font"),
        Path.Combine(Directory.GetCurrentDirectory(), "font"),
        Path.Combine(Directory.GetCurrentDirectory(), "payload", "font")
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }
    }

    return null;
}

static string? ResolvePackagedAssetsDir(string appDir)
{
    var candidates = new[]
    {
        Path.Combine(appDir, "Sunless Skies_Data"),
        Path.Combine(appDir, "payload", "Sunless Skies_Data"),
        Path.Combine(Directory.GetCurrentDirectory(), "Sunless Skies_Data"),
        Path.Combine(Directory.GetCurrentDirectory(), "payload", "Sunless Skies_Data")
    };

    foreach (var candidate in candidates)
    {
        if (Directory.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }
    }

    return null;
}

static string ResolveGameDir(string? explicitPath)
{
    var candidates = new List<string>();
    if (!string.IsNullOrWhiteSpace(explicitPath))
    {
        candidates.Add(explicitPath);
    }

    candidates.Add(Directory.GetCurrentDirectory());
    candidates.Add(@"C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies");
    candidates.Add(@"C:\Program Files\Steam\steamapps\common\Sunless Skies");

    foreach (var candidate in candidates)
    {
        if (IsGameDir(candidate))
        {
            return Path.GetFullPath(candidate);
        }
    }

    while (true)
    {
        Console.Write("Enter the Sunless Skies game folder: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new InvalidOperationException("Game folder was not entered.");
        }

        input = input.Trim('"', ' ');
        if (IsGameDir(input))
        {
            return Path.GetFullPath(input);
        }

        Console.WriteLine("Sunless Skies.exe was not found. Try again.");
    }
}

static List<string> ResolveLocalDataDirs()
{
    var candidates = new List<string>();
    AddLocalDataCandidate(candidates, Environment.GetEnvironmentVariable("USERPROFILE"));
    AddLocalDataCandidate(candidates, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
    if (!string.IsNullOrWhiteSpace(localAppData))
    {
        var appData = Directory.GetParent(localAppData)?.FullName;
        if (!string.IsNullOrWhiteSpace(appData))
        {
            candidates.Add(Path.Combine(appData, "LocalLow", "Failbetter Games", "Sunless Skies", "storage", "data"));
        }
    }

    return candidates
        .Select(Path.GetFullPath)
        .Where(Directory.Exists)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
}

static void AddLocalDataCandidate(List<string> candidates, string? userProfile)
{
    if (string.IsNullOrWhiteSpace(userProfile))
    {
        return;
    }

    candidates.Add(Path.Combine(userProfile, "AppData", "LocalLow", "Failbetter Games", "Sunless Skies", "storage", "data"));
}

static bool IsGameDir(string path)
{
    return Directory.Exists(path) && File.Exists(Path.Combine(path, "Sunless Skies.exe"));
}

static Dictionary<string, string> LoadTranslations(string textDir)
{
    var translations = new Dictionary<string, string>(StringComparer.Ordinal);

    foreach (var path in Directory.EnumerateFiles(textDir, "*.txt").OrderBy(static p => p))
    {
        var name = Path.GetFileName(path);
        if (name.StartsWith('_'))
        {
            continue;
        }

        foreach (var rawLine in File.ReadLines(path, Encoding.UTF8))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var equals = FindTranslationSeparator(line);
            if (equals < 0)
            {
                continue;
            }

            var source = line[..equals].TrimStart('\uFEFF');
            var target = line[(equals + 1)..].TrimStart('\uFEFF');
            AddTranslation(translations, source, target);
        }
    }

    return translations;
}

static int FindTranslationSeparator(string line)
{
    for (var index = 0; index < line.Length; index++)
    {
        if (line[index] != '=')
        {
            continue;
        }

        var backslashes = 0;
        for (var previous = index - 1; previous >= 0 && line[previous] == '\\'; previous--)
        {
            backslashes++;
        }

        if (backslashes % 2 == 0)
        {
            return index;
        }
    }

    return -1;
}

static void AddTranslation(Dictionary<string, string> translations, string source, string target)
{
    if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || source == target)
    {
        return;
    }

    translations[source] = target;

    var unescapedSource = UnescapeCommon(source);
    var unescapedTarget = UnescapeCommon(target);
    if (unescapedSource != source || unescapedTarget != target)
    {
        translations[unescapedSource] = unescapedTarget;
    }
}

static string UnescapeCommon(string value)
{
    return value
        .Replace("\\=", "=", StringComparison.Ordinal)
        .Replace("\\/", "/", StringComparison.Ordinal)
        .Replace("\\r", "\r", StringComparison.Ordinal)
        .Replace("\\n", "\n", StringComparison.Ordinal)
        .Replace("\\t", "\t", StringComparison.Ordinal);
}

static PatchResult PatchUnityStrings(byte[] bytes, IReadOnlyDictionary<string, string> translations)
{
    using var output = new MemoryStream(bytes.Length);
    var pos = 0;
    var replacements = 0;

    while (pos < bytes.Length)
    {
        if (pos + 4 > bytes.Length)
        {
            output.Write(bytes, pos, bytes.Length - pos);
            break;
        }

        var length = BitConverter.ToInt32(bytes, pos);
        if (length <= 0 || length > 2000 || pos + 4 + length > bytes.Length)
        {
            output.WriteByte(bytes[pos]);
            pos++;
            continue;
        }

        string source;
        try
        {
            source = Utf8Strict.GetString(bytes, pos + 4, length);
        }
        catch (DecoderFallbackException)
        {
            output.WriteByte(bytes[pos]);
            pos++;
            continue;
        }

        var sourceEnd = Align4(pos + 4 + length);
        if (sourceEnd > bytes.Length || !LooksLikeText(source) || !translations.TryGetValue(source, out var target))
        {
            output.WriteByte(bytes[pos]);
            pos++;
            continue;
        }

        var targetBytes = Utf8Strict.GetBytes(target);
        output.Write(BitConverter.GetBytes(targetBytes.Length));
        output.Write(targetBytes);
        var paddedEnd = Align4((int)output.Position);
        while (output.Position < paddedEnd)
        {
            output.WriteByte(0);
        }

        replacements++;
        pos = sourceEnd;
    }

    return new PatchResult(output.ToArray(), replacements);
}

static int Align4(int value)
{
    return (value + 3) & ~3;
}

static PatchResult PatchDataFile(byte[] data, IReadOnlyDictionary<string, string> translations)
{
    using var output = new MemoryStream(data.Length);
    var pos = 0;
    var replacements = 0;

    while (pos < data.Length)
    {
        if (data[pos] != 0x01)
        {
            output.WriteByte(data[pos]);
            pos++;
            continue;
        }

        if (!TryReadVarInt(data, pos + 1, out var length, out var textStart) ||
            length <= 0 ||
            length > 20000 ||
            textStart + length > data.Length)
        {
            output.WriteByte(data[pos]);
            pos++;
            continue;
        }

        string source;
        try
        {
            source = Utf8Strict.GetString(data, textStart, length);
        }
        catch (DecoderFallbackException)
        {
            output.WriteByte(data[pos]);
            pos++;
            continue;
        }

        if (!LooksLikeText(source) || !translations.TryGetValue(source, out var target))
        {
            output.Write(data, pos, textStart + length - pos);
            pos = textStart + length;
            continue;
        }

        var targetBytes = Utf8Strict.GetBytes(target);
        output.WriteByte(0x01);
        WriteVarInt(output, targetBytes.Length);
        output.Write(targetBytes);
        replacements++;
        pos = textStart + length;
    }

    return new PatchResult(output.ToArray(), replacements);
}

static bool TryReadVarInt(byte[] data, int offset, out int value, out int nextOffset)
{
    value = 0;
    nextOffset = offset;
    var shift = 0;

    for (var i = 0; i < 5; i++)
    {
        if (nextOffset >= data.Length)
        {
            return false;
        }

        var b = data[nextOffset++];
        value |= (b & 0x7F) << shift;
        if (b < 0x80)
        {
            return true;
        }

        shift += 7;
    }

    return false;
}

static void WriteVarInt(Stream stream, int value)
{
    while (true)
    {
        var b = value & 0x7F;
        value >>= 7;
        if (value != 0)
        {
            stream.WriteByte((byte)(b | 0x80));
        }
        else
        {
            stream.WriteByte((byte)b);
            return;
        }
    }
}

static bool LooksLikeText(string value)
{
    if (string.IsNullOrEmpty(value) || value.Contains('\0', StringComparison.Ordinal))
    {
        return false;
    }

    var hasLetter = false;
    foreach (var ch in value)
    {
        if (ch is '\r' or '\n' or '\t')
        {
            continue;
        }

        if (char.IsControl(ch))
        {
            return false;
        }

        if (char.IsLetter(ch))
        {
            hasLetter = true;
        }
    }

    return hasLetter;
}

readonly record struct PatchResult(byte[] Bytes, int Replacements);
readonly record struct FontTarget(string Name, long MaterialPathId, long AtlasPathId, long FontPathId);

sealed record Options(string? GameDir, string? TranslationDir, bool DryRun, bool PatchResources, bool PatchFont)
{
    public static Options Parse(string[] args)
    {
        string? gameDir = null;
        string? translationDir = null;
        var dryRun = false;
        var patchResources = false;
        var patchFont = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--game-dir" when i + 1 < args.Length:
                    gameDir = args[++i];
                    break;
                case "--translation-dir" when i + 1 < args.Length:
                    translationDir = args[++i];
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--patch-resources":
                    patchResources = true;
                    break;
                case "--patch-font":
                    patchFont = true;
                    break;
                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {args[i]}");
            }
        }

        return new Options(gameDir, translationDir, dryRun, patchResources, patchFont);
    }

    static void PrintHelp()
    {
        Console.WriteLine("SunlessSkiesKoreanInstaller");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --game-dir <path>         Sunless Skies game folder");
        Console.WriteLine("  --translation-dir <path>  Translation txt folder");
        Console.WriteLine("  --dry-run                 Count replacements without changing files");
        Console.WriteLine("  --patch-resources         Experimental resources.assets string replacement");
        Console.WriteLine("  --patch-font              Experimental TMP font asset replacement");
    }
}

partial class Program
{
    internal static readonly UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}

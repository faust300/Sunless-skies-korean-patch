using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

Console.OutputEncoding = Encoding.UTF8;

var inputPath = args.Length > 0
    ? args[0]
    : @"C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies\Sunless Skies_Data\resources.assets";
var outputPath = args.Length > 1
    ? args[1]
    : Path.Combine(Directory.GetCurrentDirectory(), "_tmp_static_patch", "resources.assets");
var translationDir = args.Length > 2
    ? args[2]
    : Path.Combine(Directory.GetCurrentDirectory(), "payload", "BepInEx", "Translation", "ko", "Text");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var translations = LoadTranslations(translationDir);
Console.WriteLine($"Input: {inputPath}");
Console.WriteLine($"Output: {outputPath}");
Console.WriteLine($"Translations: {translations.Count:N0}");

var manager = new AssetsManager();
var instance = manager.LoadAssetsFile(inputPath, loadDeps: false);
var file = instance.file;
var fileBytes = File.ReadAllBytes(inputPath);
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
    Console.WriteLine($"patched pathId={info.PathId} type={info.TypeId} replacements={result.Replacements}");
}

Console.WriteLine($"Touched objects: {touchedObjects:N0}");
Console.WriteLine($"String replacements: {replacements:N0}");

await using (var stream = File.Create(outputPath))
{
    var writer = new AssetsFileWriter(stream);
    file.Write(writer);
}

Console.WriteLine("Written.");

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
        if (sourceEnd > bytes.Length || !LooksLikePatchableString(source) || !translations.TryGetValue(source, out var target))
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

static bool LooksLikePatchableString(string value)
{
    if (string.IsNullOrWhiteSpace(value) || value.Contains('\0', StringComparison.Ordinal))
    {
        return false;
    }

    if (value.Length > 500)
    {
        return false;
    }

    return value.Any(char.IsLetter);
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
            var line = rawLine.TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//", StringComparison.Ordinal) || !line.Contains('='))
            {
                continue;
            }

            var equals = FirstUnescapedEquals(line);
            if (equals < 0)
            {
                continue;
            }

            var source = line[..equals];
            var target = line[(equals + 1)..];
            AddTranslation(translations, source, target);
        }
    }

    return translations;
}

static int FirstUnescapedEquals(string line)
{
    for (var i = 0; i < line.Length; i++)
    {
        if (line[i] == '=' && (i == 0 || line[i - 1] != '\\'))
        {
            return i;
        }
    }

    return -1;
}

static void AddTranslation(Dictionary<string, string> translations, string source, string target)
{
    source = UnescapeCommon(source);
    target = UnescapeCommon(target);
    if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target) || source == target)
    {
        return;
    }

    translations[source] = target;
}

static string UnescapeCommon(string value)
{
    return value
        .Replace("\\=", "=", StringComparison.Ordinal)
        .Replace("\\/", "/", StringComparison.Ordinal)
        .Replace("\\s", " ", StringComparison.Ordinal)
        .Replace("\\r", "\r", StringComparison.Ordinal)
        .Replace("\\n", "\n", StringComparison.Ordinal)
        .Replace("\\t", "\t", StringComparison.Ordinal);
}

readonly record struct PatchResult(byte[] Bytes, int Replacements);

static partial class Program
{
    internal static readonly UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
}

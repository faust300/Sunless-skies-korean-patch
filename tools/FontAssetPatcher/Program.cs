using AssetsTools.NET;
using AssetsTools.NET.Extra;

var resourcesPath = args.Length > 0
    ? args[0]
    : @"C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies\Sunless Skies_Data\resources.assets";
var fontBundlePath = args.Length > 1
    ? args[1]
    : @"C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies\font";
var outputPath = args.Length > 2
    ? args[2]
    : Path.Combine(Directory.GetCurrentDirectory(), "_tmp_static_patch", "resources.fontpatched.assets");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

var manager = new AssetsManager();
var resourcesBytes = File.ReadAllBytes(resourcesPath);
using var resourcesStream = new MemoryStream(resourcesBytes, writable: false);
var resourcesInstance = manager.LoadAssetsFile(resourcesStream, Path.GetFileName(resourcesPath), loadDeps: false, bunInst: null);
var resourcesFile = resourcesInstance.file;

var bundle = manager.LoadBundleFile(fontBundlePath, unpackIfPacked: true);
var bundleInstance = manager.LoadAssetsFileFromBundle(bundle, 0, loadDeps: false);
var bundleFile = bundleInstance.file;

var nanumMaterial = ReadObjectBytes(bundleFile, FindAsset(bundleFile, -6577409021134209645));
var nanumFont = ReadObjectBytes(bundleFile, FindAsset(bundleFile, 1796097383894433397));
var nanumAtlas = ReadObjectBytes(bundleFile, FindAsset(bundleFile, 5131270961047409378));

Console.WriteLine($"Nanum material bytes: {nanumMaterial.Length:N0}");
Console.WriteLine($"Nanum font bytes: {nanumFont.Length:N0}");
Console.WriteLine($"Nanum atlas bytes: {nanumAtlas.Length:N0}");

var targets = new[]
{
    new FontTarget("EBGaramond-Regular", MaterialPathId: 161, AtlasPathId: 1160, FontPathId: 37337),
    new FontTarget("EBGaramond-Medium", MaterialPathId: 173, AtlasPathId: 1170, FontPathId: 37336),
    new FontTarget("LibreBaskerville-Regular", MaterialPathId: 165, AtlasPathId: 1165, FontPathId: 37340),
    new FontTarget("LibreBaskerville-Bold", MaterialPathId: 167, AtlasPathId: 1166, FontPathId: 36800),
};

foreach (var target in targets)
{
    Console.WriteLine($"Patching {target.Name}");

    var materialBytes = ReplacePathId(nanumMaterial, 5131270961047409378, target.AtlasPathId);
    var fontBytes = ReplacePathId(nanumFont, -6577409021134209645, target.MaterialPathId);
    fontBytes = ReplacePathId(fontBytes, 5131270961047409378, target.AtlasPathId);

    FindAsset(resourcesFile, target.MaterialPathId).SetNewData(materialBytes);
    FindAsset(resourcesFile, target.AtlasPathId).SetNewData(nanumAtlas);
    FindAsset(resourcesFile, target.FontPathId).SetNewData(fontBytes);
}

using (var stream = File.Create(outputPath))
{
    var writer = new AssetsFileWriter(stream);
    resourcesFile.Write(writer);
}

Console.WriteLine($"Written: {outputPath}");

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
    var replacements = 0;

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
        replacements++;
        i += fromBytes.Length - 1;
    }

    Console.WriteLine($"  pathId {from} -> {to}: {replacements} replacements");
    return result;
}

readonly record struct FontTarget(string Name, long MaterialPathId, long AtlasPathId, long FontPathId);

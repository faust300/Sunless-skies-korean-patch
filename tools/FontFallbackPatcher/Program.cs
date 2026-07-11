using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;

if (args.Length < 4)
{
    Console.Error.WriteLine("Usage: FontFallbackPatcher <sharedassets0.assets> <font-bundle> <output.assets> <game-root>");
    return 1;
}

var inputPath = Path.GetFullPath(args[0]);
var bundlePath = Path.GetFullPath(args[1]);
var outputPath = Path.GetFullPath(args[2]);
var gameRoot = Path.GetFullPath(args[3]);
var dataPath = Path.Combine(gameRoot, "Sunless Skies_Data");

const long targetMaterialPathId = 8;
const long targetAtlasPathId = 27;
const long targetFontPathId = 65;
const long targetShaderPathId = 31;
const long targetFontScriptPathId = 1533;

const long sourceMaterialPathId = -6577409021134209645;
const long sourceAtlasPathId = 5131270961047409378;
const long sourceFontPathId = 1796097383894433397;

var manager = new AssetsManager();
manager.LoadClassPackage(Path.Combine(AppContext.BaseDirectory, "classdata.tpk"));
manager.LoadClassDatabaseFromPackage("2019.4.2f1");

var generator = new Cpp2IlTempGenerator(
    Path.Combine(dataPath, "il2cpp_data", "Metadata", "global-metadata.dat"),
    Path.Combine(gameRoot, "GameAssembly.dll"));
generator.SetUnityVersion(2019, 4, 2);
generator.InitializeCpp2IL();
manager.MonoTempGenerator = generator;

var targetInstance = manager.LoadAssetsFile(inputPath, loadDeps: true);
var bundle = manager.LoadBundleFile(bundlePath, unpackIfPacked: true);
var sourceInstance = manager.LoadAssetsFileFromBundle(bundle, 0, loadDeps: false);

var targetFile = targetInstance.file;
var sourceFile = sourceInstance.file;

var material = ReadField(manager, targetInstance, targetMaterialPathId);
var atlas = ReadField(manager, targetInstance, targetAtlasPathId);
var font = ReadField(manager, targetInstance, targetFontPathId);

CopyCompatible(material, ReadField(manager, sourceInstance, sourceMaterialPathId));
CopyCompatible(atlas, ReadField(manager, sourceInstance, sourceAtlasPathId));
CopyCompatible(font, ReadField(manager, sourceInstance, sourceFontPathId));

SetPPtr(font, "m_Script", 1, targetFontScriptPathId);
SetPPtr(font, "material", 0, targetMaterialPathId);
SetFirstArrayPPtr(font, "m_AtlasTextures", 0, targetAtlasPathId);
SetPPtr(font, "atlas", 0, targetAtlasPathId, required: false);

SetPPtr(material, "m_Shader", 0, targetShaderPathId);
RemapPathIds(material, sourceAtlasPathId, targetAtlasPathId);

Console.WriteLine($"Font: {font["m_Name"].AsString}");
Console.WriteLine($"Material: {material["m_Name"].AsString}");
Console.WriteLine($"Atlas: {atlas["m_Name"].AsString}");

var replacers = new List<AssetsReplacer>
{
    new AssetsReplacerFromMemory(targetFile, FindInfo(targetFile, targetMaterialPathId), material.WriteToByteArray()),
    new AssetsReplacerFromMemory(targetFile, FindInfo(targetFile, targetAtlasPathId), atlas.WriteToByteArray()),
    new AssetsReplacerFromMemory(targetFile, FindInfo(targetFile, targetFontPathId), font.WriteToByteArray())
};

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await using (var stream = File.Create(outputPath))
{
    var writer = new AssetsFileWriter(stream);
    targetFile.Write(writer, 0, replacers, manager.ClassDatabase);
}

Console.WriteLine($"Written: {outputPath}");
return 0;

static AssetTypeValueField ReadField(AssetsManager manager, AssetsFileInstance instance, long pathId)
{
    return manager.GetBaseField(instance, pathId);
}

static AssetFileInfo FindInfo(AssetsFile file, long pathId)
{
    return file.AssetInfos.First(info => info.PathId == pathId);
}

static void SetPPtr(AssetTypeValueField root, string fieldName, int fileId, long pathId, bool required = true)
{
    var pptr = root.Children.FirstOrDefault(field => field.FieldName == fieldName);
    if (pptr is null)
    {
        if (required) throw new InvalidDataException($"Missing PPtr field: {fieldName}");
        return;
    }
    pptr["m_FileID"].AsInt = fileId;
    pptr["m_PathID"].AsLong = pathId;
}

static void SetFirstArrayPPtr(AssetTypeValueField root, string fieldName, int fileId, long pathId)
{
    var arrayField = root.Children.First(field => field.FieldName == fieldName)["Array"];
    var first = arrayField.Children.First();
    first["m_FileID"].AsInt = fileId;
    first["m_PathID"].AsLong = pathId;
}

static void RemapPathIds(AssetTypeValueField field, long source, long target)
{
    if (field.FieldName == "m_PathID" && field.TemplateField.ValueType == AssetValueType.Int64 && field.AsLong == source)
    {
        field.AsLong = target;
    }
    foreach (var child in field.Children)
    {
        RemapPathIds(child, source, target);
    }
}

static void CopyCompatible(AssetTypeValueField target, AssetTypeValueField source)
{
    if (target.TemplateField.ValueType != AssetValueType.None)
    {
        target.Value = source.Value;
        return;
    }

    var targetArray = target.Children.FirstOrDefault(static child => child.FieldName == "Array");
    var sourceArray = source.Children.FirstOrDefault(static child => child.FieldName == "Array");
    if (targetArray is not null && sourceArray is not null)
    {
        targetArray.Value = sourceArray.Value;
        targetArray.Children = sourceArray.Children;
        return;
    }

    foreach (var targetChild in target.Children)
    {
        var sourceChild = source.Children.FirstOrDefault(child => child.FieldName == targetChild.FieldName);
        if (sourceChild is not null)
        {
            CopyCompatible(targetChild, sourceChild);
        }
    }
}

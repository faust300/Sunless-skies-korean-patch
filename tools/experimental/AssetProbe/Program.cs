using System.Reflection;
using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;

var target = args.Length > 0
    ? args[0]
    : @"C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies\Sunless Skies_Data\resources.assets";

Console.WriteLine($"Target: {target}");
Console.WriteLine();

PrintApiShape();
Console.WriteLine();

var manager = new AssetsManager();
AssetsFileInstance instance;
try
{
    var classPackagePath = Path.Combine(AppContext.BaseDirectory, "classdata.tpk");
    if (!File.Exists(classPackagePath))
    {
        classPackagePath = Path.Combine(Directory.GetCurrentDirectory(), "tools", "AssetProbe", "classdata.tpk");
    }

    Console.WriteLine($"Class package: {classPackagePath}");
    manager.LoadClassPackage(classPackagePath);
    var dataPath = Path.GetDirectoryName(target)!;
    var managedPath = Path.Combine(dataPath, "Managed");
    if (Directory.Exists(managedPath))
    {
        Console.WriteLine($"Managed assemblies: {managedPath}");
        manager.MonoTempGenerator = new ManagedAssemblyTemplateGenerator(managedPath);
    }
    else
    {
        var gameRoot = Directory.GetParent(dataPath)!.FullName;
        var metadataPath = Path.Combine(dataPath, "il2cpp_data", "Metadata", "global-metadata.dat");
        var assemblyPath = Path.Combine(gameRoot, "GameAssembly.dll");
        Console.WriteLine($"IL2CPP metadata: {metadataPath}");
        Console.WriteLine($"IL2CPP assembly: {assemblyPath}");
        var cpp2IlGenerator = new Cpp2IlTempGenerator(metadataPath, assemblyPath);
        cpp2IlGenerator.SetUnityVersion(2019, 4, 2);
        cpp2IlGenerator.InitializeCpp2IL();
        manager.MonoTempGenerator = cpp2IlGenerator;
    }
    instance = manager.LoadAssetsFile(target, loadDeps: true);
    manager.LoadClassDatabaseFromPackage("2019.4.2f1");
}
catch (Exception ex)
{
    Console.WriteLine($"Initial load note: {ex.GetType().Name}: {ex.Message}");
    manager = new AssetsManager();
    instance = manager.LoadAssetsFile(target, loadDeps: false);
}

var file = instance.file;
Console.WriteLine($"Unity version: {file.Metadata.UnityVersion}");
Console.WriteLine($"Assets: {file.AssetInfos.Count}");
Console.WriteLine($"TypeTree count: {file.Metadata.TypeTreeTypes.Count}");
Console.WriteLine("Externals:");
for (var i = 0; i < file.Metadata.Externals.Count; i++)
{
    Console.WriteLine($"  {i + 1}: {file.Metadata.Externals[i].PathName}");
}
Console.WriteLine("Loaded files:");
foreach (var loaded in manager.Files)
{
    Console.WriteLine($"  {loaded.path}");
}
Console.WriteLine();

var interesting = args.Length > 1
    ? args.Skip(1).ToArray()
    : new[]
    {
        "Continue",
        "New Game",
        "Paused",
        "Loading",
        "Reconnect",
        "Start a New Game"
    };
var targetBytes = File.ReadAllBytes(target);
PrintRawAssetHits(file, targetBytes, interesting);
Console.WriteLine();
var selectedPathIds = args.Skip(1)
    .Select(static value => long.TryParse(value, out var pathId) ? pathId : (long?)null)
    .Where(static value => value.HasValue)
    .Select(static value => value!.Value)
    .ToArray();
DumpSelectedFields(manager, instance, selectedPathIds.Length == 0 ? new long[] { 37944, 45131, 45140 } : selectedPathIds);
Console.WriteLine();

var hits = 0;
foreach (var info in file.AssetInfos)
{
    if (hits >= 80)
    {
        break;
    }

    try
    {
        var field = manager.GetBaseField(instance, info);
        if (field is null)
        {
            continue;
        }

        var texts = new List<(string Path, string Value)>();
        CollectStrings(field, field.FieldName, texts);
        var matched = texts
            .Where(t => interesting.Any(s => t.Value.Contains(s, StringComparison.OrdinalIgnoreCase)))
            .Take(8)
            .ToList();

        if (matched.Count == 0)
        {
            continue;
        }

        hits++;
        Console.WriteLine($"[{hits}] pathId={info.PathId} type={info.TypeId} size={info.ByteSize}");
        foreach (var (path, value) in matched)
        {
            Console.WriteLine($"  {path} = {Trim(value)}");
        }
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        // Some MonoBehaviour fields may not deserialize without a class database.
    }
}

Console.WriteLine();
Console.WriteLine($"Hits: {hits}");
return 0;

static void PrintRawAssetHits(AssetsFile file, byte[] targetBytes, string[] interesting)
{
    Console.WriteLine("Raw asset hits:");
    var hits = 0;
    foreach (var info in file.AssetInfos)
    {
        var offset = checked((int)info.GetAbsoluteByteStart(file));
        var size = checked((int)info.ByteSize);
        if (offset < 0 || size <= 0 || offset + size > targetBytes.Length)
        {
            continue;
        }

        var matched = new List<string>();
        foreach (var needle in interesting)
        {
            if (IndexOfAscii(targetBytes, offset, size, needle) >= 0)
            {
                matched.Add(needle);
            }
        }

        if (matched.Count == 0)
        {
            continue;
        }

        hits++;
        Console.WriteLine($"  pathId={info.PathId} type={info.TypeId} size={info.ByteSize} text=[{string.Join(", ", matched)}]");
        if (hits >= 80)
        {
            break;
        }
    }

    Console.WriteLine($"Raw hits: {hits}");
}

static void DumpSelectedFields(AssetsManager manager, AssetsFileInstance instance, long[] pathIds)
{
    Console.WriteLine("Selected field dumps:");
    foreach (var pathId in pathIds)
    {
        try
        {
            var field = manager.GetBaseField(instance, pathId);
            Console.WriteLine($"  pathId={pathId} root={field.FieldName} children={field.Children.Count}");
            DumpField(field, "    ", 0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  pathId={pathId} failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

static void DumpField(AssetTypeValueField field, string indent, int depth)
{
    if (depth > 4)
    {
        return;
    }

    var value = FormatValue(field);
    Console.WriteLine($"{indent}{field.FieldName} ({field.TemplateField.ValueType}){value}");

    foreach (var child in field.Children.Take(60))
    {
        DumpField(child, indent + "  ", depth + 1);
    }
}

static string FormatValue(AssetTypeValueField field)
{
    return field.TemplateField.ValueType switch
    {
        AssetValueType.String => $" = {Trim(field.AsString)}",
        AssetValueType.Int8 or AssetValueType.UInt8 or AssetValueType.Int16 or AssetValueType.UInt16 or
            AssetValueType.Int32 or AssetValueType.UInt32 or AssetValueType.Int64 or AssetValueType.UInt64 =>
            $" = {field.AsLong}",
        AssetValueType.Bool => $" = {field.AsBool}",
        AssetValueType.Float => $" = {field.AsFloat}",
        AssetValueType.Double => $" = {field.AsDouble}",
        _ => ""
    };
}

static int IndexOfAscii(byte[] data, int offset, int size, string needle)
{
    var bytes = System.Text.Encoding.ASCII.GetBytes(needle);
    var end = offset + size - bytes.Length;
    for (var i = offset; i <= end; i++)
    {
        var ok = true;
        for (var j = 0; j < bytes.Length; j++)
        {
            if (data[i + j] != bytes[j])
            {
                ok = false;
                break;
            }
        }

        if (ok)
        {
            return i;
        }
    }

    return -1;
}

static void CollectStrings(AssetTypeValueField field, string path, List<(string Path, string Value)> output)
{
    if (field.TemplateField.ValueType == AssetValueType.String)
    {
        var value = field.AsString;
        if (!string.IsNullOrWhiteSpace(value))
        {
            output.Add((path, value));
        }
    }

    foreach (var child in field.Children)
    {
        CollectStrings(child, $"{path}.{child.FieldName}", output);
    }
}

static string Trim(string value)
{
    value = value.Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
    return value.Length <= 180 ? value : value[..180] + "...";
}

static void PrintApiShape()
{
    var asm = typeof(AssetsManager).Assembly;
    Console.WriteLine($"AssetsTools.NET: {asm.Location}");
    Console.WriteLine("AssetsManager methods:");
    foreach (var method in typeof(AssetsManager).GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.Name is "LoadAssetsFile" or "GetBaseField" or "GetTypeInstance" or "LoadClassPackage" or "LoadClassDatabaseFromPackage")
        .OrderBy(m => m.Name)
        .ThenBy(m => m.GetParameters().Length))
    {
        var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({parameters})");
    }
}

sealed class ManagedAssemblyTemplateGenerator : IMonoBehaviourTemplateGenerator
{
    private readonly string managedPath;
    private readonly MonoCecilTempGenerator inner;

    public ManagedAssemblyTemplateGenerator(string managedPath)
    {
        this.managedPath = managedPath;
        inner = new MonoCecilTempGenerator(managedPath);
    }

    public List<AssetTypeTemplateField> Read(string assemblyName, string nameSpace, string className, UnityVersion unityVersion)
    {
        return inner.Read(ResolveAssembly(assemblyName), nameSpace, className, unityVersion);
    }

    public AssetTypeTemplateField GetTemplateField(AssetTypeTemplateField baseField, string assemblyName,
        string nameSpace, string className, UnityVersion unityVersion)
    {
        return inner.GetTemplateField(baseField, ResolveAssembly(assemblyName), nameSpace, className, unityVersion);
    }

    public void Dispose() => inner.Dispose();

    private string ResolveAssembly(string assemblyName)
    {
        return Path.IsPathRooted(assemblyName) ? assemblyName : Path.Combine(managedPath, assemblyName);
    }
}

using System.Reflection;
using AssetsTools.NET;
using AssetsTools.NET.Extra;

var target = args.Length > 0
    ? args[0]
    : Path.Combine(@"C:\Program Files (x86)\Steam\steamapps\common\Sunless Skies", "font");

Console.WriteLine($"Target: {target}");
Console.WriteLine();

PrintBundleApiShape();

var manager = new AssetsManager();
var bundle = manager.LoadBundleFile(target, unpackIfPacked: true);
Console.WriteLine($"Bundle name: {bundle.name}");
Console.WriteLine($"Bundle file type: {bundle.file.GetType().FullName}");
DumpPublicShape("Bundle file", bundle.file);
Console.WriteLine();

for (var i = 0; i < 12; i++)
{
    AssetsFileInstance instance;
    try
    {
        instance = manager.LoadAssetsFileFromBundle(bundle, i, loadDeps: false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{i}] not loaded: {ex.GetType().Name}: {ex.Message}");
        continue;
    }

    var file = instance.file;
    Console.WriteLine($"[{i}] Unity version: {file.Metadata.UnityVersion}");
    Console.WriteLine($"[{i}] Assets: {file.AssetInfos.Count}");

    foreach (var info in file.AssetInfos.Take(40))
    {
        Console.WriteLine($"  pathId={info.PathId} type={info.TypeId} size={info.ByteSize}");
        if (i == 0 && info.PathId == file.AssetInfos[0].PathId)
        {
            DumpPublicShape("AssetFileInfo", info);
            Console.WriteLine();
            DumpPublicShape("AssetsFile", file);
            Console.WriteLine();
        }
        try
        {
            var field = manager.GetBaseField(instance, info);
            var strings = new List<string>();
            CollectStrings(field, strings);
            foreach (var text in strings.Where(IsInterestingString).Take(8))
            {
                Console.WriteLine($"    {Trim(text)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    field read failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

static void PrintBundleApiShape()
{
    Console.WriteLine("Bundle-related methods:");
    foreach (var method in typeof(AssetsManager).GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => m.Name.Contains("Bundle", StringComparison.OrdinalIgnoreCase))
        .OrderBy(m => m.Name)
        .ThenBy(m => m.GetParameters().Length))
    {
        var parameters = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"  {method.ReturnType.Name} {method.Name}({parameters})");
    }

    Console.WriteLine();
}

static void DumpPublicShape(string label, object value)
{
    var type = value.GetType();
    Console.WriteLine($"{label} public fields:");
    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
    {
        object? fieldValue = null;
        try
        {
            fieldValue = field.GetValue(value);
        }
        catch
        {
            // Reflection probe only.
        }

        Console.WriteLine($"  {field.FieldType.Name} {field.Name} = {FormatValue(fieldValue)}");
    }

    Console.WriteLine($"{label} public properties:");
    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        object? propertyValue = null;
        try
        {
            if (property.GetIndexParameters().Length == 0)
            {
                propertyValue = property.GetValue(value);
            }
        }
        catch
        {
            // Reflection probe only.
        }

        Console.WriteLine($"  {property.PropertyType.Name} {property.Name} = {FormatValue(propertyValue)}");
    }
}

static string FormatValue(object? value)
{
    if (value is null)
    {
        return "<null>";
    }

    if (value is string text)
    {
        return text;
    }

    var type = value.GetType();
    if (typeof(System.Collections.ICollection).IsAssignableFrom(type))
    {
        return $"{type.Name}";
    }

    return value.ToString() ?? type.Name;
}

static void CollectStrings(AssetTypeValueField field, List<string> output)
{
    if (field.TemplateField.ValueType == AssetValueType.String && !string.IsNullOrWhiteSpace(field.AsString))
    {
        output.Add(field.AsString);
    }

    foreach (var child in field.Children)
    {
        CollectStrings(child, output);
    }
}

static bool IsInterestingString(string value)
{
    return value.Contains("arial", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("SDF", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Material", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("TMP", StringComparison.OrdinalIgnoreCase) ||
        value.Contains("Atlas", StringComparison.OrdinalIgnoreCase);
}

static string Trim(string value)
{
    value = value.Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
    return value.Length <= 180 ? value : value[..180] + "...";
}

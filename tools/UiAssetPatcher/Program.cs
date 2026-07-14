using System.Text;
using AssetsTools.NET;
using AssetsTools.NET.Cpp2IL;
using AssetsTools.NET.Extra;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: UiAssetPatcher <input.assets> <output.assets> <translation-dir> [untranslated-report] [skipped-field-report] [detailed-untranslated-report]");
    return 1;
}

var inputPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
var translationDir = Path.GetFullPath(args[2]);
var reportPath = args.Length > 3 ? Path.GetFullPath(args[3]) : null;
var skippedFieldReportPath = args.Length > 4 ? Path.GetFullPath(args[4]) : null;
var detailedUntranslatedReportPath = args.Length > 5 ? Path.GetFullPath(args[5]) : null;
var translations = LoadTranslations(translationDir);

Console.WriteLine($"Input: {inputPath}");
Console.WriteLine($"Output: {outputPath}");
Console.WriteLine($"Translations: {translations.Count:N0}");

var dataPath = Path.GetDirectoryName(inputPath)!;
var gameRoot = Directory.GetParent(dataPath)!.FullName;
var metadataPath = Path.Combine(dataPath, "il2cpp_data", "Metadata", "global-metadata.dat");
var assemblyPath = Path.Combine(gameRoot, "GameAssembly.dll");

var manager = new AssetsManager();
manager.LoadClassPackage(Path.Combine(AppContext.BaseDirectory, "classdata.tpk"));
manager.LoadClassDatabaseFromPackage("2019.4.2f1");

var generator = new Cpp2IlTempGenerator(metadataPath, assemblyPath);
generator.SetUnityVersion(2019, 4, 2);
generator.InitializeCpp2IL();
manager.MonoTempGenerator = generator;

var instance = manager.LoadAssetsFile(inputPath, loadDeps: true);
var file = instance.file;
var touched = 0;
var failures = 0;
var replacers = new List<AssetsReplacer>();
var untranslated = new Dictionary<string, List<long>>(StringComparer.Ordinal);
var untranslatedByField = new Dictionary<(string FieldName, string Source), List<long>>();
var skippedTranslatedFields = new Dictionary<(string FieldName, string Source, string Target), List<long>>();

foreach (var info in file.AssetInfos.Where(static info => info.TypeId == 114))
{
    try
    {
        var root = manager.GetBaseField(instance, info);
        var allTextFields = EnumerateFields(root)
            .Where(field => field.TemplateField.ValueType == AssetValueType.String)
            .ToList();
        if (skippedFieldReportPath is not null)
        {
            foreach (var field in allTextFields.Where(field => !IsTranslatableTextField(info.PathId, field.FieldName)))
            {
                var source = field.AsString;
                if (!translations.TryGetValue(source, out var target) || source == target)
                {
                    continue;
                }

                var key = (field.FieldName, source, target);
                if (!skippedTranslatedFields.TryGetValue(key, out var pathIds))
                {
                    pathIds = new List<long>();
                    skippedTranslatedFields[key] = pathIds;
                }
                pathIds.Add(info.PathId);
            }
        }

        var textFields = allTextFields
            .Where(field => IsTranslatableTextField(info.PathId, field.FieldName))
            .ToList();
        if (textFields.Count == 0)
        {
            continue;
        }

        var objectTouched = false;
        foreach (var textField in textFields)
        {
            var source = textField.AsString;
            if (!translations.TryGetValue(source, out var target) || source == target)
            {
                if (IsReportableDisplayField(info.PathId, textField.FieldName) && LooksLikeUntranslatedEnglish(source))
                {
                    if (!untranslated.TryGetValue(source, out var pathIds))
                    {
                        pathIds = new List<long>();
                        untranslated[source] = pathIds;
                    }
                    pathIds.Add(info.PathId);

                    var detailedKey = (textField.FieldName, source);
                    if (!untranslatedByField.TryGetValue(detailedKey, out var detailedPathIds))
                    {
                        detailedPathIds = new List<long>();
                        untranslatedByField[detailedKey] = detailedPathIds;
                    }
                    detailedPathIds.Add(info.PathId);
                }
                continue;
            }

            textField.AsString = target;
            objectTouched = true;
            Console.WriteLine($"  pathId={info.PathId} field={textField.FieldName}: {OneLine(source)} -> {OneLine(target)}");
        }

        if (objectTouched)
        {
            replacers.Add(new AssetsReplacerFromMemory(file, info, root));
            touched++;
        }
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        failures++;
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await using (var stream = File.Create(outputPath))
{
    var writer = new AssetsFileWriter(stream);
    file.Write(writer, 0, replacers, manager.ClassDatabase);
}

Console.WriteLine($"UI text fields patched: {touched:N0}");
Console.WriteLine($"Unreadable MonoBehaviours skipped: {failures:N0}");
Console.WriteLine($"Untranslated English UI strings: {untranslated.Count:N0}");
if (reportPath is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
    File.WriteAllLines(reportPath, untranslated
        .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        .Select(static pair => $"{string.Join(',', pair.Value)}\t{EscapeReport(pair.Key)}"), Encoding.UTF8);
    Console.WriteLine($"Report: {reportPath}");
}
if (skippedFieldReportPath is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(skippedFieldReportPath)!);
    File.WriteAllLines(skippedFieldReportPath, skippedTranslatedFields
        .OrderBy(static pair => pair.Key.FieldName, StringComparer.Ordinal)
        .ThenBy(static pair => pair.Key.Source, StringComparer.OrdinalIgnoreCase)
        .Select(static pair => $"{pair.Key.FieldName}\t{string.Join(',', pair.Value)}\t{EscapeReport(pair.Key.Source)}\t{EscapeReport(pair.Key.Target)}"), Encoding.UTF8);
    Console.WriteLine($"Translated strings in skipped fields: {skippedTranslatedFields.Sum(static pair => pair.Value.Count):N0}");
    Console.WriteLine($"Skipped field report: {skippedFieldReportPath}");
}
if (detailedUntranslatedReportPath is not null)
{
    Directory.CreateDirectory(Path.GetDirectoryName(detailedUntranslatedReportPath)!);
    File.WriteAllLines(detailedUntranslatedReportPath, untranslatedByField
        .OrderBy(static pair => pair.Key.FieldName, StringComparer.Ordinal)
        .ThenBy(static pair => pair.Key.Source, StringComparer.OrdinalIgnoreCase)
        .Select(static pair => $"{pair.Key.FieldName}\t{string.Join(',', pair.Value)}\t{EscapeReport(pair.Key.Source)}"), Encoding.UTF8);
    Console.WriteLine($"Detailed untranslated report: {detailedUntranslatedReportPath}");
}
return 0;

static Dictionary<string, string> LoadTranslations(string textDir)
{
    var translations = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var path in Directory.EnumerateFiles(textDir, "*.txt").OrderBy(static path => path))
    {
        if (Path.GetFileName(path).StartsWith('_'))
        {
            continue;
        }

        foreach (var rawLine in File.ReadLines(path, Encoding.UTF8))
        {
            var line = rawLine.TrimStart('\uFEFF');
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            var equals = FirstUnescapedEquals(line);
            if (equals < 1)
            {
                continue;
            }

            var source = Unescape(line[..equals]);
            var target = Unescape(line[(equals + 1)..]);
            if (!string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(target) && source != target)
            {
                translations[source] = target;
            }
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

static string Unescape(string value) => value
    .Replace("\\=", "=", StringComparison.Ordinal)
    .Replace("\\/", "/", StringComparison.Ordinal)
    .Replace("\\s", " ", StringComparison.Ordinal)
    .Replace("\\r", "\r", StringComparison.Ordinal)
    .Replace("\\n", "\n", StringComparison.Ordinal)
    .Replace("\\t", "\t", StringComparison.Ordinal);

static string OneLine(string value)
{
    value = value.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal);
    return value.Length <= 100 ? value : value[..100] + "...";
}

static IEnumerable<AssetTypeValueField> EnumerateFields(AssetTypeValueField field)
{
    yield return field;
    foreach (var child in field.Children)
    {
        foreach (var nested in EnumerateFields(child))
        {
            yield return nested;
        }
    }
}

static bool IsTranslatableTextField(long pathId, string fieldName) =>
    fieldName is "m_text" or "Message" or "LandmarkName" or "LandmarkDescription" or
        "AmbientMessage" or "InteractVerb" ||
    pathId == 41666 && fieldName == "data";

static bool IsReportableDisplayField(long pathId, string fieldName) =>
    IsTranslatableTextField(pathId, fieldName);

static bool LooksLikeUntranslatedEnglish(string value)
{
    if (string.IsNullOrWhiteSpace(value) || value.Any(static c => c is >= '\uAC00' and <= '\uD7A3'))
    {
        return false;
    }
    return value.Any(static c => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z');
}

static string EscapeReport(string value)
{
    var trailingSpaces = value.Length - value.TrimEnd(' ').Length;
    var body = trailingSpaces == 0 ? value : value[..^trailingSpaces];
    return body
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal) + string.Concat(Enumerable.Repeat("\\s", trailingSpaces));
}

using System.Buffers.Binary;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: GlobalMetadataPatcher <global-metadata.dat> <output.dat> <translations.txt>");
    return 1;
}

var inputPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
var translations = LoadTranslations(Path.GetFullPath(args[2]));
var searchTerms = args.Skip(3).ToArray();
var data = File.ReadAllBytes(inputPath);

if (ReadInt32(data, 0) != unchecked((int)0xFAB11BAF))
{
    throw new InvalidDataException("Not an IL2CPP global metadata file.");
}

var version = ReadInt32(data, 4);
var literalTableOffset = ReadInt32(data, 8);
var literalTableBytes = ReadInt32(data, 12);
var literalDataOffset = ReadInt32(data, 16);
var literalDataBytes = ReadInt32(data, 20);

Console.WriteLine($"Metadata version: {version}");
Console.WriteLine($"String literals: {literalTableBytes / 8:N0}");

using var output = new MemoryStream(data.Length + 4096);
output.Write(data);
var patched = 0;
var satisfied = 0;

for (var entryOffset = literalTableOffset; entryOffset < literalTableOffset + literalTableBytes; entryOffset += 8)
{
    var sourceLength = ReadInt32(data, entryOffset);
    var sourceIndex = ReadInt32(data, entryOffset + 4);
    if (sourceLength <= 0 || sourceIndex < 0 || sourceIndex + sourceLength > literalDataBytes)
    {
        continue;
    }

    var source = Encoding.UTF8.GetString(data, literalDataOffset + sourceIndex, sourceLength);
    if (searchTerms.Any(term => source.Contains(term, StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine($"  [literal] {source.Replace("\r", "\\r", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal)}");
    }
    if (!translations.TryGetValue(source, out var target))
    {
        if (translations.Values.Contains(source, StringComparer.Ordinal))
        {
            satisfied++;
        }
        continue;
    }

    var targetBytes = Encoding.UTF8.GetBytes(target);
    int targetIndex;
    if (targetBytes.Length <= sourceLength)
    {
        targetIndex = sourceIndex;
        output.Position = literalDataOffset + sourceIndex;
        output.Write(targetBytes);
        for (var i = targetBytes.Length; i < sourceLength; i++)
        {
            output.WriteByte(0);
        }
    }
    else
    {
        output.Position = output.Length;
        targetIndex = checked((int)(output.Position - literalDataOffset));
        output.Write(targetBytes);
    }

    WriteInt32(output, entryOffset, targetBytes.Length);
    WriteInt32(output, entryOffset + 4, targetIndex);
    patched++;
    satisfied++;
    Console.WriteLine($"  {source} -> {target}");
}

var outputBytes = output.ToArray();
foreach (var (source, target) in translations)
{
    if (!source.Any(static ch => ch >= 0xAC00 && ch <= 0xD7A3)) continue;
    var sourceBytes = Encoding.UTF8.GetBytes(source);
    var targetBytes = Encoding.UTF8.GetBytes(target);
    if (targetBytes.Length > sourceBytes.Length) continue;

    for (var offset = 0; offset <= outputBytes.Length - sourceBytes.Length; offset++)
    {
        if (!outputBytes.AsSpan(offset, sourceBytes.Length).SequenceEqual(sourceBytes)) continue;
        targetBytes.CopyTo(outputBytes.AsSpan(offset));
        outputBytes.AsSpan(offset + targetBytes.Length, sourceBytes.Length - targetBytes.Length).Clear();
        patched++;
        offset += sourceBytes.Length - 1;
        Console.WriteLine($"  [raw] {source} -> {target}");
    }
}

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
File.WriteAllBytes(outputPath, outputBytes);
Console.WriteLine($"Patched literals: {patched}");
Console.WriteLine($"Satisfied translations: {satisfied}/{translations.Count}");
Console.WriteLine($"Written: {outputPath}");
return satisfied == translations.Count ? 0 : 2;

static Dictionary<string, string> LoadTranslations(string path)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var rawLine in File.ReadLines(path, Encoding.UTF8))
    {
        var line = rawLine.TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
        var equals = line.IndexOf('=');
        if (equals <= 0) continue;
        result[Unescape(line[..equals])] = Unescape(line[(equals + 1)..]);
    }
    return result;
}

static string Unescape(string value) => value
    .Replace("\\r", "\r", StringComparison.Ordinal)
    .Replace("\\n", "\n", StringComparison.Ordinal)
    .Replace("\\t", "\t", StringComparison.Ordinal);

static int ReadInt32(byte[] data, int offset) => BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(offset, 4));

static void WriteInt32(MemoryStream stream, int offset, int value)
{
    Span<byte> buffer = stackalloc byte[4];
    BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
    var position = stream.Position;
    stream.Position = offset;
    stream.Write(buffer);
    stream.Position = position;
}

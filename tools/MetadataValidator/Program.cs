using AssetsTools.NET.Cpp2IL;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: MetadataValidator <global-metadata.dat> <GameAssembly.dll>");
    return 1;
}

var generator = new Cpp2IlTempGenerator(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
generator.SetUnityVersion(2019, 4, 2);
generator.InitializeCpp2IL();
generator.Dispose();
Console.WriteLine("IL2CPP metadata validation succeeded.");
return 0;

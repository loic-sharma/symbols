using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

namespace Cobalt.Symbols.Downloader
{
    class Program
    {
        // https://github.com/dotnet/runtime/blob/4f9ae42d861fcb4be2fcd5d3d55d5f227d30e723/src/libraries/System.Reflection.Metadata/src/System/Reflection/Metadata/PortablePdb/PortablePdbVersions.cs#L41
        private const ushort PortableCodeViewVersionMagic = 0x504d;

        private static readonly Uri SymbolEndpoint = new Uri("https://symbols.nuget.org/download/symbols/");
        private static readonly HttpClient Http = new HttpClient();

        static async Task Main(string[] args)
        {
            var downloads = PrepareDownloads(args);

            var tasks = Enumerable
                .Repeat(0, 32)
                .Select(async _ =>
                {
                    while (downloads.TryTake(out var downloadCtx))
                    {
                        await DownloadSymbolAsync(downloadCtx);
                    }
                });

            await Task.WhenAll(tasks);
        }

        private static ConcurrentBag<DownloadSymbolContext> PrepareDownloads(string[] assemblyPaths)
        {
            var downloads = new ConcurrentBag<DownloadSymbolContext>();

            foreach (var assemblyPath in assemblyPaths)
            {
                try
                {
                    using (var stream = new FileStream(assemblyPath, FileMode.Open))
                    using (var reader = new PEReader(stream))
                    {
                        var data = ReadDebugData(reader);
                        if (data.HasEmbeddedSymbols)
                        {
                            Console.WriteLine($"Assembly '{assemblyPath}' has embedded symbols");
                            continue;
                        }

                        var destinationPath = Path.ChangeExtension(assemblyPath, ".pdb");

                        downloads.Add(new DownloadSymbolContext(assemblyPath, destinationPath, data.Keys));
                    }
                }
                catch (FileNotFoundException)
                {
                    Console.WriteLine($"Could not find '{assemblyPath}'...");
                }
            }

            return downloads;
        }

        private static DebugData ReadDebugData(PEReader reader)
        {
            var entries = reader.ReadDebugDirectory();

            var hasEmbeddedSymbols = entries.Any(e => e.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);

            var checksums = entries
                .Where(e => e.Type == DebugDirectoryEntryType.PdbChecksum)
                .Select(reader.ReadPdbChecksumDebugDirectoryData)
                .Select(data =>
                {
                    var builder = new StringBuilder();

                    builder.Append(data.AlgorithmName);
                    builder.Append(':');

                    foreach (var bytes in data.Checksum)
                    {
                        builder.Append(bytes.ToString("x2"));
                    }

                    return builder.ToString();
                })
                .ToList();

            var keys = entries
                .Where(e => e.Type == DebugDirectoryEntryType.CodeView)
                .Select(entry =>
                {
                    var data = reader.ReadCodeViewDebugDirectoryData(entry);
                    var isPortable = entry.MinorVersion == PortableCodeViewVersionMagic;

                    var signature = data.Guid;
                    var age = data.Age;
                    var file = Uri.EscapeDataString(Path.GetFileName(data.Path.Replace('\\', '/')).ToLowerInvariant());

                    // Portable PDBs, see: https://github.com/dotnet/symstore/blob/83032682c049a2b879790c615c27fbc785b254eb/src/Microsoft.SymbolStore/KeyGenerators/PortablePDBFileKeyGenerator.cs#L84
                    // Windows PDBs, see: https://github.com/dotnet/symstore/blob/83032682c049a2b879790c615c27fbc785b254eb/src/Microsoft.SymbolStore/KeyGenerators/PDBFileKeyGenerator.cs#L52
                    var symbolId = isPortable
                        ? signature.ToString("N") + "FFFFFFFF"
                        : string.Format("{0}{1:x}", signature.ToString("N"), age);

                    return new SymbolKey($"{file}/{symbolId}/{file}", checksums);
                })
                .ToList();

            return new DebugData(hasEmbeddedSymbols, keys);
        }

        private static async Task DownloadSymbolAsync(DownloadSymbolContext downloadCtx)
        {
            foreach (var key in downloadCtx.Keys)
            {
                using (var request = new HttpRequestMessage())
                {
                    request.Method = HttpMethod.Get;
                    request.RequestUri = new Uri(SymbolEndpoint, key.Key); ;

                    if (key.Checksums.Any())
                    {
                        request.Headers.Add("SymbolChecksum", string.Join(";", key.Checksums));
                    }

                    Console.WriteLine($"GET {request.RequestUri.AbsoluteUri}");

                    var stopwatch = Stopwatch.StartNew();
                    using (var response = await Http.SendAsync(request))
                    {
                        Console.WriteLine($"    HTTP {response.StatusCode} - {stopwatch.ElapsedMilliseconds} ms");

                        if (!response.IsSuccessStatusCode)
                        {
                            continue;
                        }

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var destinationStream = new FileStream(downloadCtx.DestinationPath, FileMode.Create))
                        {
                            await contentStream.CopyToAsync(destinationStream);
                            return;
                        }
                    }
                }
            }

            Console.WriteLine($"Could not find symbol {downloadCtx.AssemblyPath}");
        }

        public class DebugData
        {
            public DebugData(
                bool hasEmbeddedSymbols,
                IReadOnlyList<SymbolKey> keys)
            {
                HasEmbeddedSymbols = hasEmbeddedSymbols;
                Keys = keys;
            }

            public bool HasEmbeddedSymbols { get; }
            public IReadOnlyList<SymbolKey> Keys { get; }
        }

        public class SymbolKey
        {
            public SymbolKey(string key, IReadOnlyList<string> checksums)
            {
                Key = key;
                Checksums = checksums;
            }

            public string Key { get; set; }
            public IReadOnlyList<string> Checksums { get; set; }
        }

        public class DownloadSymbolContext
        {
            public DownloadSymbolContext(
                string assemblyPath,
                string destinationPath,
                IReadOnlyList<SymbolKey> keys)
            {
                AssemblyPath = assemblyPath;
                DestinationPath = destinationPath;
                Keys = keys;
            }

            public string AssemblyPath { get; }
            public string DestinationPath { get; }
            public IReadOnlyList<SymbolKey> Keys { get; }
        }
    }
}

# Cobalt.Symbols.MSBuild

This project contains the build magic to download symbols. It hooks into the build and finds assemblies that are written to the output directory. If the assembly is missing symbols, it uses the [`Cobalt.Symbols.Downloader`](Cobalt.Symbols.Downloader/README.md) tool to download the symbols from the nuget.org symbol server.
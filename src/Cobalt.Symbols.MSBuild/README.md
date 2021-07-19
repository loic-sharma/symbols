# Cobalt.Symbols.MSBuild

This project is used to create the NuGet package for the MSBuild task.

Steps:

1. Create the tool to download symbols
    1. Navigate to the tool's directory: `cd ../Cobalt.Symbols.Downloader`
    1. Create the tool:
        1. `dotnet publish -c Release -f net472`
        1. `dotnet publish -c Release -f net5.0`

1. Create the NuGet package
    1. Navigate to the package's directory: `cd ../Cobalt.Symbols.MSBuild`
    1. Create the package: `dotnet pack -c Release`
    1. Open `./bin/Release/Cobalt.Symbols.MSBuild.1.0.0.nupkg` using NuGet Package Explorer
    1. Add the tool to the NuGet package
        1. Copy all files from `../Cobalt.Symbols.Downloader/bin/Release/net472/publish/` into the package at path `tools/net472`
        1. Copy all files from `../Cobalt.Symbols.Downloader/bin/Release/net5.0/publish/` into the package at path `tools/net5.0`

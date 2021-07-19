# Symbol downloader

This downloads symbols missing from your build. Use it by adding a dependency to `Cobalt.Symbols.MSBuild`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  ...

  <ItemGroup>
    <PackageReference Include="Cobalt.Symbols.MSBuild" Version="1.0.0" />
  </ItemGroup>

</Project>
```

## Development

Building this package requires manual steps:

Steps:

1. Create the tool to download symbols
    1. `dotnet publish -c Release -f net472 ./src/Cobalt.Symbols.Downloader/`
    1. `dotnet publish -c Release -f net5.0 ./src/Cobalt.Symbols.Downloader/`

1. Create the NuGet package
    1. Create the base package: `dotnet pack -c Release ./src/Cobalt.Symbols.MSBuild`
    1. Use NuGet Package Explorer to open `./src/Cobalt.Symbols.MSBuild/bin/Release/Cobalt.Symbols.MSBuild.1.0.0.nupkg`
    1. Add the tool to the NuGet package
        1. Copy all files from `./src/Cobalt.Symbols.Downloader/bin/Release/net472/publish/` into the package at path `tools/net472`
        1. Copy all files from `../src/Cobalt.Symbols.Downloader/bin/Release/net5.0/publish/` into the package at path `tools/net5.0`

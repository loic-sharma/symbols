# Cobalt.Symbols.Downloader

This a tool to download assemblies' symbols. For example:

```ps1
Cobalt.Symbols.Downloader.exe "assembly1.dll" "assembly2.dll"
```

This will try to download symbols from nuget.org's symbol server. If symbols are found, the tool will place them next to the input assemblies.
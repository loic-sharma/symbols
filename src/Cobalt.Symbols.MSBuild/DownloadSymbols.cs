using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Cobalt.Symbols.MSBuild
{
    public class DownloadSymbols : ToolTask
    {
        [Required]
        public string ExecutablePath { get; set; }

        [Required]
        public ITaskItem[] Assemblies { get; set; }

        protected override string ToolName => "NuGet.Symbols.Downloader.exe";

        protected override string GenerateFullPathToTool()
        {
            return ExecutablePath;

            // return @"D:\Code\Scratch\1\src\NuGet.Symbols.Downloader\bin\Debug\net5.0\NuGet.Symbols.Downloader.exe";
        }

        protected override string GenerateCommandLineCommands()
        {
            var builder = new CommandLineBuilder();

            foreach (var assembly in Assemblies)
            {
                builder.AppendFileNameIfNotNull(assembly);
            }

            return builder.ToString();
        }
    }

    // public class DownloadSymbols : Microsoft.Build.Utilities.Task
    // {
    //     [Required]
    //     public ITaskItem[] Assemblies { get; set; }

    //     public override bool Execute()
    //     {
    //         Log.LogMessage(
    //             MessageImportance.High,
    //             "Received {0} assemblies.",
    //             string.Join(", ", (IEnumerable<ITaskItem>)Assemblies));

    //         // Stopped with this approach. MSBuild runs using .NET Framework in Visual Studio.
    //         // We need System.Reflection.Metadata package on .NET Framework,
    //         // but MSBuild Tasks don't have great support for packages.
    //         // See: https://natemcmaster.com/blog/2017/11/11/msbuild-task-with-dependencies/

    //         //PEReader

    //         return true;
    //     }
    // }
}
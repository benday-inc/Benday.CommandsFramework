using Benday.CommandsFramework;
using Benday.CommandsFramework.Samples;
using System.Diagnostics;
using System.Reflection;

namespace Benday.CommandsFramework.Samples;

class Program
{
    static void Main(string[] args)
    {
        var assembly = typeof(SampleCommand1).Assembly;

        var versionInfo =
            FileVersionInfo.GetVersionInfo(
                Assembly.GetExecutingAssembly().Location);

        var options = new DefaultProgramOptions();

        options.Version = $"v{versionInfo.FileVersion}";
        options.ApplicationName = "Sample Tool using Commands Framework";
        options.Website = "https://www.benday.com";

        var program = new DefaultProgram(options, assembly);

        program.Run(args);
    }
}
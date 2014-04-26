
using CommandLine;
using CommandLine.Text;

namespace ChangeVcxproj
{
    public class Options
    {
        [Option('r', "root", 
            MetaValue = "DIRECTORY", 
            Required = true,
            HelpText = "Root directory within where all *.vcxproj files will be checked and updated if necessary.")]
        public string RootPath { get; set; }

        [Option("ptver", HelpText =
            "Platform toolset version for VC++ project. Default value: v100 (used in VS2010). If this parameter is not specified. You can change this value in higher level VC++ projects to make your porject be compatable with down level ones. ")]
        public string PlatformToolsetVersion { get; set; }

        [Option("gt", HelpText = 
            "Add GTest related include and libraries to *.vcxproj settings")]
        public bool SupportGTest { get; set; }

        [Option("gtinc", HelpText = "Additional include directories for GTest. Semicolon-delimited string.")]
        public string GTestIncludePaths { get; set; }

        [Option("gtlibdirs", HelpText = "Additional library directories for GTest. Semicolon-delimited string.")]
        public string GTestLibDirs { get; set; }
        [Option("gtlibdirsd", HelpText = "Additional library directories for GTest. Semicolon-delimited string. Debug version.")]
        public string GTestLibDirsDebug { get; set; }
        [Option("gtdepends", HelpText = "Additional dependencies for GTest. Semicolon-delimited string.")]
        public string GTestDependencies { get; set; }
        [Option("gtdependsd", HelpText = "Additional dependencies for GTest. Semicolon-delimited string. Debug version.")]
        public string GTestDependenciesDebug { get; set; }

        [Option("ignorelibs", HelpText = "Ignore Specific Default Libraries. Semicolon-delimited string.")]
        public string IgnoreSpecificDefaultLibraries { get; set; }
        [Option("ignorelibsd", HelpText = "Ignore Specific Default Libraries. Semicolon-delimited string. Debug version.")]
        public string IgnoreSpecificDefaultLibrariesDebug { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this, 
                current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}

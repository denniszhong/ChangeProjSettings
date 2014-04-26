using System;
using System.IO;
using ChangeVcxproj.Modifier;
using CommandLine;

namespace ChangeVcxproj
{
    partial class Program
    {
        #region definitions

        static Options options = new Options();

        #endregion

        static void Main(string[] args)
        {
            // -r "D:\Dropbox\WS\Code\DSQ\Algs\Sort\projs\heapsort - Copy"
            
            // -r "D:\Dropbox\WS\Code\DSQ\Algs\Sort\projs\heapsort - Copy" --gt --gtinc "" --gtlibdirs "" --gtdepends "" --ignorelibs "" --gtlibdirsd "" --gtdependsd "" --ignorelibsd ""
            // -r "D:\Dropbox\WS\Code\DSQ\Algs\Sort\projs\heapsort - Copy" --gt --gtinc "D:\Dropbox\WS\Code\OS\Code.Google\gtest-1.7.0\include" --gtlibdirs "D:\Dropbox\WS\Code\OS\Code.Google\gtest-1.7.0\msvc\gtest-md\Release" --gtdepends "gtest.lib" --ignorelibs "libcpmt.lib;libcmt.lib" --gtlibdirsd "D:\Dropbox\WS\Code\OS\Code.Google\gtest-1.7.0\msvc\gtest-md\Debug" --gtdependsd "gtestd.lib" --ignorelibsd "libcpmtd.lib;libcmtd.lib"


            var parser = new Parser(with => with.HelpWriter = Console.Error);
            
            //if (CommandLine.Parser.Default.ParseArguments(args, options))
            //{
            //    // Values are available here
            //    if (options.SupportGTest) 
            //        Console.WriteLine("Include Dir: {0}", options.GTestInclude);
            //}

            if (parser.ParseArgumentsStrict(args, options, 
                () => Environment.Exit(-2)))
            {
                if (!Directory.Exists(options.RootPath))
                {
                    Console.WriteLine("Specific path does not exist.");
                    return;
                }

                string searchPatten = "*.*proj";

                string[] filePaths = Directory.GetFiles(options.RootPath,
                    searchPatten, SearchOption.AllDirectories);
                if (filePaths.Length == 0)
                {
                    Console.WriteLine(string.Format("Didn't find any {0} file under {1}!", 
                                                    searchPatten, 
                                                    options.RootPath));
                    return;
                }

                foreach (var file in filePaths)
                {
                    Console.WriteLine(string.Format("\nStart to modify settings to file: {0}", file));
                    
                    ModifierFactory modifierFactory = GetModifierFactory(new FileInfo(file).Extension);
                    if (modifierFactory != null &&
                        modifierFactory.CreateModifier().ModifySettings(file, options))
                    {
                        Console.WriteLine("Successfully modified settings!");
                    }
                    else
                    {
                        Console.Error.WriteLine("Failed to modify settings!");
                    }
                }
            }
        }

        static ModifierFactory GetModifierFactory(string suffix)
        {
            switch (suffix.ToUpper())
            {
                case ".VCXPROJ":
                    return new VcxporjModifierFactory();
                case ".CSPROJ":
                    return new CsprojModifierFactory();
                default:
                    return null;
            }
        }
    }
}

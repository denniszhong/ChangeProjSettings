using System;
using System.Linq;
using System.Xml.Linq;

namespace ChangeVcxproj.Modifier
{
    public class VcxprojModifier : Modifier
    {
        /// <summary>
        /// Root path for temp files
        /// </summary>
        static readonly string TEMP_ROOT_PATH = "D:\\Temp\\Slns\\CPP_$(SolutionName)\\$(ProjectName)\\";

        /// <summary>
        /// Definitions of Output Directory, Intermediate Directory and Build Log File
        /// </summary>
        static readonly string OUT_DIR = TEMP_ROOT_PATH + "$(OutputType)\\$(Configuration)\\$(PlatformShortName)\\";
        static readonly string INT_DIR = TEMP_ROOT_PATH + "IntDir_$(IntDir)$(PlatformShortName)\\";
        static readonly string BUILD_LOG_FILE = TEMP_ROOT_PATH + "log\\$(PlatformShortName)\\$(MSBuildProjectName).log";

        /// <summary>
        /// Definitions of two different conditions used for Debug/Release configurations
        /// </summary>
        static readonly string CONDITION_DEBUG_Win32 = "'$(Configuration)|$(Platform)'=='Debug|Win32'";
        static readonly string CONDITION_RELEASE_Win32 = "'$(Configuration)|$(Platform)'=='Release|Win32'";


        public override bool ModifySettings(string file, Options paramOptions)
        {
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            XElement root = XElement.Load(file);
            bool needSave = false;

            // 
            // Set <PlatformToolset /> if it is specified va param: ptver
            // 
            if (!string.IsNullOrEmpty(paramOptions.PlatformToolsetVersion) &&
                !SetProjectToolset(root, ns, ref needSave, paramOptions.PlatformToolsetVersion))
            {
                return false;
            }

            // 
            // Set <OutDir/> and <IntDir/>
            // 
            if (!SetOutDir(root, ns, ref needSave, OUT_DIR))
            {
                return false;
            }

            // 
            // Set BuildLog_Path
            // 
            if (!SetBuildLogPath(root, ns, ref needSave, BUILD_LOG_FILE))
            {
                return false;
            }

            // 
            // Set GTEST settings, including the include path, library directories, and dependent libraries and ignored dependent libraries
            // 
            if (paramOptions.SupportGTest && 
                !SetGTestSettings(root, ns, paramOptions, ref needSave))
            {
                return false;
            }

            if (needSave)
                root.Save(file);

            return true;
        }

        private bool SetProjectToolset(
            XElement root, 
            XNamespace ns, 
            ref bool needSave, 
            string platformToolsetVersion)
        {
            try
            {
                var propertyGroups =
                    from el in root.Elements(ns + "PropertyGroup")
                    where (
                            el.Attribute("Condition") != null &&  // "Condition" attribute must exist
                            el.Attribute("Label") != null &&     // Must contains: Label="Configuration"
                            el.Attribute("Label").Value.Equals("Configuration") &&
                            ( // Restrict values of thee attribute "Condition"
                              el.Attribute("Condition").Value.Equals(CONDITION_DEBUG_Win32) ||
                              el.Attribute("Condition").Value.Equals(CONDITION_RELEASE_Win32)
                            )
                          )
                    select el;

                //int count = (itemDefinitionGroups.ToList<XElement>()).Count;
                foreach (var propertyGroup in propertyGroups)
                {
                    XElement elPlatformToolset = propertyGroup.Element(ns + "PlatformToolset");

                    if (elPlatformToolset != null)
                    {
                        if (elPlatformToolset.Value.Equals(platformToolsetVersion))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <PlatformToolset />, already existed."));
                        }
                        else
                        {
                            elPlatformToolset.Value = platformToolsetVersion;
                            Console.WriteLine(string.Format("\t<PlatformToolset /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <OutDir /> element
                        elPlatformToolset = new XElement(ns + "PlatformToolset", platformToolsetVersion);
                        propertyGroup.Add(elPlatformToolset);
                        Console.WriteLine(string.Format("\t<PlatformToolset /> was added successfully."));
                        needSave = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        private static bool SetGTestSettings(
            XElement root, XNamespace ns, 
            Options options, ref bool needSave)
        {
            try
            {
                string inc = options.GTestIncludePaths;

                string libDirs = options.GTestLibDirs;
                string libDirsD = options.GTestLibDirsDebug;

                string depends = options.GTestDependencies;
                string dependsD = options.GTestDependenciesDebug;

                string ignorelibs = options.IgnoreSpecificDefaultLibraries;
                string ignorelibsD = options.IgnoreSpecificDefaultLibrariesDebug;

                bool checkGTestSettings = false;
                if (checkGTestSettings)
                {
                    // TODO: check above values
                }

                #region Set <AdditionalIncludeDirectories /> element

                // query the ClCompile elements for both Debug and Release
                var subElementsOfClCompile =
                    from el in root.Descendants((ns + "ClCompile"))  // Use Descendants() to get all descendants
                    where                                            // (not only the children of root node)
                    (
                        el.Parent.Attribute("Condition") != null &&
                        (el.Parent.Attribute("Condition").Value.Equals(CONDITION_DEBUG_Win32) ||
                          el.Parent.Attribute("Condition").Value.Equals(CONDITION_RELEASE_Win32))
                    )
                    select el;

                foreach (var subElement in subElementsOfClCompile)
                {
                    XElement elAdditionalIncludeDirectories = subElement.Element(ns + "AdditionalIncludeDirectories");
                    if (elAdditionalIncludeDirectories != null)
                    {
                        // Edit <AdditionalIncludeDirectories />
                        if (elAdditionalIncludeDirectories.Value.Equals(inc))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <AdditionalIncludeDirectories />, already existed."));
                        }
                        else
                        {
                            elAdditionalIncludeDirectories.Value = inc;
                            Console.WriteLine(string.Format("\t<AdditionalIncludeDirectories /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <AdditionalIncludeDirectories /> element
                        elAdditionalIncludeDirectories = new XElement(ns + "AdditionalIncludeDirectories", inc);
                        subElement.Add(elAdditionalIncludeDirectories);
                        Console.WriteLine(string.Format("\t<AdditionalIncludeDirectories /> was added successfully."));
                        needSave = true;
                    }
                }

                #endregion

                #region Edit elements under <Link />  (Release configration)

                // query the CLCompile elements for both Debug and Release
                var subElementsOfLink =                        // Use Descendants() to get all descendants    
                    from el in root.Descendants(ns + "Link")   // (not only the children of root node)
                    where (
                        el.Parent.Attribute("Condition") != null &&
                        (el.Parent.Attribute("Condition").Value.Equals(CONDITION_RELEASE_Win32)))
                    select el;

                foreach (var subElement in subElementsOfLink)
                {
                    #region <AdditionalLibraryDirectories /> <== libDirs

                    XElement elLibDirs = subElement.Element(ns + "AdditionalLibraryDirectories");
                    if (elLibDirs != null)
                    {
                        // Edit <AdditionalLibraryDirectories />
                        if (elLibDirs.Value.Equals(libDirs))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <AdditionalLibraryDirectories />, already existed."));
                        }
                        else
                        {
                            // Add <AdditionalLibraryDirectories />
                            elLibDirs.Value = libDirs;
                            Console.WriteLine(string.Format("\t<AdditionalLibraryDirectories /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <AdditionalIncludeDirectories /> element
                        elLibDirs = new XElement(ns + "AdditionalLibraryDirectories", libDirs);
                        subElement.Add(elLibDirs);
                        Console.WriteLine(string.Format("\t<AdditionalLibraryDirectories /> was added successfully."));
                        needSave = true;
                    }

                    #endregion

                    #region <AdditionalDependencies />  <== depends

                    XElement elDepends = subElement.Element(ns + "AdditionalDependencies");
                    if (elDepends != null)
                    {
                        // Edit <AdditionalDependencies />
                        if (elDepends.Value.Equals(depends) || // Need check if $(AdditionalDependencies) already existed
                            elDepends.Value.Equals(depends + ";%(AdditionalDependencies)"))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <AdditionalDependencies />, already existed."));
                        }
                        else
                        {
                            // Add <AdditionalDependencies />
                            elDepends.Value = depends;
                            Console.WriteLine(string.Format("\t<AdditionalDependencies /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <AdditionalIncludeDirectories /> element
                        elDepends = new XElement(ns + "AdditionalDependencies", depends);
                        subElement.Add(elDepends);
                        Console.WriteLine(string.Format("\t<AdditionalDependencies /> was added successfully."));
                        needSave = true;
                    }


                    // Add ;%(AdditionalDependencies) for <AdditionalDependencies />
                    if (!elDepends.Value.Contains(";%(AdditionalDependencies)"))
                    {
                        elDepends.Value += ";%(AdditionalDependencies)";
                        needSave = true;
                    }

                    #endregion

                    #region <IgnoreSpecificDefaultLibraries />  <== ignorelibs

                    XElement elIgnoreLibs = subElement.Element(ns + "IgnoreSpecificDefaultLibraries");
                    if (elIgnoreLibs != null)
                    {
                        // Edit <IgnoreSpecificDefaultLibraries />
                        if (elIgnoreLibs.Value.Equals(ignorelibs))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <IgnoreSpecificDefaultLibraries />, already existed."));
                        }
                        else
                        {
                            // Add <IgnoreSpecificDefaultLibraries />
                            elIgnoreLibs.Value = ignorelibs;
                            Console.WriteLine(string.Format("\t<IgnoreSpecificDefaultLibraries /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <AdditionalIncludeDirectories /> element
                        elIgnoreLibs = new XElement(ns + "IgnoreSpecificDefaultLibraries", ignorelibs);
                        subElement.Add(elIgnoreLibs);
                        Console.WriteLine(string.Format("\t<IgnoreSpecificDefaultLibraries /> was added successfully."));
                        needSave = true;
                    }

                    #endregion
                }

                #endregion

                #region Edit elements under <Link />  (Debug configration)

                // query the CLCompile elements for both Debug and Release
                var subElementsOfLinkDebug =
                    from el in root.Descendants(ns + "Link")  // Use Descendants() to get all descendants 
                    where (                                   // (not only the children of root node)
                        el.Parent.Attribute("Condition") != null &&
                        (el.Parent.Attribute("Condition").Value.Equals(CONDITION_DEBUG_Win32)))
                    select el;

                foreach (var subElement in subElementsOfLinkDebug)
                {
                    #region <AdditionalLibraryDirectories /> <== libDirsd

                    XElement elLibDirs = subElement.Element(ns + "AdditionalLibraryDirectories");
                    if (elLibDirs != null)
                    {
                        // Edit <AdditionalLibraryDirectories />
                        if (elLibDirs.Value.Equals(libDirsD))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <AdditionalLibraryDirectories />, already existed."));
                        }
                        else
                        {
                            // Add <AdditionalLibraryDirectories />
                            elLibDirs.Value = libDirsD;
                            Console.WriteLine(string.Format("\t<AdditionalLibraryDirectories /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <AdditionalIncludeDirectories /> element
                        elLibDirs = new XElement(ns + "AdditionalLibraryDirectories", libDirsD);
                        subElement.Add(elLibDirs);
                        Console.WriteLine(string.Format("\t<AdditionalLibraryDirectories /> was added successfully."));
                        needSave = true;
                    }

                    #endregion

                    #region <AdditionalDependencies />  <== depends

                    XElement elDepends = subElement.Element(ns + "AdditionalDependencies");
                    if (elDepends != null)
                    {
                        // Edit <AdditionalDependencies />
                        if (elDepends.Value.Equals(dependsD) || // Need check if $(AdditionalDependencies) already existed
                            elDepends.Value.Equals(dependsD + ";%(AdditionalDependencies)"))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <AdditionalDependencies />, already existed."));
                        }
                        else
                        {
                            // Add <AdditionalDependencies />
                            elDepends.Value = dependsD;
                            Console.WriteLine(string.Format("\t<AdditionalDependencies /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <AdditionalIncludeDirectories /> element
                        elDepends = new XElement(ns + "AdditionalDependencies", dependsD);
                        subElement.Add(elDepends);
                        Console.WriteLine(string.Format("\t<AdditionalDependencies /> was added successfully."));
                        needSave = true;
                    }

                    // Add ;%(AdditionalDependencies) for <AdditionalDependencies />
                    if (!elDepends.Value.Contains(";%(AdditionalDependencies)"))
                    {
                        elDepends.Value += ";%(AdditionalDependencies)";
                        needSave = true;
                    }

                    #endregion

                    #region <IgnoreSpecificDefaultLibraries />  <== ignorelibs

                    XElement elIgnoreLibs = subElement.Element(ns + "IgnoreSpecificDefaultLibraries");
                    if (elIgnoreLibs != null)
                    {
                        // Edit <IgnoreSpecificDefaultLibraries />
                        if (elIgnoreLibs.Value.Equals(ignorelibsD))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <IgnoreSpecificDefaultLibraries />, already existed."));
                        }
                        else
                        {
                            // Add <IgnoreSpecificDefaultLibraries />
                            elIgnoreLibs.Value = ignorelibsD;
                            Console.WriteLine(string.Format("\t<IgnoreSpecificDefaultLibraries /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <AdditionalIncludeDirectories /> element
                        elIgnoreLibs = new XElement(ns + "IgnoreSpecificDefaultLibraries", ignorelibsD);
                        subElement.Add(elIgnoreLibs);
                        Console.WriteLine(string.Format("\t<IgnoreSpecificDefaultLibraries /> was added successfully."));
                        needSave = true;
                    }

                    #endregion
                }

                #endregion
            }
            catch (System.Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        private static bool SetOutDir(
            XElement root, 
            XNamespace ns, 
            ref bool needSave, 
            string outDir)
        {
            try
            {
                var propertyGroups =
                    from el in root.Elements(ns + "PropertyGroup")
                    where (
                            el.Attribute("Condition") != null &&  // "Condition" attribute must exist
                            el.Attribute("Label") == null &&      // Exclude elements which contains attribute "Label"
                            ( // Restrict values of thee attribute "Condition"
                              el.Attribute("Condition").Value.Equals(CONDITION_DEBUG_Win32) ||
                              el.Attribute("Condition").Value.Equals(CONDITION_RELEASE_Win32)
                            )
                          )
                    select el;

                //int count = (propertyGroups.ToList<XElement>()).Count;
                foreach (var propertyGroup in propertyGroups)
                {
                    XElement elementOutDir = propertyGroup.Element(ns + "OutDir");
                    if (elementOutDir != null)
                    {
                        // Edit <OutDir /> element
                        if (elementOutDir.Value.Equals(outDir))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <OutDir />, already existed."));
                        }
                        else
                        {
                            elementOutDir.Value = outDir;
                            Console.WriteLine(string.Format("\t<OutDir /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <OutDir /> element
                        elementOutDir = new XElement(ns + "OutDir", outDir);
                        propertyGroup.Add(elementOutDir);
                        Console.WriteLine(string.Format("\t<OutDir /> was added successfully."));
                        needSave = true;
                    }

                    XElement elementIntDir = propertyGroup.Element(ns + "IntDir");
                    if (elementIntDir != null)
                    {
                        // Edit <IntDir /> element
                        if (elementIntDir.Value.Equals(INT_DIR))
                        {
                            Console.WriteLine(string.Format("\tSkipping setting <IntDir />, already existed."));
                        }
                        else
                        {
                            elementIntDir.Value = INT_DIR;
                            Console.WriteLine(string.Format("\t<IntDir /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <IntDir /> element
                        elementIntDir = new XElement(ns + "IntDir", INT_DIR);
                        propertyGroup.Add(elementIntDir);
                        Console.WriteLine(string.Format("\t<IntDir /> was added successfully."));
                        needSave = true;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        private static bool SetBuildLogPath(
            XElement root,
            XNamespace ns,
            ref bool needSave,
            string buildLogPath)
        {
            try
            {
                var itemDefinitionGroups =
                    from el in root.Elements(ns + "ItemDefinitionGroup")
                    where (el.Attribute("Condition") != null &&  // "Condition" attribute must exist
                            (el.Attribute("Condition").Value.Equals(CONDITION_DEBUG_Win32) ||
                              el.Attribute("Condition").Value.Equals(CONDITION_RELEASE_Win32)))
                    select el;

                //int count = (itemDefinitionGroups.ToList<XElement>()).Count;
                foreach (var itemDefinitionGroup in itemDefinitionGroups)
                {
                    XElement elementBuildLog = itemDefinitionGroup.Element(ns + "BuildLog");
                    XElement elementBuildLogPath = null;

                    if (elementBuildLog == null)
                    {
                        elementBuildLogPath = new XElement(ns + "BuildLog", new XElement(ns + "Path", buildLogPath));
                        itemDefinitionGroup.Add(elementBuildLogPath);

                        Console.WriteLine(string.Format(
                            "\t<BuildLog><Path></Path></BuildLog> was added successfully."));
                        needSave = true;
                    }
                    else
                    {
                        elementBuildLogPath = elementBuildLog.Element(ns + "Path");
                        if (elementBuildLogPath == null)
                        {
                            // BuildLog exist, but BuildLog/Path not exist,
                            // Create <Path /> under <BuildLog />
                            elementBuildLogPath = new XElement(ns + "Path", buildLogPath);
                            elementBuildLog.Add(elementBuildLogPath);
                            Console.WriteLine(string.Format(
                                "\t<Path /> was added to <BuildLog /> successfully."));
                            needSave = true;
                        }
                        else
                        {
                            // Edit <Path /> in <BuildLog />
                            if (elementBuildLogPath.Value.Equals(buildLogPath))
                            {
                                Console.WriteLine(string.Format(
                                    "\tSkipping setting <BuildLog><Path></Path></BuildLog>, already existed."));
                            }
                            else
                            {
                                elementBuildLogPath.Value = buildLogPath;
                                Console.WriteLine(string.Format(
                                    "\t<BuildLog><Path></Path></BuildLog> was updated successfully."));
                                needSave = true;
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return false;
            }

            return true;
        }
    }
}

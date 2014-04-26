using System;
using System.Linq;
using System.Xml.Linq;

namespace ChangeVcxproj.Modifier
{
    public class CsprojModifier : Modifier
    {
        /// <summary>
        /// Root path for temp files
        /// </summary>
        static readonly string TEMP_ROOT_PATH = "D:\\Temp\\Slns\\CS_$(SolutionName)\\$(ProjectName)\\";

        /// <summary>
        /// Output path for release configuration
        /// </summary>
        static readonly string OUTPUT_PATH = TEMP_ROOT_PATH + "bin\\$(Configuration)";

        /// <summary>
        /// The top-level folder where all configuration-specific 
        /// intermediate output folders are created.
        /// </summary>
        static readonly string BASE_INTERMEDIATE_OUTPUT_PATH = TEMP_ROOT_PATH + "obj\\";

        /// <summary>
        /// The full intermediate output path as derived from BaseIntermediateOutputPath, 
        /// if no path is specified. For example, \obj\debug\. 
        /// If this property is overridden, then setting BaseIntermediateOutputPath has no effect
        /// </summary>
        static readonly string INTERMEDIATE_OUTPUT_PATH = "$(BaseIntermediateOutputPath)$(Configuration)\\$(Platform)\\";


        public override bool ModifySettings(string file, Options paramOptions)
        {
            XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";
            XElement root = XElement.Load(file);
            bool needSave = false;

            //
            // Set <OutputPath /> <BaseIntermediateOutputPath /> and <IntermediateOutputPath />
            //
            if (!SetOutputPaths(root, ns, ref needSave, 
                                            OUTPUT_PATH,
                                            BASE_INTERMEDIATE_OUTPUT_PATH, 
                                            INTERMEDIATE_OUTPUT_PATH))
            {
                return false;
            }

            if (needSave)
                root.Save(file);

            return true;
        }

        private bool SetOutputPaths(XElement root, XNamespace ns, ref bool needSave, 
                                    string outputPath,
                                    string baseIntermediateOutputPath, 
                                    string intermediateOutputPath)
        {
            try
            {
                var propertyGroups =
                    from el in root.Elements(ns + "PropertyGroup")
                    where (
                        el.Attribute("Condition") != null &&
                        // " '$(Configuration)|$(Platform)' == 'Release|x86' "
                        el.Attribute("Condition").Value.TrimStart(new char[] { ' ' }).StartsWith("'$(Configuration)|$(Platform)'")
                    )
                    select el;

                foreach (var propertyGroup in propertyGroups)
                {
                    #region Modify <OutputPath />

                    XElement elementOutputPath = propertyGroup.Element(ns + "OutputPath");
                    if (elementOutputPath != null)
                    {
                        // Edit <OutputPath />
                        if (elementOutputPath.Value.Equals(outputPath))
                        {
                            // outputPath => D:\Temp\Slns\CS_$(SolutionName)\bin\Release\
                            Console.WriteLine(string.Format("\tSkipping setting <OutputPath />, already existed."));
                        }
                        else
                        {
                            elementOutputPath.Value = outputPath;

                            Console.WriteLine(string.Format("\t<OutputPath /> was updated successfully."));
                            needSave = true;
                        }
                    }
                    else
                    {
                        // Add <OutputPath />
                        elementOutputPath = new XElement(ns + "OutputPath", outputPath);
                        propertyGroup.Add(elementOutputPath);

                        Console.WriteLine(string.Format("\t<OutputPath /> was added successfully."));
                        needSave = true;
                    }

                    #endregion

                    #region Modify <BaseIntermediateOutputPath />

                    XElement elementBaseIntermediateOutputPath = 
                        propertyGroup.Element(ns + "BaseIntermediateOutputPath");
                    if (elementBaseIntermediateOutputPath != null)
                    {
                        // Edit <BaseIntermediateOutputPath />
                        if (elementBaseIntermediateOutputPath.Value.Equals(baseIntermediateOutputPath))
                        {
                            // D:\Temp\Slns\CS_ConsoleApplication1\obj\
                            Console.WriteLine(string.Format(
                                "\tSkipping setting <BaseIntermediateOutputPath />, already existed."));
                        }
                    }
                    else
                    {
                        // Add <BaseIntermediateOutputPath />
                        elementBaseIntermediateOutputPath = 
                            new XElement(ns + "BaseIntermediateOutputPath", baseIntermediateOutputPath);
                        propertyGroup.Add(elementBaseIntermediateOutputPath);

                        Console.WriteLine(string.Format(
                            "\t<BaseIntermediateOutputPath /> was added successfully."));
                        needSave = true;
                    }

                    #endregion

                    #region Modify <IntermediateOutputPath />

                    XElement elementIntermediateOutputPath =
                        propertyGroup.Element(ns + "IntermediateOutputPath");
                    if (elementIntermediateOutputPath != null)
                    {
                        // Edit <IntermediateOutputPath />
                        if (elementIntermediateOutputPath.Value.Equals(intermediateOutputPath))
                        {
                            // D:\Temp\Slns\CS_ConsoleApplication1\obj\
                            Console.WriteLine(string.Format(
                                "\tSkipping setting <IntermediateOutputPath />, already existed."));
                        }
                    }
                    else
                    {
                        // Add <IntermediateOutputPath />
                        elementIntermediateOutputPath =
                            new XElement(ns + "IntermediateOutputPath", intermediateOutputPath);
                        propertyGroup.Add(elementIntermediateOutputPath);

                        Console.WriteLine(string.Format(
                            "\t<IntermediateOutputPath /> was added successfully."));
                        needSave = true;
                    }

                    #endregion
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

using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using StubGenerator.Common.Options;
using System;
using System.Collections.Generic;
using System.IO;

namespace StubGenerator.Common
{
    internal static class Configuration
    {
        // Path of the directory where the files will be generated
        internal static string PathOfDirectoryWhereFileAreGenerated = "";

        // Path of the directory where all DLLs referenced by the assemblies to analyze are located
        internal static string ReferencedAssembliesFolderPath = "";

        private static OutputOptions _outputOptions;

        internal static OutputOptions OutputOptions
        {
            get
            {
                if(_outputOptions == null)
                {
                    _outputOptions = OutputOptions.Default;
                }
                return _outputOptions;
            }
            set
            {
                _outputOptions = value;
            }

        }

        private static bool _isUsingVersion2 = true;
        internal static bool IsUsingVersion2
        {
            get
            {
                return _isUsingVersion2;
            }
            set
            {
                _isUsingVersion2 = value;
                string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                if (value)
                {
                    supportedElementsPath = Path.Combine(currentDirectory, "Resources\\BridgeSupportedElements.xml");
                }
                else
                {
                    supportedElementsPath = Path.Combine(currentDirectory, "Resources\\SupportedElements.xml");
                }
            }
        }

        internal static string SLMigrationCoreAssemblyFolderPath = Path.Combine(
            AnalysisUtils.GetProgramFilesX86Path(),
            @"MSBuild\CSharpXamlForHtml5\InternalStuff\Compiler\SLMigration");

        // Path of supportedElements file
        internal static string supportedElementsPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources\\BridgeSupportedElements.xml");

        // Path of mscorlib assembly
        internal static string mscorlibFolderPath = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\Silverlight\v5.0";

        // Path of SDK folder
        internal static string sdkFolderPath = @"C:\Program Files (x86)\Microsoft SDKs\Silverlight\v5.0\Libraries\Client";

        // Path of folder containing the assemblies we want to analyze
        ////OSMRP1
        internal static string assembliesToAnalyzePath = "";

        // Methods to add manually because mono cecil does not detect them (a method is represented by a Tuple<string, string, string>(string assemblyName, string typeName, string methodName))
        internal static Tuple<string, string, string>[] MethodsToAddManuallyBecauseTheyAreUndetected = new Tuple<string, string, string>[0];

        // Add the option to add code directly into a specified type
        internal static Dictionary<string, Dictionary<string, HashSet<string>>> CodeToAddManuallyBecauseItIsUndetected = new Dictionary<string, Dictionary<string, HashSet<string>>>();

        internal static HashSet<string> ExcludedFiles = new HashSet<string>();

        // Types that do not have the same name in their original implementation and our implementation.
        internal static Dictionary<string, string> TypesThatNeedToBeRenamed = new Dictionary<string, string>()
        {
            { "System.ServiceModel.ClientBase`1", "CSHTML5_ClientBase`1" },
        };

        internal static string[] TypesThatNeedFullName = new string[]
        {
            "System.Windows.PropertyMetadata",
            "Telerik.Windows.PropertyMetadata",
            "System.Windows.Controls.ItemsControl",
            "Telerik.Windows.Controls.ItemsControl",
            "System.Windows.Controls.SelectionMode",
            "Telerik.Windows.Controls.SelectionMode",
        };

        internal static readonly HashSet<string> UrlNamespacesThatBelongToUserCode = new HashSet<string>();

        internal static readonly HashSet<string> AttributesToIgnoreInXamlBecauseTheyAreFromBaseClasses = new HashSet<string>();
    }
}

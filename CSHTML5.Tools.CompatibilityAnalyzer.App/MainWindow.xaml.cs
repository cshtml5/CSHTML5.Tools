#define ALWAYS_AGGREGATE_METHODS
//#define ASK_USER_TO_CHOOSE_ASSEMBLIES_TO_ANALYZE
//#define SAVE_CSV_DOCUMENT

using Mono.Cecil;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CSHTML5.Tools.AssemblyAnalysisCommon.Analyzer;
using CSHTML5.Tools.AssemblyAnalysisCommon.Analyzer.AssemblyReaderParameters;
using CSHTML5.Tools.CompatibilityAnalyzer.App.Properties;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CoreSupportedMethodsContainer _coreSupportedMethods = null;

        public MainWindow()
        {
            InitializeComponent();

#if SAVE_CSV_DOCUMENT
            // Initialize the output CSV file path:
            string csvFileName = "SL Migration Analysis - "
                    + DateTime.Now.Year.ToString() + "-"
                    + DateTime.Now.Month.ToString() + "-"
                    + DateTime.Now.Day.ToString() + " "
                    + DateTime.Now.Hour.ToString() + "-"
                    + DateTime.Now.Minute.ToString()
                    + ".csv";
            string outputCsvFolderPath = Configuration.GeneratedFilesFolderPath;
            if (!string.IsNullOrEmpty(outputCsvFolderPath))
            {
                if (!Directory.Exists(outputCsvFolderPath))
                {
                    outputCsvFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
            }
            else
            {
                outputCsvFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            OutputCsvFilePathTextBox.Text = System.IO.Path.Combine(outputCsvFolderPath, csvFileName);
#endif

            // Initialize the output Excel file path:
            ResetOutputExcelFilePath();

            // Initialize the SL Migration Core assembly folder path field:
            if (string.IsNullOrEmpty(Settings.Default.CoreAssemblyPath))
                ResetCoreAssemblyPath();
            else
                CoreAssemblyPathTextBox.Text = Settings.Default.CoreAssemblyPath;

            // Initialize the path to the "Features and Estimations.xlsx" file:
            if (string.IsNullOrEmpty(Settings.Default.FeaturesAndEstimationsPath))
                ResetFeaturesAndEstimationsPath();
            else
                FeaturesAndEstimationsPathTextBox.Text = Settings.Default.FeaturesAndEstimationsPath;

            // Initialize the path that contains the Silverlight "Mscorlib.dll" file:
            if (string.IsNullOrEmpty(Settings.Default.MscorlibFolderPath))
                ResetMscorlibFolderPath();
            else
                MscorlibFolderPathTextBox.Text = Settings.Default.MscorlibFolderPath;

            // Initialize the path that contains the Silverlight SDK:
            if (string.IsNullOrEmpty(Settings.Default.SDKFolderPath))
                ResetSDKFolderPath();
            else
                SDKFolderPathTextBox.Text = Settings.Default.SDKFolderPath;

            // Reload the previous value of the "other paths" field:
            if (string.IsNullOrEmpty(Settings.Default.OtherFoldersPath))
                ResetOtherFoldersPath();
            else
                OtherFoldersPathTextBox.Text = Settings.Default.OtherFoldersPath;

            // Supported elements path:
            if (string.IsNullOrEmpty(Settings.Default.SupportedElementsPath))
                ResetSupportedElementsPath();
            else
                SupportedElementsPathTextBox.Text = Settings.Default.SupportedElementsPath;

            // XAML files to ignore:
            if (string.IsNullOrEmpty(Settings.Default.XamlFilesToIgnore))
                ResetXamlFilesToIgnore();
            else
                XamlFilesToIgnoreTextBox.Text = Settings.Default.XamlFilesToIgnore;
        }

        private void ButtonResetValues_Click(object sender, RoutedEventArgs e)
        {
            ResetOutputExcelFilePath();
            ResetCoreAssemblyPath();
            ResetFeaturesAndEstimationsPath();
            ResetMscorlibFolderPath();
            ResetMscorlibFolderPath();
            ResetSDKFolderPath();
            ResetOtherFoldersPath();
            ResetSupportedElementsPath();
            ResetXamlFilesToIgnore();

            SaveContentOfTextBoxes();
        }

        void SaveContentOfTextBoxes()
        {
            Settings.Default.CoreAssemblyPath = CoreAssemblyPathTextBox.Text;
            Settings.Default.FeaturesAndEstimationsPath = FeaturesAndEstimationsPathTextBox.Text;
            Settings.Default.MscorlibFolderPath = MscorlibFolderPathTextBox.Text;
            Settings.Default.SDKFolderPath = SDKFolderPathTextBox.Text;
            Settings.Default.OtherFoldersPath = OtherFoldersPathTextBox.Text;
            Settings.Default.SupportedElementsPath = SupportedElementsPathTextBox.Text;
            Settings.Default.XamlFilesToIgnore = XamlFilesToIgnoreTextBox.Text;

            Settings.Default.Save();
        }

        void ResetOutputExcelFilePath()
        {
            string excelFileName = "SL Migration Analysis - "
                   + DateTime.Now.Year.ToString() + "-"
                   + DateTime.Now.Month.ToString() + "-"
                   + DateTime.Now.Day.ToString() + " "
                   + DateTime.Now.Hour.ToString() + "-"
                   + DateTime.Now.Minute.ToString()
                   + ".xlsx";
            string outputExcelFolderPath = Configuration.GeneratedFilesFolderPath;
            if (!string.IsNullOrEmpty(outputExcelFolderPath))
            {
                if (!Directory.Exists(outputExcelFolderPath))
                {
                    outputExcelFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                }
            }
            else
            {
                outputExcelFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }
            OutputExcelFilePathTextBox.Text = System.IO.Path.Combine(outputExcelFolderPath, excelFileName);
        }

        void ResetCoreAssemblyPath()
        {
            CoreAssemblyPathTextBox.Text = System.IO.Path.Combine(GetProgramFilesX86Path(), @"MSBuild\CSharpXamlForHtml5\InternalStuff\Compiler\SLMigration");
        }

        void ResetFeaturesAndEstimationsPath()
        {
            FeaturesAndEstimationsPathTextBox.Text = Configuration.DefaultPathToFeaturesAndEstimationsFile;
        }

        void ResetMscorlibFolderPath()
        {
            MscorlibFolderPathTextBox.Text = System.IO.Path.Combine(GetProgramFilesX86Path(), @"Reference Assemblies\Microsoft\Framework\Silverlight\v5.0\");
        }

        void ResetSDKFolderPath()
        {
            SDKFolderPathTextBox.Text = System.IO.Path.Combine(GetProgramFilesX86Path(), @"Microsoft SDKs\Silverlight\v5.0\Libraries\Client");
        }

        void ResetOtherFoldersPath()
        {
            OtherFoldersPathTextBox.Text = "";
        }

        void ResetSupportedElementsPath()
        {
            try
            {
                var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
                //string rootDotNetForHtml5Path = currentDirectory.Parent.Parent.Parent.FullName;
                string path = System.IO.Path.Combine(currentDirectory.FullName, @"Resources\BridgeSupportedElements.xml");
                SupportedElementsPathTextBox.Text = path;
            }
            catch
            {
                SupportedElementsPathTextBox.Text = "";
            }
        }

        void ResetXamlFilesToIgnore()
        {
            XamlFilesToIgnoreTextBox.Text = @"*\System.Windows.xaml, *\TelerikStyleOverride.xaml, *\Telerik.Windows.Controls.xaml, *\Telerik.Windows.Controls.Data.xaml, *\Telerik.Windows.Controls.Input.xaml, *\Telerik.Windows.Controls.Navigation.xaml, *\Telerik.Windows.Controls.DataVisualization.xaml, *\Telerik.Windows.Controls.Chart.xaml, *\Telerik.Windows.Controls.Diagrams.xaml, *\Telerik.Windows.Controls.Diagrams.Extensions.xaml, *\Telerik.Windows.Controls.Docking.xaml, *\Telerik.Windows.Controls.Expressions.xaml, *\Telerik.Windows.Controls.GanttView.xaml, *\Telerik.Windows.Controls.GridView.xaml, *\Telerik.Windows.Controls.ImageEditor.xaml, *\Telerik.Windows.Controls.RibbonView.xaml, *\Telerik.Windows.Controls.RichTextBoxUI.xaml, *\Telerik.Windows.Controls.ScheduleView.xaml, *\Telerik.Windows.Controls.Spreadsheet.xaml, *\Telerik.Windows.Documents.xaml, *\Telerik.Windows.Documents.Proofing.xaml";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string coreAssemblyPath = CoreAssemblyPathTextBox.Text;
            string supportedElementsPath = SupportedElementsPathTextBox.Text;
            string mscorlibFolderPath = MscorlibFolderPathTextBox.Text;
            string sdkFolderPath = SDKFolderPathTextBox.Text;
            string otherFoldersPath = OtherFoldersPathTextBox.Text;

            HashSet<string> xamlFilesToIgnore = null;
            try
            {
                xamlFilesToIgnore = new HashSet<string>(XamlFilesToIgnoreTextBox.Text.Split(',').Select(s => s.Trim().ToLower()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid list of XAML files to exclude." + Environment.NewLine + Environment.NewLine + ex.ToString());
                return;
            }

            if (string.IsNullOrEmpty(coreAssemblyPath)
                || string.IsNullOrEmpty(supportedElementsPath)
                || string.IsNullOrEmpty(mscorlibFolderPath))
            {
                MessageBox.Show("Please fill all the fields before continuing.");
                return;
            }

            if (!File.Exists(supportedElementsPath))
            {
                MessageBox.Show("File not found: " + supportedElementsPath);
                return;
            }

            var featuresAndEstimationsFileProcessor = new FeaturesAndEstimationsFileProcessor(FeaturesAndEstimationsPathTextBox.Text);

            // Save the content of the text boxes for reuse when relaunching the application:
            SaveContentOfTextBoxes();

#if ASK_USER_TO_CHOOSE_ASSEMBLIES_TO_ANALYZE
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".dll";
            dlg.Filter = "Assemblies and executables (*.dll,*.exe)|*.dll;*.exe";
            dlg.Multiselect = true;

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                string[] fileNames = dlg.FileNames;
#else
            string[] fileNames = Configuration.AssembliesToAnalyze;

            if (true)
            {
#endif

                if (_coreSupportedMethods == null)
                {
                    _coreSupportedMethods = new CoreSupportedMethodsContainer(coreAssemblyPath);
                }

                var logger = new LoggerThatAggregatesAllErrors();
                var watch = Stopwatch.StartNew();
                var listOfUnsupportedMethods = new List<UnsupportedMethodInfo>();

                // Get the path of the folder where the first assembly is located, and add that path to the list of places where to look in for resolving referenced assemblies:
                string firstFileToAnalyze = fileNames.FirstOrDefault();
                string additionalFolderWhereToResolveAssemblies = null;
                if (firstFileToAnalyze != null)
                {
                    additionalFolderWhereToResolveAssemblies = System.IO.Path.GetDirectoryName(firstFileToAnalyze);
                }

                // Do the analysis:
                try
                {
                    IReaderParametersFactory readerParametersFactory = new SilverlightReaderParametersFactory(mscorlibFolderPath,
                        sdkFolderPath, otherFoldersPath, additionalFolderWhereToResolveAssemblies);

                    readerParametersFactory = new WpfReaderParametersFactory();

                    foreach (var filename in fileNames)
                    {
                        CompatibilityAnalyzer.Analyze(
                            filename,
                            logger,
                            listOfUnsupportedMethods,
                            _coreSupportedMethods,
                            fileNames,
                            Configuration.UrlNamespacesThatBelongToUserCode,
                            Configuration.AttributesToIgnoreInXamlBecauseTheyAreFromBaseClasses,
                            xamlFilesToIgnore,
                            supportedElementsPath,
                            true,
                            readerParametersFactory
                            );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }

                // Replace all telerik assembly names with "http://schemas.telerik.com/2008/xaml/presentation" so as to avoid differences between namespaces from .xaml files and namespaces from .cs files:
                foreach (UnsupportedMethodInfo unsupportedMethodInfo in listOfUnsupportedMethods)
                {
                    if (unsupportedMethodInfo.MethodAssemblyName.StartsWith("Telerik."))
                    {
                        unsupportedMethodInfo.MethodAssemblyName = "http://schemas.telerik.com/2008/xaml/presentation";
                    }
                }

                // Replace all Expression Blend-related namespaces with "http://schemas.microsoft.com/expression/2010/..." so as to avoid differences between namespaces from .xaml files and namespaces from .cs files:
                foreach (UnsupportedMethodInfo unsupportedMethodInfo in listOfUnsupportedMethods)
                {
                    if (unsupportedMethodInfo.MethodAssemblyName.StartsWith("Microsoft.Expression.")
                        || unsupportedMethodInfo.MethodAssemblyName.StartsWith("System.Windows.Interactivity")
                        || unsupportedMethodInfo.MethodAssemblyName == "http://schemas.microsoft.com/expression/2010/drawing"
                        || unsupportedMethodInfo.MethodAssemblyName == "http://schemas.microsoft.com/expression/2010/interactivity"
                        || unsupportedMethodInfo.MethodAssemblyName == "http://schemas.microsoft.com/expression/2010/interactions"
                        )
                    {
                        unsupportedMethodInfo.MethodAssemblyName = "http://schemas.microsoft.com/expression/2010/...";
                    }
                }

                // Aggregate all MEF-related namespaces:
                foreach (UnsupportedMethodInfo unsupportedMethodInfo in listOfUnsupportedMethods)
                {
                    if (unsupportedMethodInfo.MethodAssemblyName.StartsWith("System.ComponentModel.Composition."))
                    {
                        unsupportedMethodInfo.MethodAssemblyName = "System.ComponentModel.Composition";
                    }
                }

                // Aggregate all DomainServices-related namespaces:
                foreach (UnsupportedMethodInfo unsupportedMethodInfo in listOfUnsupportedMethods)
                {
                    if (unsupportedMethodInfo.MethodAssemblyName.StartsWith("System.ServiceModel.DomainServices.Client."))
                    {
                        unsupportedMethodInfo.MethodAssemblyName = "System.ServiceModel.DomainServices.Client";
                    }
                }

                // Replace all the native MS UI-related assembly names with "http://schemas.microsoft.com/winfx/2006/xaml/presentation" so as to avoid differences between namespaces from .xaml files and namespaces from .cs files:
                foreach (UnsupportedMethodInfo unsupportedMethodInfo in listOfUnsupportedMethods)
                {
                    if (unsupportedMethodInfo.MethodAssemblyName == "System.Windows"
                        || unsupportedMethodInfo.MethodAssemblyName == "System.Windows.Controls"
                        || unsupportedMethodInfo.MethodAssemblyName == "System.Windows.Controls.Data"
                        || unsupportedMethodInfo.MethodAssemblyName == "System.Windows.Browser"
                        || unsupportedMethodInfo.MethodAssemblyName == "System.Windows.Controls.Navigation"
                        || unsupportedMethodInfo.MethodAssemblyName == "System.Windows.Controls.Toolkit")
                    {
                        unsupportedMethodInfo.MethodAssemblyName = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
                    }
                }

                // Process the list of unsupported methods:
                SortedDictionary<Tuple<string, string>, HashSet<string>> unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed;
                ProcessUnsupportedMethods(listOfUnsupportedMethods, out unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed, rectifyMethodName: true);

                // Remove some stuff:
                foreach (Tuple<string, string> additionalEntryToRemove in Configuration.AdditionalEntriesToRemoveBeforeAggregation)
                {
                    unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.Remove(additionalEntryToRemove);
                }

                // For classes that are not defined at all (ie. missing constructor), aggregate all the methods into the same line:
                List<Tuple<string, string>> entriesToRemoveFromDictionary = new List<Tuple<string, string>>(); // This is here because we cannot remove the keys while iterating in the dictionary, so we do it afterwards.
                List<KeyValuePair<Tuple<string, string>, HashSet<string>>> entriesToAddToDictionary = new List<KeyValuePair<Tuple<string, string>, HashSet<string>>>(); // This is here because we cannot add entries while iterating in the dictionary, so we do it afterwards.
                HashSet<Tuple<string, string>> classesForWhichTheMethodsHaveAlreadyBeenAggregated = new HashSet<Tuple<string, string>>();
                foreach (KeyValuePair<Tuple<string, string>, HashSet<string>> pair in unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed)
                {
                    string theFullMethodName = pair.Key.Item1;
                    string theAssemblyName = pair.Key.Item2;
                    HashSet<string> whereItIsUsed = pair.Value;

                    if (!Configuration.EntriesToNotAggregateWithOtherEntries.Contains(theFullMethodName))
                    {
#if ALWAYS_AGGREGATE_METHODS
                        if (theFullMethodName.Contains("."))
#else
                        if (theFullMethodName.Contains("..ctor"))
#endif
                        {
#if ALWAYS_AGGREGATE_METHODS
                            string className = theFullMethodName.Substring(0, theFullMethodName.IndexOf("."));
#else
                            string className = theFullMethodName.Substring(0, theFullMethodName.IndexOf("..ctor"));
#endif
                            if (!classesForWhichTheMethodsHaveAlreadyBeenAggregated.Contains(new Tuple<string, string>(className, theAssemblyName)))
                            {
                                classesForWhichTheMethodsHaveAlreadyBeenAggregated.Add(new Tuple<string, string>(className, theAssemblyName));
                                List<string> methodNames = new List<string>();

                                // Find all other methods that concern the same class:
                                foreach (KeyValuePair<Tuple<string, string>, HashSet<string>> pair2 in unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed)
                                {
                                    //todo-perf: optimize performance by creating a dictionary beforehand instead of nesting two iterations!

                                    string theFullMethodName2 = pair2.Key.Item1;
                                    string theAssemblyName2 = pair2.Key.Item2;
                                    HashSet<string> whereItIsUsed2 = pair.Value;
                                    if (!Configuration.EntriesToNotAggregateWithOtherEntries.Contains(theFullMethodName2))
                                    {
                                        if (theFullMethodName2.Contains('.')
                                        && !theFullMethodName2.Contains("..ctor")
                                        )
                                        {
                                            string className2 = theFullMethodName2.Substring(0, theFullMethodName2.IndexOf('.'));
                                            if (className2 == className && theAssemblyName2 == theAssemblyName)
                                            {
                                                string methodName2 = theFullMethodName2.Substring(theFullMethodName2.IndexOf('.') + 1);
                                                methodNames.Add(methodName2);
                                                HashSetHelpers.AddItemsFromOneHashSetToAnother(whereItIsUsed2, whereItIsUsed);
                                                entriesToRemoveFromDictionary.Add(pair2.Key);
                                            }
                                        }
                                    }
                                }

                                // Aggregate them:
                                string newMethodName;
                                if (methodNames.Count > 0)
                                {
                                    if (theFullMethodName.Contains("..ctor"))
                                    {
                                        newMethodName = className + " (members used: " + string.Join(", ", methodNames) + ")";
                                    }
                                    else
                                    {
                                        newMethodName = className + "." + string.Join(", .", methodNames);
                                    }
                                }
                                else
                                {
                                    newMethodName = className;
                                }

                                entriesToRemoveFromDictionary.Add(pair.Key);
                                entriesToAddToDictionary.Add(new KeyValuePair<Tuple<string, string>, HashSet<string>>(
                                    new Tuple<string, string>(newMethodName, theAssemblyName),
                                    whereItIsUsed));
                            }
                        }
                    }
                }
                // Perform the actual removal and addition (note: this cannot be done before because it is not possible to remove or add an item from a collection while iterating it with "foreach");
                foreach (var key in entriesToRemoveFromDictionary)
                {
                    unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.Remove(key);
                }
                foreach (var entry in entriesToAddToDictionary)
                {
                    unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.Add(entry.Key, entry.Value);
                }

                //-------------------------------------
                // Merge classes that are related to each other:
                //-------------------------------------
                MergingRelatedClasses.MergeRelatedClasses(Configuration.ClassesRelatedToEachOther, unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed);

                //-------------------------------------
                // Remove elements with an empty assembly name because they are due to "clr-namespace:..." without any assembly being specified, which means that the assembly is the user assembly itself, so we should remove the entry:
                //-------------------------------------
                entriesToRemoveFromDictionary = new List<Tuple<string, string>>();
                foreach (var entry in unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed)
                {
                    if (entry.Key.Item2 == "")
                        entriesToRemoveFromDictionary.Add(entry.Key);
                }
                foreach (var key in entriesToRemoveFromDictionary)
                {
                    unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.Remove(key);
                }

                //-------------------------------------
                // Remove some additional stuff:
                //-------------------------------------

                foreach (Tuple<string, string> additionalEntryToRemove in Configuration.AdditionalEntriesToRemoveAfterAggregation)
                {
                    unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.Remove(additionalEntryToRemove);
                }

                //-------------------------------------
                // Save the result:
                //-------------------------------------

                // Save as Excel document:
                ExcelGenerator.Generate(
                    OutputExcelFilePathTextBox.Text,
                    featuresAndEstimationsFileProcessor,
                    unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed,
                    fileNames.Select(fileName => System.IO.Path.GetFileName(fileName)));

#if SAVE_CSV_DOCUMENT
                // Save as CSV document:
                CsvGenerator.Generate(OutputCsvFilePathTextBox.Text, unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed);
#endif

                var elapsedMs = watch.ElapsedMilliseconds;
                MessageBox.Show(string.Format("Operation completed in {0} seconds.", Math.Floor(elapsedMs / 1000d).ToString()));

                //-------------------------------------
                // Open the result:
                //-------------------------------------

                Process.Start(OutputExcelFilePathTextBox.Text);

                //-------------------------------------
                // Close this app:
                //-------------------------------------

                System.Windows.Application.Current.Shutdown();
            }
        }

        private static void AddItemsFromOneHashSetToAnother(HashSet<string> source, HashSet<string> destination)
        {
            foreach (string str in source)
            {
                if (!destination.Contains(str))
                {
                    destination.Add(str);
                }
            }
        }

        private static void ProcessUnsupportedMethods(List<UnsupportedMethodInfo> listOfUnsupportedMethods, out SortedDictionary<Tuple<string, string>, HashSet<string>> unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed, bool rectifyMethodName)
        {
            unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed = new SortedDictionary<Tuple<string, string>, HashSet<string>>();
            foreach (var unsupportedMethod in listOfUnsupportedMethods)
            {
                string fullMethodName = unsupportedMethod.TypeName + "." + unsupportedMethod.MethodName;
                string methodAssemblyName = unsupportedMethod.MethodAssemblyName;
                if (rectifyMethodName)
                {
                    if (fullMethodName.Contains(".get_"))
                    {
                        fullMethodName = fullMethodName.Replace(".get_", ".");
                    }
                    else if (fullMethodName.Contains(".set_"))
                    {
                        fullMethodName = fullMethodName.Replace(".set_", ".");
                    }
                    else if (fullMethodName.Contains(".add_"))
                    {
                        fullMethodName = fullMethodName.Replace(".add_", ".");
                    }
                    else if (fullMethodName.Contains(".remove_"))
                    {
                        fullMethodName = fullMethodName.Replace(".remove_", ".");
                    }
                    else if (fullMethodName.Contains(".op_"))
                    {
                        int index = fullMethodName.IndexOf(".op_");
                        string operatorName = fullMethodName.Substring(index + 4);
                        string typeName = fullMethodName.Substring(0, index);
                        fullMethodName = string.Format(@"{0} '{1}' operator", typeName, operatorName);
                    }
                    else
                    {
                        fullMethodName = fullMethodName + "(...)";
                    }
                    fullMethodName = fullMethodName
                        .Replace("`1", "<T>")
                        .Replace("`2", "<T1,T2>")
                        .Replace("`3", "<T1,T2,T3>")
                        .Replace("`4", "<T1,T2,T3,T4>")
                        .Replace("`5", "<T1,T2,T3,T4,T5>")
                        .Replace("`6", "<T1,T2,T3,T4,T5,T6>")
                        .Replace("`7", "<T1,T2,T3,T4,T5,T6,T7>")
                        .Replace("`8", "<T1,T2,T3,T4,T5,T6,T7,T8>");
                }
                HashSet<string> locationsWhereTheMethodIsUsed;
                var key = new Tuple<string, string>(fullMethodName, methodAssemblyName);
                if (unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.ContainsKey(key))
                {
                    locationsWhereTheMethodIsUsed = unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed[key];
                }
                else
                {
                    locationsWhereTheMethodIsUsed = new HashSet<string>();
                    unsupportedMethodsAndTheirAssemblyToLocationsWhereTheyAreUsed.Add(key, locationsWhereTheMethodIsUsed);
                }

                string className = unsupportedMethod.CallingMethodFullName;
#if FULL_PATH_OUTPUT
                    string ext = ".cs";
#else
                string ext = "";
#endif
                if (className.ToLowerInvariant().EndsWith(".xaml"))
                {
                    ext = ".xaml";
                    className = className.Substring(className.LastIndexOf('/') + 1); // Removes the path of the XAML file.
                }
                className = className.Replace("`1", "").Replace("`2", "").Replace("`3", "").Replace("`4", "").Replace("`5", "").Replace("`6", "");
                if (className.Contains("."))
                    className = className.Substring(0, className.LastIndexOf("."));

#if FULL_PATH_OUTPUT
                    string location = unsupportedMethod.UserAssemblyName + "/.../" + className + ext;
#else
                string location = className + ext;
#endif
                if (!locationsWhereTheMethodIsUsed.Contains(location))
                    locationsWhereTheMethodIsUsed.Add(location);
            }
        }


        static string GetProgramFilesX86Path()
        {
            // Credits: http://stackoverflow.com/questions/194157/c-sharp-how-to-get-program-files-x86-on-windows-vista-64-bit

            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }
    }
}

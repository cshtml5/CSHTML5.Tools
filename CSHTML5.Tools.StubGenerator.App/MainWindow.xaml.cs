using StubGenerator.Common.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace DotNetForHtml5.PrivateTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonGeneratedFilesFolderClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFileDialog = new FolderBrowserDialog();
            if(openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GeneratedFilesFolderPath.Text = openFileDialog.SelectedPath;
            }
        }

        private void ButtonReferencedAssembliesFolderClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFileDialog = new FolderBrowserDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ReferencedAssembliesFolderPath.Text = openFileDialog.SelectedPath;
            }
        }

        private void ButtonAssembliesToAnalyzeFolderClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFileDialog = new FolderBrowserDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AssembliesToAnalyzeFolderPath.Text = openFileDialog.SelectedPath;
            }
        }

        private void ButtonMscorlibFolderClick(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog openFileDialog = new FolderBrowserDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MscorlibFolderPath.Text = openFileDialog.SelectedPath;
            }
        }

        private void ButtonUndetectedMethodXMLFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                UndetectedMethodXMLFilePath.Text = openFileDialog.FileName;
            }
        }

        private void ButtonAdditionnalCodeXMLFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                AdditionnalCodeXMLFilePath.Text = openFileDialog.FileName;
            }
        }

        private void ButtonIgnoredFilesXMLFileClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                IgnoredFilesXMLFilePath.Text = openFileDialog.FileName;
            }
        }

        private OutputOptions _options;
        private OutputOptions Options
        {
            get
            {
                if(_options == null)
                {
                    _options = OutputOptions.Default;
                }
                return _options;
            }
            set
            {
                _options = value;
            }
        }
        private void ConfigureOptionsButtonClick(object sender, RoutedEventArgs e)
        {
            var window = new OptionsPicker();
            if(window.ShowDialog() == true)
            {
                Options = window.Options;
            }
        }

        private Tuple<string, string, string>[] GetUndetectedMethods(string xmlFilePath)
        {
            List<Tuple<string, string, string>> res = new List<Tuple<string, string, string>>();
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlFilePath);
            }
            catch(Exception e)
            {
                return res.ToArray();
            }
            var root = xmlDoc.FirstChild;
            foreach(var assembly in root.ChildNodes.OfType<XmlNode>())
            {
                if(assembly.LocalName != "#comment")
                {
                    string assemblyName = assembly.Attributes["Name"].Value;
                    foreach (var type in assembly.ChildNodes.OfType<XmlNode>())
                    {
                        if (type.LocalName != "#comment")
                        {
                            string typeName = type.Attributes["Name"].Value;
                            foreach (var method in type.ChildNodes.OfType<XmlNode>())
                            {
                                if (method.LocalName != "#comment")
                                {
                                    res.Add(new Tuple<string, string, string>(assemblyName, typeName, method.Attributes["Name"].Value));
                                }
                            }
                        }
                    }
                }
            }
            return res.ToArray();
        }

        private Dictionary<string, Dictionary<string, HashSet<string>>> GetAdditionnalCode(string xmlFilePath)
        {
            Dictionary<string, Dictionary<string, HashSet<string>>> res = new Dictionary<string, Dictionary<string, HashSet<string>>>();
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlFilePath);
            }
            catch (Exception e)
            {
                return res;
            }
            var root = xmlDoc.FirstChild;
            foreach (var assembly in root.ChildNodes.OfType<XmlNode>())
            {
                if(assembly.LocalName != "#comment")
                {
                    string assemblyName = assembly.Attributes["Name"].Value;
                    Dictionary<string, HashSet<string>> types = new Dictionary<string, HashSet<string>>();
                    foreach (var type in assembly.ChildNodes.OfType<XmlNode>())
                    {
                        if(type.LocalName != "#comment")
                        {
                            string typeName = type.Attributes["Name"].Value;
                            HashSet<string> codeLines = new HashSet<string>();
                            foreach (var codeBlock in type.ChildNodes.OfType<XmlNode>())
                            {
                                if(codeBlock.LocalName != "#comment")
                                {
                                    string codeLine = codeBlock.Attributes["Content"].Value;
                                    codeLines.Add(codeLine);
                                }
                            }
                            if (codeLines.Count > 0)
                            {
                                types.Add(typeName, codeLines);
                            }
                        }
                    }
                    if (types.Count > 0)
                    {
                        res.Add(assemblyName, types);
                    }
                }
            }
            return res;
        }

        private HashSet<string> GetIgnoredFiles(string xmlFilePath)
        {
            HashSet<string> res = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(xmlFilePath);
            }
            catch (Exception e)
            {
                return res;
            }
            var root = xmlDoc.FirstChild;
            foreach (var file in root.ChildNodes.OfType<XmlNode>())
            {
                if (file.LocalName != "#comment")
                {
                    string filePath = file.Attributes["Path"].Value;
                    res.Add(filePath);
                }
            }
            return res;
        }

        private async Task Start()
        {
            PleaseWaitContainer.Visibility = Visibility.Visible;
            await Task.Delay(100); // This gives the "Please wait" message the time to display itself.
            StubGenerator.Common.Configuration.OutputOptions = Options;
            StubGenerator.Common.Configuration.MethodsToAddManuallyBecauseTheyAreUndetected = GetUndetectedMethods(UndetectedMethodXMLFilePath.Text);
            StubGenerator.Common.Configuration.CodeToAddManuallyBecauseItIsUndetected = GetAdditionnalCode(AdditionnalCodeXMLFilePath.Text);
            StubGenerator.Common.Configuration.ExcludedFiles = GetIgnoredFiles(IgnoredFilesXMLFilePath.Text);
            StubGenerator.Common.Configuration.assembliesToAnalyzePath = AssembliesToAnalyzeFolderPath.Text;
            StubGenerator.Common.Configuration.mscorlibFolderPath = MscorlibFolderPath.Text;
            StubGenerator.Common.Configuration.ReferencedAssembliesFolderPath = ReferencedAssembliesFolderPath.Text;
            StubGenerator.Common.Configuration.PathOfDirectoryWhereFileAreGenerated = GeneratedFilesFolderPath.Text;
            StubGenerator.Common.Configuration.IsUsingVersion2 = CSHTML5Version.SelectedIndex == 0;
            StubGenerator.Common.StubGenerator stubGenerator = new StubGenerator.Common.StubGenerator();
            stubGenerator.Run();
            PleaseWaitContainer.Visibility = Visibility.Collapsed;
            await Task.Delay(100); // This gives the "Please wait" message the time to close.
        }

        private async void StartButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await Start();
                System.Windows.MessageBox.Show("Success.");
            }
            catch (Exception ex)
            {
                PleaseWaitContainer.Visibility = Visibility.Collapsed;
                System.Windows.MessageBox.Show("Something went wrong. Please verify your configuration and try again." + Environment.NewLine + Environment.NewLine + ex.ToString());
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}

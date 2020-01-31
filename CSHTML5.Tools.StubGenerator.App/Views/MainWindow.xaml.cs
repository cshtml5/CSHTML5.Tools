using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using DotNetForHtml5.PrivateTools.AssemblyAnalysisCommon;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using StubGenerator.Common.Options;
using Configuration = StubGenerator.Common.Configuration;

namespace DotNetForHtml5.PrivateTools
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		// Objects used for caching
		private readonly System.Configuration.Configuration _config;
		private readonly KeyValueConfigurationCollection _settings;
		
		public MainWindow()
		{
			InitializeComponent();
			
			// Load cached stuff
			_config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			_settings = _config.AppSettings.Settings;

			GeneratedFilesFolderPath.Text = _settings["GeneratedFilesFolderPath"].Value;
			ReferencedAssembliesFolderPath.Text = _settings["ReferencedAssembliesFolderPath"].Value;
			AssembliesToAnalyzeFolderPath.Text = _settings["AssembliesToAnalyzeFolderPath"].Value;
			try { SelectedProduct.SelectedIndex = int.Parse(_settings["ProductIndex"].Value); }
			catch (FormatException) {}
		}

		private static bool OpenFolderPicker(out string result)
		{
			CommonOpenFileDialog dialog = new CommonOpenFileDialog
			{
				IsFolderPicker = true,
				InitialDirectory = Directory.GetCurrentDirectory()
			};

			result = null;

			if (dialog.ShowDialog() != CommonFileDialogResult.Ok) return false;

			result = dialog.FileName;
			return true;
		}

		private static bool OpenFilePicker(out string result)
		{
			OpenFileDialog dialog = new OpenFileDialog
			{
				InitialDirectory = Directory.GetCurrentDirectory()
			};

			result = null;

			if (dialog.ShowDialog() == false) return false;

			result = dialog.FileName;
			return true;
		}

		private void ButtonGeneratedFilesFolderClick(object sender, RoutedEventArgs e)
		{
			if (OpenFolderPicker(out string path))
				GeneratedFilesFolderPath.Text = path;
		}

		private void ButtonReferencedAssembliesFolderClick(object sender, RoutedEventArgs e)
		{
			if (OpenFolderPicker(out string path))
				ReferencedAssembliesFolderPath.Text = path;
		}

		private void ButtonAssembliesToAnalyzeFolderClick(object sender, RoutedEventArgs e)
		{
			if (OpenFolderPicker(out string path))
				AssembliesToAnalyzeFolderPath.Text = path;
		}

		private void ButtonMscorlibFolderClick(object sender, RoutedEventArgs e)
		{
			if (OpenFolderPicker(out string path))
				MscorlibFolderPath.Text = path;
		}

		private void ButtonUndetectedMethodXMLFileClick(object sender, RoutedEventArgs e)
		{
			if (OpenFilePicker(out string path))
				UndetectedMethodXMLFilePath.Text = path;
		}

		private void ButtonAdditionnalCodeXMLFileClick(object sender, RoutedEventArgs e)
		{
			if (OpenFilePicker(out string path))
				UndetectedMethodXMLFilePath.Text = path;
		}

		private void ButtonIgnoredFilesXMLFileClick(object sender, RoutedEventArgs e)
		{
			if (OpenFilePicker(out string path))
				UndetectedMethodXMLFilePath.Text = path;
		}

		private OutputOptions _options;

		private OutputOptions Options
		{
			get => _options ?? (_options = OutputOptions.Default);
			set => _options = value;
		}

		private void ConfigureOptionsButtonClick(object sender, RoutedEventArgs e)
		{
			OptionsPicker window = new OptionsPicker();
			if (window.ShowDialog() == true)
			{
				Options = window.Options;
			}
		}

		private static Tuple<string, string, string>[] GetUndetectedMethods(string xmlFilePath)
		{
			List<Tuple<string, string, string>> res = new List<Tuple<string, string, string>>();
			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				xmlDoc.Load(xmlFilePath);
			}
			catch (Exception)
			{
				return res.ToArray();
			}

			XmlNode root = xmlDoc.FirstChild;
			foreach (XmlNode assembly in root.ChildNodes.OfType<XmlNode>())
			{
				if (assembly.LocalName == "#comment") continue;
				
				string assemblyName = assembly.Attributes["Name"].Value;
				foreach (XmlNode type in assembly.ChildNodes.OfType<XmlNode>())
				{
					if (type.LocalName == "#comment") continue;
					
					string typeName = type.Attributes["Name"].Value;
					foreach (XmlNode method in type.ChildNodes.OfType<XmlNode>())
					{
						if (method.LocalName == "#comment") continue;
						
						res.Add(new Tuple<string, string, string>(assemblyName, typeName, method.Attributes["Name"].Value));
					}
				}
			}

			return res.ToArray();
		}

		private static Dictionary<string, Dictionary<string, HashSet<string>>> GetAdditionnalCode(string xmlFilePath)
		{
			Dictionary<string, Dictionary<string, HashSet<string>>> res = new Dictionary<string, Dictionary<string, HashSet<string>>>();
			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				xmlDoc.Load(xmlFilePath);
			}
			catch (Exception)
			{
				return res;
			}

			XmlNode root = xmlDoc.FirstChild;
			foreach (XmlNode assembly in root.ChildNodes.OfType<XmlNode>())
			{
				if (assembly.LocalName == "#comment") continue;
				
				string assemblyName = assembly.Attributes["Name"].Value;
				Dictionary<string, HashSet<string>> types = new Dictionary<string, HashSet<string>>();
				foreach (XmlNode type in assembly.ChildNodes.OfType<XmlNode>())
				{
					if (type.LocalName == "#comment") continue;
					
					string typeName = type.Attributes["Name"].Value;
					HashSet<string> codeLines = new HashSet<string>();
					foreach (XmlNode codeBlock in type.ChildNodes.OfType<XmlNode>())
					{
						if (codeBlock.LocalName == "#comment") continue;
						
						string codeLine = codeBlock.Attributes["Content"].Value;
						codeLines.Add(codeLine);
					}

					if (codeLines.Count > 0)
					{
						types.Add(typeName, codeLines);
					}
				}

				if (types.Count > 0)
				{
					res.Add(assemblyName, types);
				}
			}

			return res;
		}

		private static HashSet<string> GetIgnoredFiles(string xmlFilePath)
		{
			HashSet<string> res = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				xmlDoc.Load(xmlFilePath);
			}
			catch (Exception)
			{
				return res;
			}

			XmlNode root = xmlDoc.FirstChild;
			foreach (XmlNode file in root.ChildNodes.OfType<XmlNode>())
			{
				if (file.LocalName == "#comment") continue;
				
				string filePath = file.Attributes["Path"].Value;
				res.Add(filePath);
			}

			return res;
		}

		private async Task Start()
		{
			PleaseWaitContainer.Visibility = Visibility.Visible;
			await Task.Delay(100); // This gives the "Please wait" message the time to display itself.
			
			Configuration.OutputOptions = Options;
			Configuration.MethodsToAddManuallyBecauseTheyAreUndetected = GetUndetectedMethods(UndetectedMethodXMLFilePath.Text);
			Configuration.CodeToAddManuallyBecauseItIsUndetected = GetAdditionnalCode(AdditionnalCodeXMLFilePath.Text);
			Configuration.ExcludedFiles = GetIgnoredFiles(IgnoredFilesXMLFilePath.Text);
			Configuration.AssembliesToAnalyzePath = AssembliesToAnalyzeFolderPath.Text;
			Configuration.MscorlibFolderPath = MscorlibFolderPath.Text;
			Configuration.ReferencedAssembliesFolderPath = ReferencedAssembliesFolderPath.Text;
			Configuration.PathOfDirectoryWhereFileAreGenerated = GeneratedFilesFolderPath.Text;
			switch (((ComboBoxItem) SelectedProduct.SelectedItem).Tag)
			{
				case "OpenSilver":
					Configuration.TargetProduct = Product.OPENSILVER;
					break;
				case "CSHTML5v2":
					Configuration.TargetProduct = Product.CSHTML5_V2;
					break;
				case "CSHTML5":
					Configuration.TargetProduct = Product.CSHTML5;
					break;
			}

			StubGenerator.Common.StubGenerator stubGenerator = new StubGenerator.Common.StubGenerator();
			stubGenerator.Run();
			PleaseWaitContainer.Visibility = Visibility.Collapsed;
			await Task.Delay(100); // This gives the "Please wait" message the time to close.
		}

		private async void StartButtonClick(object sender, RoutedEventArgs e)
		{
			// Save stuff to cache
			_settings["GeneratedFilesFolderPath"].Value = GeneratedFilesFolderPath.Text;
			_settings["ReferencedAssembliesFolderPath"].Value = ReferencedAssembliesFolderPath.Text;
			_settings["AssembliesToAnalyzeFolderPath"].Value = AssembliesToAnalyzeFolderPath.Text;
			_settings["ProductIndex"].Value = SelectedProduct.SelectedIndex.ToString();
			_config.Save(ConfigurationSaveMode.Modified);
			
			try
			{
				await Start();
				MessageBox.Show("Success.");
			}
			catch (Exception ex)
			{
				PleaseWaitContainer.Visibility = Visibility.Collapsed;
				MessageBox.Show("Something went wrong. Please verify your configuration and try again." + Environment.NewLine + Environment.NewLine + ex);
				Console.Error.WriteLine("Something went wrong. Please verify your configuration and try again." + Environment.NewLine + Environment.NewLine + ex);
			}
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
# CSHTML5.Tools.CompatibilityAnalyzer

Instructions this tool are coming soon


# CSHTML5.Tools.StubGenerator

This tool lets you create stubs for the 3rd-party referenced libraries used in your Silverlight projects.

The main purpose is to make the migration from Silverlight to the web via CSHTML5 easier by allowing you to replace your Silverlight 3rd-party libraries with JavaScript-based implementations while maintaining your original Silverlight code unchanged.

For context and general information about the migration from Silverlight to the web via CSHTML5, please read [this page](http://cshtml5.com/links/migrate-silverlight-wpf-apps-to-html-javascript.aspx).


### Tutorial

To create stubs from your Silverlight 3rd-party referenced libraries, please follow these steps:

1. Make sure that CSHTML5 version 1.x is installed on your computer ([Download Link here](http://cshtml5.com/links/community-edition.aspx)). Even if you are using CSHTML5 version 2.x, you still need to have version 1.x installed to use the Stub Generator.

2. Download the source code in this repository and open the .SLN file with Visual Studio.

3. Launch the project "CSHTML5.Tools.StubGenerator.App". It is a WPF application. The following window will appear:

![Stub Generator screenshot](/screenshots/cshtml5_stub_generator_screenshot1.png "Stub Generator screenshot")

4. Fill in the fields. You basically need to have 3 folders on your disk:

- Field #1: Enter the path to an empty folder on your disk where the stub files will be generated. In the screenshot below, it is "c:\temp\Output" (make sure that the folder exists and is empty):

![Folder screenshot](/screenshots/config_folder1.png "Folder screenshot")

- Field #2: Enter the path to a folder on your disk that contains all your Silverlight assemblies, that is, both your own application assemblies and the 3rd-party assemblies (Telerik, etc.). The easiest way is to point to the "bin\Debug\" folder of your Silverlight application. Alternatively, you can extract your .XAP file and point to the extracted files. In the screenshot below, we have extracted the .XAP file into the folder "c:\temp\ExtractedXAP":

![Folder screenshot](/screenshots/config_folder2.png "Folder screenshot")

- Field #3: Enter the path to a folder on your disk that contains only your own application assemblies. The easiest way to obtain it is to copy your own application assemblies (without copying the 3rd-party assemblies such as Telerik) from the "bin\Debug\" folder of your Silverlight application into a new empty folder on your disk, and then point to that folder. Alternatively, you can copy your application assemblies from the extracted .XAP file into a new folder on your disk, and then point to that folder. In the screenshot below, we have copy the application assemblie into the folder "c:\temp\OriginalApplicationAssemblies":

![Folder screenshot](/screenshots/config_folder3.png "Folder screenshot")

(Optional) If you want to use a specific OpenSilver.dll instead of CSHTML5, compile the OpenSilver configuration and set Field #9 to the compilation output dir e.g. OpenSilver\src\Runtime\Runtime\bin\OpenSilver\SL.WorkInProgress\netstandard2.0

You can leave the other fields empty or with their default value for now.

5. Click "Start" to generate the stubs. A message will appear indicating that the operation may take a few minutes.

6. After the operation is completed, open the folder that you specified in the first field (for example, in the screenshot above, it is "c:\temp\Output"). You will find all your stubs there.

Note: it is frequent that the stubs contain more classes than are actually needed by your application. This may be due for example to the fact that your application defines some styles for the 3rd-party controls (such as "TelerikStyleOverride.xaml" or "Resources\Themes\Telerik.Windows.Controls.xaml"), which reference some Telerik controls that you do not use in your application, or which you do not intend to migrate. In fact, when migrating a Silverlight application, the original Telerik styles are often removed altogether because the migrated controls are implemented via equivalent 3rd-party libraries such as Kendo UI, which come with their own styles.

To reduce the number of generated stubs to the strict minimum that is useful for the migration, we recommend you to remove the styles of the 3rd-party components, recompile your Silverlight application, and then re-run the Stub Generator. You may also want to manually strip out the code from the generated stubs that is not useful to get the migrated application to compile.


For any questions, please contact CSHTML5 support ([link](http://cshtml5.com/contact.aspx)).




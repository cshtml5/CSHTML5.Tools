using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetForHtml5.PrivateTools.AssemblyCompatibilityAnalyzer
{
    public static class Configuration
    {
        internal static readonly string GeneratedFilesFolderPath = "";

        internal static readonly string DefaultPathToFeaturesAndEstimationsFile = "";

        internal static readonly string[] AssembliesToAnalyze = new string[0];

        public static readonly HashSet<string> UrlNamespacesThatBelongToUserCode = new HashSet<string>();

        public static readonly HashSet<string> AttributesToIgnoreInXamlBecauseTheyAreFromBaseClasses = new HashSet<string>()
        {
            "AutomationProperties.AutomationId", //Attached
            "Background",
            "Class", //Attached
            "Cursor",
            "DesignHeight", //Attached
            "DesignWidth", //Attached
            "FlowDirection",
            "FontFamily",
            "FontSize",
            "FontWeight",
            "Foreground",
            "GotFocus",
            "Height",
            "HorizontalAlignment",
            "HorizontalContentAlignment",
            "Ignorable", //Attached
            "IsEnabled",
            "IsHidden", //Attached
            "IsTabStop",
            "Key", //Attached
            "LayoutOverrides", //Attached
            "Loaded",
            "LocalizationManager.ResourceKey",
            "LostFocus",
            "Margin",
            "MaxHeight",
            "MaxWidth",
            "MinHeight",
            "MinWidth",
            "MouseWheel",
            "Name", //Attached
            "RenderTransform",
            "StyleManager.Theme",
            "Uid", //Attached
            "Unloaded",
            "UseLayoutRounding",
            "VerticalAlignment",
            "VerticalContentAlignment",
            "Visibility",
            "Width",
        };

        internal static readonly HashSet<string> EntriesToNotAggregateWithOtherEntries = new HashSet<string>()
        {
            "Canvas.MouseRightButtonDown",
            "FrameworkElement.DataContextChanged",
            "DateTime.ToLongTimeString(...)",
            "Type.GetInterface(...)",
            "Type.GetTypeCode(...)",
            "Type.InvokeMember(...)",
            "UIElement.MouseRightButtonDown",
            "Control.TabNavigation",
            "HtmlDocument.Submit(...)",
            "TextBox.Select(...)",
            "TextBox.SelectionChanged",
            "TextBox.LineHeight",
            "UIElement.Clip",
            "UIElement.OnCreateAutomationPeer(...)",
            "Grid.Grid.Projection",
            "ScrollViewer.ComputedVerticalScrollBarVisibility",
            "Page.Title",
        }; 

        internal static readonly List<Tuple<string, string>> AdditionalEntriesToRemoveBeforeAggregation = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("Boolean..ctor(...)", "mscorlib"),
            new Tuple<string, string>("Double..ctor(...)", "mscorlib"),
            new Tuple<string, string>("Double.Get(...)", "mscorlib"),
            new Tuple<string, string>("Double.Set(...)", "mscorlib"),
            new Tuple<string, string>("Int32..ctor(...)", "mscorlib"),
            new Tuple<string, string>("Int32.Get(...)", "mscorlib"),
            new Tuple<string, string>("Int32.Set(...)", "mscorlib"),
            new Tuple<string, string>("String..ctor(...)", "mscorlib"),
            new Tuple<string, string>("Application.LoadComponent(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"),
            new Tuple<string, string>("Border.Get(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // This appears when client code has Border[,]
            new Tuple<string, string>("Border.Set(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // This appears when client code has Border[,]
            new Tuple<string, string>("BindingExpression.ParentBinding", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code this is a field rather than a property, this is why the analyzer does not find it.
            new Tuple<string, string>("ButtonBase.OnClick(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            //new Tuple<string, string>("ButtonBase.OnMouseEnter(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            //new Tuple<string, string>("ButtonBase.OnMouseLeave(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            new Tuple<string, string>("ChildWindow.OnClosed(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("ChildWindow.OnOpened(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Dispatcher.BeginInvoke(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's "CoreDispatcher.BeginInvoke(...)" in our code.
            new Tuple<string, string>("Canvas.Triggers", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with "BeginStoryboard" and "EventTrigger"
            new Tuple<string, string>("Color 'Inequality' operator", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Now implemented
            new Tuple<string, string>("ContentPropertyAttribute.Name", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Control.DefaultStyleKey", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "FrameworkElement".
            new Tuple<string, string>("Control.GetTemplateChild(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Control.OnGotFocus(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            new Tuple<string, string>("Control.OnLostFocus(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            new Tuple<string, string>("Control.OnKeyDown(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            new Tuple<string, string>("Control.OnKeyUp(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            new Tuple<string, string>("Control.OnMouseEnter(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            new Tuple<string, string>("Control.OnMouseLeave(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's in "UIElement".
            new Tuple<string, string>("Duration 'Implicit' operator", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Now implemented
            new Tuple<string, string>("Binding.Converter", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Binding.ConverterParameter", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Binding.ElementName", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Binding.Path", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Grid.ColumnDefinitions", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Grid.RowDefinitions", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("ResourceDictionary.MergedDictionaries", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("ToolTipService.ToolTip", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Color..ctor(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("FontWeight..ctor(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("HorizontalAlignment..ctor(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("VerticalAlignment..ctor(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("TabPanel..ctor(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("VisualStateManager.VisualStateGroups", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("INotifyCollectionChanged.CollectionChanged", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("ObservableCollection<T>.OnCollectionChanged(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("ObservableCollection<T>.OnPropertyChanged(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("ItemsControl.OnItemsChanged(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Canvas.MouseRightButtonDown", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with the non-supported "UIElement.MouseRightButtonDown", not sure why detected differently
            new Tuple<string, string>("ContentControl.TabNavigation", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with the non-supported "Control.TabNavigation", not sure why detected differently
            new Tuple<string, string>("SilverlightHost.Content", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Named "Host" instead of "SilverlightHost"
            new Tuple<string, string>("UIElement.MouseWheel", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Already mentioned the non-supported "MouseWheelEventHandler"
            new Tuple<string, string>("UIElement.UpdateLayout(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Already mentioned the non-supported "LayoutUpdated"
            new Tuple<string, string>("ToolTip.Triggers", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with "BeginStoryboard" and "EventTrigger"
            new Tuple<string, string>("Grid.Clip", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with the non-supported "UIElement.Clip", not sure why detected differently
            new Tuple<string, string>("Grid.Projection", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with "PlaneProjection"
            new Tuple<string, string>("Canvas.Canvas.ZIndex", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Malformed
            new Tuple<string, string>("Grid.Grid.Column", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Malformed
            new Tuple<string, string>("Grid.Grid.Row", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Malformed
            new Tuple<string, string>("Grid.Grid.ColumnSpan", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Malformed
            new Tuple<string, string>("Grid.Grid.RowSpan", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Malformed
            new Tuple<string, string>("ScrollViewer.ScrollViewer.VerticalScrollBarVisibility", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Malformed
            new Tuple<string, string>("Page.NavigationContext", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with NavigationContext.QueryString
            new Tuple<string, string>("Page.Language", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Redundant with XmlLanguage.GetLanguage(...)
            new Tuple<string, string>("Interaction.Triggers", "http://schemas.microsoft.com/expression/2010/..."), // Redundant with EventTrigger and DataTrigger
            new Tuple<string, string>("Interaction.Behaviors", "http://schemas.microsoft.com/expression/2010/..."), // Should be supported or easy to support (we already have the Behavior class)
            new Tuple<string, string>("PresentationFrameworkCollection<T>.Add(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.Clear(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.Contains(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.Count", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.GetEnumerator(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.IndexOf(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.Insert(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.Item", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.Remove(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("PresentationFrameworkCollection<T>.RemoveAt(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // In our code, UIElementCollection inherits from ObservableCollection<UIElement> rather than PresentationFrameworkCollection<UIElement>./
            new Tuple<string, string>("AsyncCompletedEventArgs.RaiseExceptionIfNecessary(...)", "System"), // Not sure why not found.
            new Tuple<string, string>("Uri.AbsolutePath", "System"), // Not sure why not found.
            new Tuple<string, string>("XmlReader.Create(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.GetAttribute(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.NodeType", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.IsEmptyElement", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.Read(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.ReadElementContentAsString(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.Name", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.LocalName", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.ReadStartElement(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.ReadEndElement(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.MoveToNextAttribute(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.MoveToElement(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.MoveToContent(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.MoveToFirstAttribute(...)", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlReader.Value", "System.Xml"), // Implemented in Cshtml5_XmlReader
            new Tuple<string, string>("XmlWriter.WriteElementString(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteAttributeString(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteString(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteStartAttribute(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteEndAttribute(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.Close(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteStartElement(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteEndElement(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.Create(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteStartDocument(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("XmlWriter.WriteEndDocument(...)", "System.Xml"), // Implemented in Cshtml5_XmlWriter
            new Tuple<string, string>("IsolatedStorageFile.DeleteFile(...)", "mscorlib"), // Not sure why not found.
            new Tuple<string, string>("IsolatedStorageFile.FileExists(...)", "mscorlib"), // Not sure why not found.
            new Tuple<string, string>("IsolatedStorageFile.OpenFile(...)", "mscorlib"), // Not sure why not found.
            new Tuple<string, string>("KeyCollection.GetEnumerator(...)", "mscorlib"), // Now it's supported
            new Tuple<string, string>("ValueCollection.GetEnumerator(...)", "mscorlib"), // Now it's supported

            // Telerik:
            new Tuple<string, string>("Grid.Grid.Column", "http://schemas.telerik.com/2008/xaml/presentation"), // Malformed
            new Tuple<string, string>("Grid.Grid.Row", "http://schemas.telerik.com/2008/xaml/presentation"), // Malformed
            new Tuple<string, string>("Grid.Grid.ColumnSpan", "http://schemas.telerik.com/2008/xaml/presentation"), // Malformed
            new Tuple<string, string>("Grid.Grid.RowSpan", "http://schemas.telerik.com/2008/xaml/presentation"), // Malformed
            new Tuple<string, string>("ScrollViewer.ScrollViewer.HorizontalScrollBarVisibility", "http://schemas.telerik.com/2008/xaml/presentation"), // Malformed
            new Tuple<string, string>("RadRoutedEventHandler..ctor(...)", "http://schemas.telerik.com/2008/xaml/presentation"), // Detail, used in multiple controls
            new Tuple<string, string>("RadRoutedEventArgs.Handled", "http://schemas.telerik.com/2008/xaml/presentation"), // Detail, used in multiple controls
            new Tuple<string, string>("RadRoutedEventArgs.OriginalSource", "http://schemas.telerik.com/2008/xaml/presentation"), // Detail, used in multiple controls
            new Tuple<string, string>("RadRoutedEventArgs.Source", "http://schemas.telerik.com/2008/xaml/presentation"), // Detail, used in multiple controls
            new Tuple<string, string>("Popup.Owner", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("Mouse.AddMouseDownHandler(...)", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("Mouse.RemoveMouseDownHandler(...)", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("ItemsControl.ItemContainerGenerator", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("ItemContainerGenerator.ContainerFromIndex(...)", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("Selector.SelectedValue", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("Selector.SelectedIndex", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("DependencyObjectExtensions.AddHandler(...)", "http://schemas.telerik.com/2008/xaml/presentation"), // Detail, used in multiple controls
            new Tuple<string, string>("Label.OpacityMask", "http://schemas.microsoft.com/winfx/2006/xaml/presentation/sdk"),
        };

        internal static readonly List<Tuple<string, string>> AdditionalEntriesToRemoveAfterAggregation = new List<Tuple<string, string>>()
        {
            new Tuple<string, string>("Dispatcher.BeginInvoke(...)", "mscorlib"), // Named "CoreDispatcher" at the time of writing.
            new Tuple<string, string>("NotifyCollectionChangedEventArgs (members used: Action, NewItems, NewStartingIndex, OldItems)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("NotifyCollectionChangedEventHandler", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("ObservableCollection<T> (members used: CollectionChanged, OnCollectionChanged(...))", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Visibility", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("StaticResource (members used: ResourceKey)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // It's called "StaticResourceExtension".
            new Tuple<string, string>("BusyIndicator.BusyContent", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Soon to be supported
            new Tuple<string, string>("TextDecorations.Underline", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Enumerator.Current, .MoveNext(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Not sure why not found.
            new Tuple<string, string>("Enumerator.Current, .MoveNext(...)", "System"), // Not sure why not found.
            new Tuple<string, string>("Enumerator.Current, .MoveNext(...)", "mscorlib"), // Not sure why not found.
            new Tuple<string, string>("PropertyChangedEventArgs.PropertyName", "System"), // Not sure why not found.
            new Tuple<string, string>("AsyncCompletedEventArgs (members used: Cancelled, Error, RaiseExceptionIfNecessary(...), UserState)", "System"), // Not sure why not found.
            new Tuple<string, string>("Behavior.OnAttached(...), .OnDetaching(...)", "System.Windows.Interactivity"), // Not sure why not found.
            new Tuple<string, string>("Behavior.OnAttached(...), .OnDetaching(...)", "http://schemas.microsoft.com/expression/2010/..."), // Not sure why not found.
            new Tuple<string, string>("ClientBase<T> (members used: Channel, InnerChannel, InvokeAsync(...))", "System.ServiceModel"), // Used in "Reference.cs": is supported under the name CSHTML5_ClientBase
            new Tuple<string, string>("ClientBase<T> (members used: Channel, Endpoint, InnerChannel, InvokeAsync(...))", "System.ServiceModel"), // Used in "Reference.cs": is supported under the name CSHTML5_ClientBase
            new Tuple<string, string>("ServiceEndpoint.Address", "System.ServiceModel"), // Minor thing
            new Tuple<string, string>("IChannel.GetProperty(...)", "System.ServiceModel"), // Used in "Reference.cs": should be supported
            new Tuple<string, string>("ICommunicationObject.BeginClose(...), .BeginOpen(...), .EndClose(...), .EndOpen(...)", "System.ServiceModel"), // Used in "Reference.cs": should be supported
            new Tuple<string, string>("IHttpCookieContainerManager.CookieContainer", "System.ServiceModel"), // Used in "Reference.cs": should be supported
            new Tuple<string, string>("ResourceManager.GetString(...)", "mscorlib"), // RESX files are supposed to be supported
            new Tuple<string, string>("SendOrPostCallback", "mscorlib"), // Not sure why not found.
            new Tuple<string, string>("ValueCollection.GetEnumerator(...)", "mscorlib"), // Not sure why not found.
            new Tuple<string, string>("IsolatedStorageFileStream", "mscorlib"), // Not sure why not found.
            new Tuple<string, string>("DependencyObjectCollection<T>.GetEnumerator(...)", "http://schemas.microsoft.com/winfx/2006/xaml/presentation"), // Used in the "foreach" only to iterate a collection of dependency objects, so the types of collections that we have do the job fine

            // Telerik:
            new Tuple<string, string>("IGroup.Items, .Key", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("ISortDescriptor.SortDirection", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("ItemsControl.ItemContainerStyleSelector", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("HeaderedItemsControl.Header", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("StyleSelector (members used: SelectStyle(...))", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("StyleManager.GetTheme(...), .SetTheme(...)", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("ValidationTooltip (members used: TooltipContent, TooltipContentTemplate, TooltipPlacementTarget)", "http://schemas.telerik.com/2008/xaml/presentation"),
            new Tuple<string, string>("HeaderedContentControl.Header, HeaderedItemsControl.Header, HierarchicalDataTemplate (members used: ItemContainerStyle, ItemsSource)", "http://schemas.telerik.com/2008/xaml/presentation"), // Redundant with RadTreeView etc.
        };

        internal static readonly List<HashSet<string>> ClassesRelatedToEachOther = new List<HashSet<string>>()
        {
            new HashSet<string>() { "DataGrid", "DataGridCellsPresenter", "DataGridColumn", "DataGridColumnHeadersPresenter", "DataGridDetailsPresenter", "DataGridFrozenGrid", "DataGridRow", "DataGridRowHeader", "DataGridRowsPresenter", "DataGridTemplateColumn", "DataGridTextColumn", "DataGridBeginningEditEventArgs", "DataGridCellEditEndedEventArgs", "DataGridPreparingCellForEditEventArgs", "DataGridRowDetailsEventArgs", "DataGridRowEditEndedEventArgs", "DataGridRowEditEndingEventArgs" },
            new HashSet<string>() { "Application", "ApplicationUnhandledExceptionEventArgs" },
            new HashSet<string>() { "BeginStoryboard", "EventTrigger" },
            new HashSet<string>() { "INotifyDataErrorInfo", "DataErrorsChangedEventArgs", "ValidationErrorEventArgs" },
            new HashSet<string>() { "DoubleAnimationUsingKeyFrames", "EasingDoubleKeyFrame", "SplineDoubleKeyFrame", "DiscreteDoubleKeyFrame", "LinearDoubleKeyFrame" },
            new HashSet<string>() { "ColorAnimationUsingKeyFrames", "SplineColorKeyFrame" },
            new HashSet<string>() { "TreeView", "TreeViewItem", "HierarchicalDataTemplate", "HeaderedItemsControl", "HeaderedContentControl" },
            new HashSet<string>() { "PrintDocument", "PrintPageEventArgs" },
            new HashSet<string>() { "VisualStateGroup", "VisualStateGroup.Transitions", "VisualTransition" },
            new HashSet<string>() { "MouseWheelEventHandler", "MouseWheelEventArgs", "ScriptObject", "HtmlEventArgs" },
            new HashSet<string>() { "AreaSeries", "Axis", "Chart", "ColumnBarBaseSeries<T>", "ColumnSeries", "DataPointSeries", "DisplayAxis", "LinearAxis", "LineAreaBaseSeries<T>", "NumericAxis" },
            new HashSet<string>() { "BackgroundWorker", "DoWorkEventHandler", "ProgressChangedEventHandler", "RunWorkerCompletedEventHandler" },
            new HashSet<string>() { "AssemblyCatalog", "ExportProvider", "CompositionHost" }, // MEF-related classes
            new HashSet<string>() { "XmlReader", "XmlReaderSettings" },
            new HashSet<string>() { "XmlWriter", "XmlWriterSettings" },
            new HashSet<string>() { "ChannelFactory", "ServiceEndpoint", "ClientRuntime" },
            new HashSet<string>() { "ComplexObject", "DomainContext", "DomainException", "Entity", "EntityChangeSet", "EntityCollection<T>", "EntityContainer", "EntityKey", "EntityQueryable", "EntityRef<T>", "EntitySet", "EntitySet<T>", "OperationBase", "WebContextBase", "WebDomainClient<T>" }, // RIA Services
            new HashSet<string>() { "HashAlgorithm", "SHA256Managed" },
            new HashSet<string>() { "ParameterizedThreadStart", "Thread" },
            new HashSet<string>() { "RNGCryptoServiceProvider", "RandomNumberGenerator" },
            new HashSet<string>() { "RotateTransform", "ScaleTransform", "SkewTransform", "TranslateTransform" },
            new HashSet<string>() { "ProgressBar", "LinearGradientBrush" }, // cf. "WorkingStatus.xaml" in EPS project
            new HashSet<string>() { "StartupEventArgs", "SilverlightHost" }, // For the "InitParams" member
            new HashSet<string>() { "CollectionViewSource", "PropertyGroupDescription" },

            // Telerik:
            new HashSet<string>() { "GridViewCell", "GridViewCellBase", "GridViewCellClipboardEventArgs", "GridViewCellEditEndedEventArgs", "GridViewCellInfo", "GridViewColumn", "GridViewColumnCollection", "GridViewComboBoxColumn", "GridViewDataColumn", "GridViewDataControl", "GridViewElementExportingEventArgs", "GridViewExportOptions", "GridViewLength", "GridViewRowItem", "GridViewRowItemEventArgs", "GridViewSortingEventArgs", "GridViewBeginningEditRoutedEventArgs", "GridViewBoundColumnBase", "RadGridView", "RadGridViewCommands", "RadRowItem", "SortDescriptor", "SortDescriptorBase", "BaseItemsControl", "CancelRoutedEventArgs", "ColumnGroupDescriptor", "ColumnSortDescriptor", "ColumnWidthChangingEventArgs", "DataControl", "DataItemCollection", "GridViewColumnGroup", "GridViewHyperlinkColumn", "GridViewImageColumn",
                                    "VirtualQueryableCollectionView", "VirtualQueryableCollectionViewItemsLoadingEventArgs", "QueryableCollectionView",
                                    "AverageFunction", "SumFunction",
                                    "FilteringDropDown",
                                    "RadDataPager" },
            new HashSet<string>() { "ChildrenOfTypeExtensions", "ParentOfTypeExtensions" },
            new HashSet<string>() { "RadDocking", "RadDocumentPane", "RadPane", "RadPaneGroup", "RadSplitContainer" },
            new HashSet<string>() { "RadSlider", "DoubleRangeBase" },
            new HashSet<string>() { "RadRoutedEventArgs", "RadRoutedEventHandler" },
            new HashSet<string>() { "RadTabControl", "RadTabControlBase", "RadTabItem" },
            new HashSet<string>() { "RadToolBar", "RadToolBarSeparator", "RadSplitButton" },
            new HashSet<string>() { "RadWindow", "WindowBase", "WindowPreviewClosedEventArgs", "DialogParameters", "RadWindowManager", "WindowClosedEventArgs" },
            new HashSet<string>() { "ExportExtensions", "PngBitmapEncoder" },
            new HashSet<string>() { "RadTransitionControl", "PerspectiveRotationTransition" },
            new HashSet<string>() { "RadNumericUpDown", "RadRangeBase" },
            new HashSet<string>() { "RadChart", "Axis", "AxisStyles", "AxisX", "AxisY", "BarSeriesDefinition", "ChartArea", "ChartDefaultView", "ChartLegend", "ISeriesDefinition", "LinearPointMarkSeriesDefinition", "LineSeriesDefinition", "PolarAreaSeries", "PolarAxis", "PolarChartGrid", "PolarDataPoint", "PolarPointSeries", "RadPolarChart", "SeriesMapping",
                                    "MarkerShape", "ItemMapping", "SamplingSettings", "NumericRadialAxis" },
            new HashSet<string>() { "RadTreeView", "RadTreeViewItem" },
            new HashSet<string>() { "RadUpload", "RadUploadSelectedFile", "RadUploadSession", "UploadStartedEventHandler", "FileUploadFailedEventHandler", "FileUploadFailedEventArgs" },
            new HashSet<string>() { "ReportViewer", "ReportViewerModel", "RenderBeginEventHandler", "RenderBeginEventArgs" },
            new HashSet<string>() { "LinearScale", "RadHorizontalLinearGauge", "RadVerticalLinearGauge", "Marker", "BarIndicator", "ScaleObject", "ScaleObject.ScaleObject", "StateIndicator", "GaugeRange" },
            new HashSet<string>() { "RadMediaPlayer", "RadMediaItem" },
            new HashSet<string>() { "RadCalendar", "CalendarPanel" },
            new HashSet<string>() { "DateTimePickerClock", "DateTimePickerExtensions", "DateTimePickerExtensions.DateTimePickerExtensions", "RadDateTimePicker", "TransitionPanel" },
            new HashSet<string>() { "Office_BlackTheme", "MetroTheme", "MetroColors", "MetroColorPalette" },
            new HashSet<string>() { "InvertedBooleanToVisibilityConverter", "TextToVisibilityConverter", "HorizontalContentAlignmentToTextAlignmentConverter" },
            new HashSet<string>() { "RadContextMenu", "RadMenuItem" },
        };
    }
}

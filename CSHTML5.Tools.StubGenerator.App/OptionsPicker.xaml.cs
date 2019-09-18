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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DotNetForHtml5.PrivateTools
{
    /// <summary>
    /// Interaction logic for OptionsPicker.xaml
    /// </summary>
    public partial class OptionsPicker : Window
    {
        public OutputOptions Options { get; }

        public OptionsPicker()
        {
            Options = new OutputOptions();
            InitializeComponent();
        }

        private void MethodOptionsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBox)sender).SelectedIndex)
            {
                case 0:
                    Options.OutputMethodOptions = OutputMethodOptions.OUTPUT_RETURN_TYPE;
                    break;
                case 1:
                    Options.OutputMethodOptions = OutputMethodOptions.OUTPUT_RETURN_TYPE_NOT_NULL;
                    break;
                case 2:
                    Options.OutputMethodOptions = OutputMethodOptions.OUTPUT_NOT_IMPLEMENTED;
                    break;
            }
        }

        private void PropertyOptionsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBox)sender).SelectedIndex)
            {
                case 0:
                    Options.OutputPropertyOptions = OutputPropertyOptions.OUTPUT_PRIVATE_FIELD;
                    break;
                case 1:
                    Options.OutputPropertyOptions = OutputPropertyOptions.OUTPUT_RETURN_TYPE;
                    break;
                case 2:
                    Options.OutputPropertyOptions = OutputPropertyOptions.OUTPUT_RETURN_TYPE_NOT_NULL;
                    break;
                case 3:
                    Options.OutputPropertyOptions = OutputPropertyOptions.OUTPUT_NOT_IMPLEMENTED;
                    break;
            }
        }

        private void EventOptionsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBox)sender).SelectedIndex)
            {
                case 0:
                    Options.OutputEventOptions = OutputEventOptions.AUTO_IMPLEMENT;
                    break;
                case 1:
                    Options.OutputEventOptions = OutputEventOptions.OUTPUT_EMPTY_IMPLEMENTATION;
                    break;
                case 2:
                    Options.OutputEventOptions = OutputEventOptions.OUTPUT_NOT_IMPLEMENTED;
                    break;
            }
        }

        private void ValidateButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void ShowFullTypeNameChecked(object sender, RoutedEventArgs e)
        {
            Options.OutputFullTypeName = true;
        }

        private void ShowFullTypeNameUnchecked(object sender, RoutedEventArgs e)
        {
            Options.OutputFullTypeName = false;
        }

        private void GenerateOnlyPublicMembersChecked(object sender, RoutedEventArgs e)
        {
            Options.OutputOnlyPublicAndProtectedMembers = true;
        }

        private void GenerateOnlyPublicMembersUnchecked(object sender, RoutedEventArgs e)
        {
            Options.OutputOnlyPublicAndProtectedMembers = false;
        }
    }
}

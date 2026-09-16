using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OmniHub.Core.Enums;
using OmniHub.UI.ViewModels;

namespace OmniHub.UI.Components;

public partial class DynamicParameterControl : UserControl
{
    public DynamicParameterControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Event handler for the Browse button in DynamicParameterControl.
    /// Distinguishes between input files (OpenFileDialog) and output files (SaveFileDialog).
    /// </summary>
    public void BrowseFile_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ParameterViewModel vm)
        {
            BrowseFile(vm);
        }
    }

    /// <summary>
    /// Opens the appropriate file dialog based on whether the parameter represents an output or input file.
    /// </summary>
    public static void BrowseFile(ParameterViewModel vm)
    {
        if (vm == null) return;

        if (IsOutputFileParameter(vm))
        {
            var saveDialog = new SaveFileDialog
            {
                Title = vm.IsArabic
                    ? (!string.IsNullOrWhiteSpace(vm.DisplayLabel) ? $"حفظ {vm.DisplayLabel}" : "حفظ الملف الناتج")
                    : (!string.IsNullOrWhiteSpace(vm.Label) ? $"Save {vm.Label}" : "Save Output File"),
                Filter = string.IsNullOrEmpty(vm.Definition.Filter) ? "All Files (*.*)|*.*" : vm.Definition.Filter,
                FileName = vm.SuggestOutputFileName(),
                AddExtension = true,
                OverwritePrompt = true
            };

            var initialDir = vm.SuggestInitialDirectory();
            if (!string.IsNullOrWhiteSpace(initialDir) && Directory.Exists(initialDir))
            {
                saveDialog.InitialDirectory = initialDir;
            }

            var defaultExt = vm.GetDefaultExtension();
            if (!string.IsNullOrWhiteSpace(defaultExt))
            {
                saveDialog.DefaultExt = defaultExt.TrimStart('.');
            }

            if (saveDialog.ShowDialog() == true)
            {
                vm.Value = saveDialog.FileName;
            }
        }
        else
        {
            var openDialog = new OpenFileDialog
            {
                Title = vm.IsArabic
                    ? (!string.IsNullOrWhiteSpace(vm.DisplayLabel) ? $"اختيار {vm.DisplayLabel}" : "اختيار ملف")
                    : (!string.IsNullOrWhiteSpace(vm.Label) ? $"Select {vm.Label}" : "Select File"),
                Filter = string.IsNullOrEmpty(vm.Definition.Filter) ? "All Files (*.*)|*.*" : vm.Definition.Filter,
                CheckFileExists = true,
                CheckPathExists = true
            };

            var initialDir = vm.SuggestInitialDirectory();
            if (!string.IsNullOrWhiteSpace(initialDir) && Directory.Exists(initialDir))
            {
                openDialog.InitialDirectory = initialDir;
            }

            if (!string.IsNullOrWhiteSpace(vm.Value) && !vm.Value.Contains("${"))
            {
                try
                {
                    if (File.Exists(vm.Value))
                    {
                        openDialog.FileName = Path.GetFileName(vm.Value);
                        openDialog.InitialDirectory = Path.GetDirectoryName(vm.Value);
                    }
                    else if (Directory.Exists(vm.Value))
                    {
                        openDialog.InitialDirectory = vm.Value;
                    }
                }
                catch { }
            }

            if (openDialog.ShowDialog() == true)
            {
                vm.Value = openDialog.FileName;
            }
        }
    }

    /// <summary>
    /// Checks if a parameter represents an output/target/destination file.
    /// Examines: ParameterType == OutputFile, or Id/Label containing 'Output', 'Target', 'Destination', 'الناتج', 'حفظ'.
    /// </summary>
    public static bool IsOutputFileParameter(ParameterViewModel vm)
    {
        if (vm == null) return false;
        if (vm.Type == ParameterType.OutputFile) return true;

        var id = vm.Id ?? string.Empty;
        var label = vm.Label ?? string.Empty;
        var desc = vm.Definition?.Description ?? string.Empty;

        return id.IndexOf("Output", StringComparison.OrdinalIgnoreCase) >= 0 ||
               id.IndexOf("Target", StringComparison.OrdinalIgnoreCase) >= 0 ||
               id.IndexOf("Destination", StringComparison.OrdinalIgnoreCase) >= 0 ||
               label.IndexOf("Output", StringComparison.OrdinalIgnoreCase) >= 0 ||
               label.IndexOf("Target", StringComparison.OrdinalIgnoreCase) >= 0 ||
               label.IndexOf("Destination", StringComparison.OrdinalIgnoreCase) >= 0 ||
               label.IndexOf("الناتج", StringComparison.OrdinalIgnoreCase) >= 0 ||
               label.IndexOf("المستهدف", StringComparison.OrdinalIgnoreCase) >= 0 ||
               label.IndexOf("حفظ", StringComparison.OrdinalIgnoreCase) >= 0 ||
               desc.IndexOf("حفظ", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}


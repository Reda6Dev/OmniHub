using CommunityToolkit.Mvvm.ComponentModel;
using OmniHub.Core.Enums;

namespace OmniHub.UI.ViewModels;

public partial class NewToolParameterViewModel : ObservableObject
{
    [ObservableProperty] private string _id = "";
    [ObservableProperty] private string _label = "";
    [ObservableProperty] private ParameterType _type = ParameterType.Text;
    [ObservableProperty] private string _defaultValue = "";
    [ObservableProperty] private bool _isRequired = true;

    public string TypeText => Type switch
    {
        ParameterType.FilePath => "FilePath — ملف",
        ParameterType.DirectoryPath => "DirectoryPath — مجلد",
        ParameterType.Text => "Text — نص",
        ParameterType.Number => "Number — رقم",
        ParameterType.Checkbox => "Checkbox — نعم/لا",
        ParameterType.Dropdown => "Dropdown — قائمة",
        ParameterType.OutputFile => "OutputFile — ملف إخراج/حفظ",
        _ => Type.ToString()
    };

    partial void OnTypeChanged(ParameterType value) => OnPropertyChanged(nameof(TypeText));
}

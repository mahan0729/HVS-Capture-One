using System.Windows;
using System.Windows.Controls;
using HVSCaptureOne.App.ViewModels.Wizard;

namespace HVSCaptureOne.App.Views.Wizard;

/// <summary>
/// Interaction logic for FileImportStepView.xaml.
/// Handles drag-drop events and delegates file processing to the ViewModel.
/// </summary>
public partial class FileImportStepView : UserControl
{
    /// <summary>
    /// Initializes the FileImportStepView.
    /// </summary>
    /// <returns></returns>
    public FileImportStepView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Sets the drag effect to Copy when a file is dragged over the control.
    /// </summary>
    /// <returns></returns>
    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    /// <summary>
    /// Handles a file dropped onto the control and passes the path to the ViewModel.
    /// </summary>
    /// <returns></returns>
    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        var files = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (files?.Length > 0 && DataContext is FileImportStepViewModel vm)
            vm.HandleDrop(files[0]);
    }
}

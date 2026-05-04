using System.Windows;

namespace HVSCaptureOne.App.Views;

public partial class HelpDialog : Window
{
    public HelpDialog() => InitializeComponent();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}

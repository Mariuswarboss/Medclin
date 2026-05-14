using System.Windows;

namespace Mediclin.UI.Views.Dialogs;

public partial class ConfirmDeleteDialog : Window
{
    public ConfirmDeleteDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        TitleStrip.Text = title;
        MessageBlock.Text = message;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

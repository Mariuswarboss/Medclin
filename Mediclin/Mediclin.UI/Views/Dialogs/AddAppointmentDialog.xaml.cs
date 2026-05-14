using System.Windows;

namespace Mediclin.UI.Views.Dialogs;

public partial class AddAppointmentDialog : Window
{
    public AddAppointmentDialog()
    {
        InitializeComponent();
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

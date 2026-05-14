using System.Windows;
using Mediclin.UI.Services;
using Mediclin.UI.ViewModels.Dialogs;

namespace Mediclin.UI.Views.Dialogs;

public partial class AddPatientDialog : Window
{
    public AddPatientDialog(ApplicationServices app)
    {
        InitializeComponent();
        DataContext = new AddPatientDialogViewModel(app, this);
    }
}

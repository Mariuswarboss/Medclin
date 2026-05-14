using System.Windows.Input;
using Mediclin.UI.ViewModels.Doctor;

namespace Mediclin.UI.Views.Doctor;

public partial class PatientsView
{
    public PatientsView()
    {
        InitializeComponent();
    }

    private void PatientsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PatientsViewModel vm)
        {
            vm.OpenEMRCommand.Execute(null);
        }
    }
}

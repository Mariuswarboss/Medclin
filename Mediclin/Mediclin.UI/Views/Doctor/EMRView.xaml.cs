using System.Windows;
using System.Windows.Controls;
using Mediclin.UI.ViewModels.Doctor;

namespace Mediclin.UI.Views.Doctor;

public partial class EMRView : UserControl
{
    public EMRView()
    {
        InitializeComponent();
    }

    private void OnChangePatientClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is EMRViewModel vm)
            vm.IsSearchPanelVisible = true;
    }
}

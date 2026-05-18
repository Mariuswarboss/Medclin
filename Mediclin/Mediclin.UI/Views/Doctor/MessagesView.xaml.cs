using System;
using System.Windows.Controls;

namespace Mediclin.UI.Views.Doctor;

public partial class MessagesView : UserControl, IDisposable
{
    private bool _disposed;

    public MessagesView()
    {
        InitializeComponent();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (DataContext is IDisposable d)
        {
            d.Dispose();
        }
    }
}

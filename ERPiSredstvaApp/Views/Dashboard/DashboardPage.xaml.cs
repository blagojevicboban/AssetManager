using System.Windows.Controls;
using ERPiSredstvaData;

namespace ERPiSredstvaApp.Views.Dashboard;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _viewModel = new DashboardViewModel(db);
        DataContext = _viewModel;
    }
}

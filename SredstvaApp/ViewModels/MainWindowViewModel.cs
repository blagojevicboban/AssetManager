using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SredstvaDbContext _dbContext;

    [ObservableProperty]
    private string title = "SredstvaSystem - Osnovna Sredstva";
    
    [ObservableProperty]
    private ObservableCollection<Sredstvo> sredstva = new();
    
    public MainWindowViewModel(SredstvaDbContext dbContext)
    {
        _dbContext = dbContext;
        LoadData();
    }

    private void LoadData()
    {
        var data = _dbContext.Sredstva.ToList();
        Sredstva = new ObservableCollection<Sredstvo>(data);
    }
}

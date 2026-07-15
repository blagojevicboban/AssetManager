using System.Windows.Controls;
using SredstvaData;

namespace SredstvaApp.Views.Kartice;

public partial class KarticePage : Page
{
    private readonly SredstvaDbContext _db;

    public KarticePage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
    }
}

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using SredstvaData;

namespace SredstvaApp.Views.Korisnici;

public partial class LoginWindow : Window
{
    private readonly SredstvaDbContext _db;

    public LoginWindow(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        TxtUsername.Focus();
    }

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            DoLogin();
        }
    }

    private void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        DoLogin();
    }

    private void DoLogin()
    {
        TxtError.Visibility = Visibility.Collapsed;
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Unesite korisničko ime i lozinku.");
            return;
        }

        var hash = SredstvaDbContext.HashPassword(password);
        
        var korisnik = _db.Korisnici.FirstOrDefault(k => k.KorisnickoIme == username && k.LozinkaHash == hash);

        if (korisnik == null)
        {
            ShowError("Pogrešno korisničko ime ili lozinka.");
            return;
        }

        if (!korisnik.JeAktivan)
        {
            ShowError("Vaš nalog je deaktiviran. Obratite se administratoru.");
            return;
        }

        // Uspešan login
        AppSession.TrenutniKorisnik = korisnik;
        
        var mainWindow = new MainWindow(_db);
        mainWindow.Show();
        
        this.Close();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }
}

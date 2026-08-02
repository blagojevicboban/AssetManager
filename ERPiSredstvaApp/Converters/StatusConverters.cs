using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPiSredstvaApp.Converters;

public class BooleanToStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isKnjizen && isKnjizen)
            return new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Zelenkasto (Aktivno / Proknjiženo)
        
        return new SolidColorBrush(Color.FromRgb(220, 53, 69)); // Crveno (Neaktivno / Nije proknjiženo)
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToStatusTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAktivan && isAktivan)
            return "AKTIVAN";
        
        return "NEAKTIVAN";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToDeactivateTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isAktivan && isAktivan)
            return "Deaktiviraj";
        
        return "Aktiviraj";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

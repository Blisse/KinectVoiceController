using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace KinectMusicControl.Views.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is String)
            {
                var stringValue = (String) value;

                if (!String.IsNullOrWhiteSpace(stringValue))
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }

            throw new NotSupportedException("You can't do this");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("You can't do this");
        }
    }
}

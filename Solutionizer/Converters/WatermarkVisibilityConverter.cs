using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Solutionizer.Converters {
    public class WatermarkVisibilityConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            if (values[0] is bool && values[1] is bool) {
                var hasText = !(bool) values[0];
                var hasFocus = (bool) values[1];

                if (hasFocus || hasText) {
                    return Visibility.Collapsed;
                }
            }
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return new object[0];
        }
    }
}
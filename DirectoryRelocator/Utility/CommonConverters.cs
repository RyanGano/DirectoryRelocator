using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DirectoryRelocator.Utility
{
	class CommonConverters
	{
		public static readonly IValueConverter BooleanToVisibility = new BooleanToVisibilityConverter();
		public static readonly IValueConverter BooleanToInverseVisibility = new BooleanToInverseVisibilityConverter();
	}

	[ValueConversion(typeof(bool), typeof(Visibility))]
	internal class BooleanToInverseVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (value as bool?).GetValueOrDefault() ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}

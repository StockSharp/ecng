﻿namespace Ecng.Xaml.Converters
{
	using System;
	using System.Globalization;
	using System.Windows;
	using System.Windows.Data;

	using Ecng.Common;

	public class BoolToVisibilityConverter : IValueConverter
	{
		public BoolToVisibilityConverter()
		{
			TrueVisibilityValue = Visibility.Visible;
			FalseVisibilityValue = Visibility.Collapsed;
		}

		public Visibility FalseVisibilityValue { get; set; }
		public Visibility TrueVisibilityValue { get; set; }

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var param = parameter == null || parameter.To<bool>();
			var val = (bool)value;

			return val == param ? TrueVisibilityValue : FalseVisibilityValue;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((Visibility)value == TrueVisibilityValue);
		}
	}
}
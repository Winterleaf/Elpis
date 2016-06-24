/*
 * Copyright 2012 - Adam Haile
 * http://adamhaile.net
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify 
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version.
 * 
 * Elpis is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License 
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/

namespace Elpis.Wpf
{
    public class BinaryImageConverter : System.Windows.Data.IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (value == null || value.GetType() != typeof (byte[]))
            {
                return parameter ?? new System.Windows.Media.Imaging.BitmapImage();
            }

            byte[] data = (byte[]) value;
            System.Windows.Media.Imaging.BitmapImage result = new System.Windows.Media.Imaging.BitmapImage();
            result.BeginInit();
            result.StreamSource = new System.IO.MemoryStream(data);
            result.EndInit();
            return result;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }

    public class BooleanToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            bool state = true;
            if (parameter != null)
                state = (bool) parameter;

            if (value is bool)
            {
                return (bool) value
                    ? (state ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden)
                    : (state ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible);
            }

            return System.Windows.Visibility.Visible;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }

    public class WindowStateToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (!(value is System.Windows.WindowState)) return value;
            bool state = (System.Windows.WindowState) value == System.Windows.WindowState.Maximized;
            if (parameter != null && !bool.Parse((string) parameter))
                state = !state;

            return state ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }

    public class WindowStateToThicknessConverter : System.Windows.Data.IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            if (!(value is System.Windows.WindowState)) return value;
            bool state = (System.Windows.WindowState) value == System.Windows.WindowState.Maximized;
            int margin = 0;

            if (parameter != null && !state)
                margin = int.Parse((string) parameter);

            return new System.Windows.Thickness(margin);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }

    public class AssemblyVersionConverter : System.Windows.Data.IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            try
            {
                System.Version ver = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
                return ver.ToString();
            }
            catch (System.Exception)
            {
                return "0.0.0.0";
            }
        }

        public object ConvertBack(object value, System.Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}
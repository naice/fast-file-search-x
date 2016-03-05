using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BindingConverter
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parm = (string)parameter;
            bool show = false;

            if (value is string || value is String)
            {
                if (parameter is string && parm.Contains("nullorempty"))
                {
                    show = !string.IsNullOrEmpty(value as string);
                }
                else if (parameter is string && parm.Contains("nullorwhitespace"))
                {
                    show = !string.IsNullOrWhiteSpace(value as string);
                }
                else
                {
                    show = value != null;
                }

                if (parameter is string && parm.Contains("invert"))
                    show = !show;

            }
            else if (value is bool || value is Boolean)
            {
                show = (bool)value;
                if (parameter is string && parm == "invert")
                    show = !show;
            }
            else if (value is int)
            {
                show = (int)value > 0;
                if (parameter is string && parm == "invert")
                    show = !show;
            }
            else
            {
                if (parameter is string && parm == "invert")
                    show = value == null;
                else
                    show = value != null;
            }

            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
    public class EnumToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //bool show = ((int)value) == System.Convert.ToInt32(parameter);
            bool show = System.Convert.ToInt32(value) == System.Convert.ToInt32(parameter);

            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
    public class WpfFileIconCacheConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string key = value as string;
            if (false == string.IsNullOrEmpty(key))
            {
                if (ffsx.Class.WpfFileIconCache.ICONS.ContainsKey(key))
                {
                    return ffsx.Class.WpfFileIconCache.ICONS[key];
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }
    }
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return FileSizeToString((long)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;
        }

        private static string FileSizeToString(long size)
        {
            return string.Format("{0:F} KB", (double)size / 1024);
        }
    }
    public class FileMaskConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ToString(value as IEnumerable<string>);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ToMask(value as string);
        }

        public static ObservableCollection<string> ToMask(string masks)
        {
            ObservableCollection<string> r = new ObservableCollection<string>();

            if (!string.IsNullOrEmpty(masks))
            {
                var tokens = masks.Split(new char[]{';'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in tokens)
                {
                    string mask = item.Trim();
                    if (!string.IsNullOrWhiteSpace(mask))
                        r.Add(mask);
                }
            }

            return r;
        }
        public static string ToString(IEnumerable<string> masks)
        {
            string r = "";
            if (masks != null)
            {
                string delim = "";
                foreach (var item in masks)
                {
                    r += delim + item.Trim();

                    delim = "; ";
                }
            }
            return r.Trim();
        }
    }
}

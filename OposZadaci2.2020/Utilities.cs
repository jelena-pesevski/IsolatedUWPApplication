using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace OposZadaci2._2020
{
    public static class Utilities
    {
        public static Visibility ToVisibility(this bool isVisible) => isVisible ? Visibility.Visible : Visibility.Collapsed;

        public static string Shorten(this string text, int maxLength, bool fromEnd = false)
        {
            if (text.Length > maxLength)
                return $"{text.Substring(fromEnd ? text.Length - maxLength : 0, maxLength)}...";
            else
                return text;
        }
    }





   
}

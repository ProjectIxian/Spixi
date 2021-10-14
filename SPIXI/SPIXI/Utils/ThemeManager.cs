using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xamarin.Forms;

namespace SPIXI
{
    public enum ThemeAppearance
    {
        automatic = 0,
        light = 1,
        dark = 2
    }

    public static class ThemeManager
    {
        private static ThemeAppearance activeAppearance = ThemeAppearance.automatic;
        private static string activeTheme = "spixiui";

        public static bool loadTheme(string name, ThemeAppearance appearance)
        {
            var platform_utils = DependencyService.Get<IPlatformUtils>();

            string appearance_name = "light";
            if(appearance == ThemeAppearance.dark)
            {
                appearance_name = "dark";
            }
            else if(appearance == ThemeAppearance.automatic)
            {
                if(Application.Current.UserAppTheme == OSAppTheme.Dark)
                    appearance_name = "dark";
            }

            string theme_folder_path = Path.Combine(platform_utils.getAssetsPath(), Path.Combine("html", "css"));
            string original_theme_file_path = Path.Combine(theme_folder_path, name + "-" + appearance_name + ".css");
            string active_theme_file_path = Path.Combine(theme_folder_path, "spixiui.css");

            System.IO.File.Copy(original_theme_file_path, active_theme_file_path, true);

            activeTheme = name;
            activeAppearance = appearance;

            Application.Current.Properties["appearance"] = (int)activeAppearance;
            Application.Current.SavePropertiesAsync();  // Force-save properties for compatibility with WPF

            return true;
        }

        public static bool changeAppearance(ThemeAppearance newAppearance)
        {
            return loadTheme(activeTheme, newAppearance);
        }

        public static ThemeAppearance getActiveAppearance()
        {
            return activeAppearance;
        }

    }
}

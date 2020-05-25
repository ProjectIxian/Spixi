using IXICore.Meta;
using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using Xamarin.Forms;

namespace SPIXI.Lang
{
    public static class SpixiLocalization
    {
        private static string language = "en-us";
        private static Dictionary<string, string> localizedStrings = new Dictionary<string, string>();

        public static bool loadLanguage(string lang)
        {
            string lang_file_name = Path.Combine("lang", lang + ".txt");
            if (!File.Exists(lang_file_name))
            {
                Logging.error("Unknown language " + lang);
                return false;
            }

            language = lang;
            localizedStrings.Clear();

            StreamReader sr = File.OpenText(lang_file_name);
            string last_key = "";

            while(!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if(line == "" || line.StartsWith(";"))
                {
                    continue;
                }
                last_key = line.Substring(0, line.IndexOf("=")).Trim();
                string value = line.Substring(line.IndexOf("=") + 1).Trim();
                localizedStrings.Add(last_key, value);
            }

            sr.Close();
            sr.Dispose();

            return false;
        }

        public static string getLocalizedString(string key)
        {
            if(localizedStrings.Count == 0)
            {
                loadLanguage(language);
            }
            if(localizedStrings.ContainsKey(key))
            {
                return localizedStrings[key];
            }
            return null;
        }

        public static string _SL(string key)
        {
            return getLocalizedString(key);
        }

        public static string getCurrentLanguage()
        {
            return language;
        }

        public static void localizeHtml(string html_file_path, string localized_file_path)
        {
            if(!File.Exists(html_file_path))
            {
                Logging.error("HTML File doesn't exist: " + html_file_path);
                return;
            }
            StreamReader sr = File.OpenText(html_file_path);
            StreamWriter sw = File.CreateText(localized_file_path);

            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if(line == "")
                {
                    continue;
                }
                while (line.Contains("*SL{"))
                {
                    string key = line.Substring(line.IndexOf("*SL{") + 4);
                    key = key.Substring(0, key.IndexOf("}"));
                    string value = _SL(key);
                    if (value == null)
                    {
                        Logging.error("Unknown localization key; " + key);
                        value = "";
                    }
                    line = line.Replace("*SL{" + key + "}", value);
                }
                sw.WriteLine(line);
            }

            sr.Close();
            sr.Dispose();

            sw.Flush();
            sw.Close();
            sw.Dispose();
        }
    }
}

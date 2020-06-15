using IXICore.Meta;
using SPIXI.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using Xamarin.Forms;

namespace SPIXI.Lang
{
    public static class SpixiLocalization
    {
        private static List<string> languages = new List<string> {
            "en-us",
            "de-de",
            "fr-fr",
            "sl-si"
        };

        private static bool loaded = false;
        private static string language = "en-us";
        private static Dictionary<string, string> localizedStrings = new Dictionary<string, string>();
        private static Dictionary<string, string> customStrings = new Dictionary<string, string>();

        public static bool loadLanguage(string lang)
        {
            loaded = false;

            Stream file_stream = null;
            try
            {
                string lang_file_path = "";
                if (languages.Contains(lang))
                {
                    lang_file_path = Path.Combine("lang", lang + ".txt");
                }
                else
                {
                    string lang_part = lang.Substring(0, lang.IndexOf('-'));
                    string found_lang_part = languages.Find(x => x.StartsWith(lang_part));
                    if (found_lang_part != null)
                    {
                        lang_file_path = Path.Combine("lang", found_lang_part + ".txt");
                    }
                }
                if(lang_file_path != "")
                {
                    file_stream = DependencyService.Get<IPlatformUtils>().getAsset(lang_file_path);
                }
            }
            catch(Exception)
            {
                file_stream = null;
            }
            if (file_stream == null)
            {
                Logging.error("Unknown language " + lang);
                return false;
            }

            Dictionary<string, string> localized_strings = new Dictionary<string, string>(customStrings);

            StreamReader sr = new StreamReader(file_stream);
            string last_key = "";

            while(!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if(line == "" || line.StartsWith(";"))
                {
                    continue;
                }

                int sep_index = line.IndexOf("=");
                if(sep_index == -1)
                {
                    return false;
                }

                last_key = line.Substring(0, sep_index).Trim();
                string value = line.Substring(sep_index + 1).Trim();
                if(last_key == "" || value == "")
                {
                    return false;
                }
                localized_strings.Add(last_key, value);
            }

            sr.Close();
            sr.Dispose();

            file_stream.Close();
            file_stream.Dispose();

            loaded = true;
            localizedStrings = localized_strings;
            language = lang;

            return true;
        }

        public static void addCustomString(string key, string value)
        {
            customStrings.AddOrReplace(key, value);
            localizedStrings.AddOrReplace(key, value);
        }

        public static string getLocalizedString(string key)
        {
            if(!loaded)
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

        public static string localizeHtml(Stream stream)
        {
            StreamReader sr = new StreamReader(stream);
            string lines = "";
            while (!sr.EndOfStream)
            {
                string line = sr.ReadLine().Trim();
                if (line == "")
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
                lines += line + "\n";
            }

            sr.Close();
            sr.Dispose();

            return lines;
        }
    }
}

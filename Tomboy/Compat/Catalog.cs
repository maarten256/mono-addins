// ---------------------------------------------------------------------------------
// Copyright (c) 2025 Maarten Jacobs
// All rights reserved.
// This file is part of tomboy-modern and was created specifically for this project.
// ---------------------------------------------------------------------------------
using System.Globalization;
using System.Resources;

namespace Tomboy.Compat
{
    public static class Catalog
    {
        private static ResourceManager _resourceManager;
        private static string _package;

        public static void Init(string package, string localeDir = null)
        {
            // Save package for debugging
            _package = package;

            // You can map package to resource name, or just use fixed string
            _resourceManager = new ResourceManager("Tomboy.Strings", typeof(Catalog).Assembly);

            // Optional: use localeDir for custom cultures — ignored for now
        }

        public static string GetString(string key)
        {
            if (_resourceManager == null)
            {
                // Fallback: identity function
                return key;
            }

            try
            {
                string result = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
                return string.IsNullOrEmpty(result) ? key : result;
            }
            catch
            {
                return key;
            }
        }

        public static string GetString(string key, params object[] args)
        {
            string format = GetString(key);
            return string.Format(format, args);
        }

        public static string GetPluralString(string singular, string plural, int count)
        {
            return (count == 1) ? singular : plural;
        }
    }
}

using System;
using System.Configuration;

namespace isBetradarMTS.Utilities
{
    public static class AppSettings
    {
        /// <summary>
        /// if not given in app setting default is 3
        /// </summary>
        public static int EventRequestRetryCount
        {
            get
            {
                string strCount = GetAppSettings("EventRequestRetryCount");
                return string.IsNullOrEmpty(strCount) ? 3 : Convert.ToInt32(strCount);
            } 
        }
        public static string GetDefaultDbConnectionString()
        {
             return ConfigurationManager.ConnectionStrings["mysqlConnection"].ConnectionString;
        }
        public static string GetDbConnectionString(string key)
        {
            if (key == string.Empty)
                return ConfigurationManager.ConnectionStrings["mysqlConnection"].ConnectionString;
            else
                return ConfigurationManager.ConnectionStrings[key].ConnectionString;
        }
        public static string GetAppSettings(string key)
        {
            return ConfigurationManager.AppSettings[key].ToString();
        }
    }
}

using isBetradarMTS.Utilities;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace isBetradarMTS
{
    class Program
    {
        /// <summary>
        /// A <see cref="ILog"/> instance used for execution logging
        /// </summary>
        private static ILog _log;
        //private MtsSession mtsSession;
        public static string _connection;
        /// Main entry point
        /// </summary>
        private static void Main()
        {
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            //configure log4net
            XmlConfigurator.Configure();
            _log.Info("In program.Main reading config...");
            _connection = AppSettings.GetDefaultDbConnectionString();

            _log.Info("Configuring service...");
            MtsServiceConfiguration.Configure(_log);

            //var mtsSession = new MtsSession(_log);
            //mtsSession.Start();
        }
    }
}

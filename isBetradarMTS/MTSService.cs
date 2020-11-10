using isBetradarMTS.Utilities;
using log4net;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace isBetradarMTS
{
    public class MTSService
    {
        private readonly ILog _log;

        private MtsSession mtsSession;
        public MTSService(ILog log)
        {
            _log = log;

        }
        public void Start()
        {
            _log.Info("MTS Service started successfully...");

            _log.Info("Initializing and starting MTS-session...");

            mtsSession = new MtsSession(_log);

            mtsSession.Start();
        }
        public void Stop()
        {
            mtsSession.Stop();
            _log.Info("MTS Service stoped...");
        }
    }
}

using log4net;
using Topshelf;

namespace isBetradarMTS
{
    internal static class MtsServiceConfiguration
    {
        internal static void Configure(ILog logger)
        {
            HostFactory.Run(configure =>
            {
                configure.Service<MTSService>(service =>
                {
                    service.ConstructUsing(s => new MTSService(logger));
                    service.WhenStarted(s => s.Start());
                    service.WhenStopped(s => s.Stop());
                });
                //Setup Account that window service use to run.  
                configure.RunAsLocalSystem();
                configure.SetServiceName("MTSService");
                configure.SetDisplayName("isBetradarMTSService");
                configure.SetDescription("Betradar MTS Service");
            });
        }
    }
}

using System.ServiceProcess;

namespace Upsmile.Sync.Initiator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var ServicesToRun = new ServiceBase[] 
                                              { 
                                                  new USInWinService() 
                                              };
            ServiceBase.Run(ServicesToRun);
        }
    }
}

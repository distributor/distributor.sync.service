using System.ServiceProcess;

namespace Upsmile.Sync.Executant
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            var servicesToRun = new ServiceBase[] 
                                              { 
                                                  new USExWinService() 
                                              };
            ServiceBase.Run(servicesToRun);
        }
    }
}

using System;
using System.ServiceProcess;

namespace WhiteListService
{
    static class Program
    {
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new WhiteListService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}

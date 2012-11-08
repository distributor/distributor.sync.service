using System;
using System.Data.EntityClient;
using System.Data.SqlClient;
using System.ServiceModel;
using SyncServicesModel;
using Upsmile.Sync.Initiator;

namespace CheckService
{
    class Program
    {
        static void Main()
        {
        //    Console.WriteLine("Press any ley to start");
        //    Console.ReadLine();
        //    try
        //    {
        //        var lEndPointAddress =
        //            new EndpointAddress(new Uri("net.tcp://62.122.56.156:9000/Upsmile.Sync.Executant/USExService.svc"));
        //        Console.WriteLine("CallSyncService: lEndPointAddress = {0}", lEndPointAddress);

        //        using (var factory = new ChannelFactory<IUSInService>(new NetTcpBinding(), lEndPointAddress))
        //        {
        //            try
        //            {
        //                Console.WriteLine("CallSyncService: ChannelFactory created");
        //                IUSInService service = factory.CreateChannel();
        //                Console.WriteLine("factory.CreateChannel created");
        //                Console.WriteLine("Введите ID LinkSyncEntity для запуска синхронизации");
        //                var s = Console.ReadLine();
        //                var lLinkSyndcId = Convert.ToInt32(s);
        //                string lResult = service.EntitySync(lLinkSyndcId);
        //                Console.WriteLine("CallSyncService: service.EntitySync выполнен");
        //                factory.Close();
        //                Console.WriteLine("CallSyncService: ChannelFactory Closed");
        //                if (lResult != string.Empty)
        //                {
        //                    Console.WriteLine("CallSyncService: Возникла ошибка во время синхронизации LinkId = {0}", lResult);
        //                }
        //            }
        //            finally
        //            {
        //                factory.Abort();
        //            }                
        //        }

        //        //ChannelFactory<IUSInService> factory = new ChannelFactory<IUSInService>(new NetTcpBinding(), lEndPointAddress);
        //        //try
        //        //{
        //        //    Console.WriteLine("CallSyncService: ChannelFactory created");
        //        //    IUSInService service = factory.CreateChannel();
        //        //    Console.WriteLine("factory.CreateChannel created");
        //        //    Console.WriteLine("Введите ID LinkSyncEntity для запуска синхронизации");
        //        //    var s = Console.ReadLine();
        //        //    var lLinkSyndcId = Convert.ToInt32(s);
        //        //    string lResult = service.EntitySync(lLinkSyndcId);
        //        //    Console.WriteLine("CallSyncService: service.EntitySync выполнен");
        //        //    factory.Close();
        //        //    Console.WriteLine("CallSyncService: ChannelFactory Closed");
        //        //    if (lResult != string.Empty)
        //        //    {
        //        //        Console.WriteLine("CallSyncService: Возникла ошибка во время синхронизации LinkId = {0}", lResult);
        //        //    }
        //        //}
        //        //finally
        //        //{
        //        //    factory.Abort();
        //        //}                
        //        Console.WriteLine("Press any ley to finish");
        //        Console.ReadLine();
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine("Error: {0}", e);
        //        Console.WriteLine("Error: {0}",e.InnerException);
        //        Console.WriteLine("Press any ley to finish");
        //        Console.ReadLine();
        //    }

                SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();
                sqlBuilder.UserID = "distrdev";
                sqlBuilder.DataSource = "SWCOPY";
                sqlBuilder.Password = "distrdev";
                string ProviderString = sqlBuilder.ToString();

                EntityConnectionStringBuilder entBuilder = new EntityConnectionStringBuilder();
                entBuilder.Provider = "Devart.Data.Oracle";
                entBuilder.ProviderConnectionString = ProviderString;
                entBuilder.Metadata = @"res://*/emSyncServices.csdl|
                                        res://*/emSyncServices.ssdl|
                                        res://*/emSyncServices.msl";

                string lDbConnectionString = entBuilder.ToString();
                
                //string lDbConnectionString = Properties.Settings.Default.DBConnectionString;
                using (var lData = new SyncServicesEntities(lDbConnectionString))
                {
                    var lresXML = string.Empty;
                    var lErrorMsg = string.Empty;
                    var lres = lData.GET_ENTITY_DATA(91038, 1, ref lresXML, ref lErrorMsg);
                    Console.WriteLine(lresXML);
                    Console.WriteLine(lErrorMsg);                        
                }
        }
    }
}

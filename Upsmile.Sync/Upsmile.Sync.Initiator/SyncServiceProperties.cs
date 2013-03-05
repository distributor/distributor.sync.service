using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Microsoft.Samples.ChunkingChannel;
using Upsmile.Sync.BasicClasses.ExtensionMethods;

namespace Upsmile.Sync.Initiator
{
    /// <summary>
    /// класс отвечающий за синхронизацию сущность
    /// </summary>
    class SyncServiceProperties : IUSLogBasicClass
    {
        private delegate string CallSyncServiceDelegate();

        private Mutex mut;

        private string CallSyncService()
        {
            string lResult = string.Empty;

            if (mut.WaitOne(0))
            {
                try
                {
                    var lServiceHostAddress = new Uri(Properties.Settings.Default.ServiceHostAddress);
                    var lServiceEndPointAddress = Properties.Settings.Default.ServiceEndPointAddress;
                    var lEndPointAddress =
                        new EndpointAddress(new Uri(String.Format("{0}/{1}", lServiceHostAddress, lServiceEndPointAddress)));
                    this.WriteLog(USLogLevel.Trace, "SyncServiceProperties.CallSyncService: lEndPointAddress = {0}", lEndPointAddress);

                    var lBinding = new TcpChunkingBinding();
                    lBinding.OpenTimeout = Properties.Settings.Default.OpenTimeout;
                    lBinding.ReceiveTimeout = Properties.Settings.Default.ReceiveTimeout;
                    lBinding.SendTimeout = Properties.Settings.Default.SendTimeout;
                    lBinding.CloseTimeout = Properties.Settings.Default.CloseTimeout;

                    this.WriteLog(USLogLevel.Trace, "lBinding params lBinding.OpenTimeout = {0}; lBinding.ReceiveTimeout = {1}; lBinding.SendTimeout = {2}; lBinding.CloseTimeout = {3}", lBinding.OpenTimeout, lBinding.ReceiveTimeout, lBinding.SendTimeout, lBinding.CloseTimeout);

                    using (var factory = new ChannelFactory<IUSInService>(lBinding, lEndPointAddress))
                    {
                        try
                        {
                            this.WriteLog(USLogLevel.Trace, "SyncServiceProperties.CallSyncService: ChannelFactory created");
                            var service = factory.CreateChannel();
                            this.WriteLog(USLogLevel.Trace, "SyncServiceProperties.CallSyncService: IUSExService service = factory.CreateChannel created");
                            lResult = service.EntitySync(LinkSyncServiceEntitiesId);
                            this.WriteLog(USLogLevel.Trace, "SyncServiceProperties.CallSyncService: service.EntitySync выполнен");
                            factory.Close();
                        }
                        finally
                        {
                            factory.Abort();
                        }
                    }
                    this.WriteLog(USLogLevel.Trace, "SyncServiceProperties.CallSyncService: ChannelFactory Closed");
                }
                catch (Exception e)
                {
                    this.WriteLog(USLogLevel.Trace|USLogLevel.Debug, string.Format("SyncServiceProperties.CallSyncService: Ошибка синхронизации LinkSyncServiceEntitiesId = {0}. Ошибка: {1}", LinkSyncServiceEntitiesId, e));
                    this.WriteLogException(string.Format("SyncServiceProperties.CallSyncService: Ошибка синхронизации LinkSyncServiceEntitiesId = {0}. Ошибка: {1}", LinkSyncServiceEntitiesId, e), e);
                    lResult = string.Empty;
                }
                finally
                {
                    mut.ReleaseMutex();                    
                }
            }

            return lResult;
        }
        
        public double LinkSyncServiceEntitiesId { get; set; }

        public SyncServiceProperties(double aLinkSyncServiceEntitiesId)
        {

            LinkSyncServiceEntitiesId = aLinkSyncServiceEntitiesId;
            mut = new Mutex(false, string.Format("LinkSyncServiceEntitiesId_{0}", LinkSyncServiceEntitiesId));
        }
        
        public void SyncData()
        {
            var dlgt = new CallSyncServiceDelegate(CallSyncService); 
            dlgt.BeginInvoke(null, null);            
        }
    }

    class SyncServiceManager
    {
        private List<SyncServiceProperties> _syncServiceProperties = new List<SyncServiceProperties>();

        public void CallSyncData(double aLinkSyncServiceEntitiesId)
        {
            var lIndex = -1;
            // поиск синхронизируемого метода в структуре
            lock (_syncServiceProperties)
            {
                var q = from sp in _syncServiceProperties
                        where (sp.LinkSyncServiceEntitiesId == aLinkSyncServiceEntitiesId)
                        select sp;
                
                if (q.ToList().Count == 0)
                {
                    // нет такой строки
                    _syncServiceProperties.Add(new SyncServiceProperties(aLinkSyncServiceEntitiesId));
                }

                foreach (var lItem in q.ToList())
                {
                    lIndex = _syncServiceProperties.IndexOf(lItem);
                }                    
            }

            if (lIndex != -1)
            {
                _syncServiceProperties[lIndex].SyncData();
            }
        }
    }
}

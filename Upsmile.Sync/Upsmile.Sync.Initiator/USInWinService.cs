using System;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Samples.ChunkingChannel;
using SyncServicesModel;
using System.Data.SqlClient;
using System.Data.EntityClient;
using Upsmile.Sync.BasicClasses.ExtensionMethods;


namespace Upsmile.Sync.Initiator
{
    public partial class USInWinService : ServiceBase, IUSLogBasicClass
    {
        private ServiceHost _host;
        Timer _timer = new Timer();
        // менеджер синхронизации
        private SyncServiceManager _syncServiceManager = new SyncServiceManager();

        public USInWinService()
        {
            InitializeComponent();
            double lRunSyncFrequency = Properties.Settings.Default.RunSyncFrequency;
            _timer.Elapsed += TimerElapsed;
            _timer.Interval = lRunSyncFrequency;
            this.WriteLog(USLogLevel.Trace, "USInWinService: Частота запуска = {0} сек", lRunSyncFrequency / 1000);
        }
        private string GetConnectionString()
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();
            sqlBuilder.MaxPoolSize = Properties.Settings.Default.MaxPoolSize;
            sqlBuilder.Pooling = Properties.Settings.Default.Pooling;
            sqlBuilder.UserID = Properties.Settings.Default.UserID;
            sqlBuilder.DataSource = Properties.Settings.Default.DataSource;
            sqlBuilder.Password = Properties.Settings.Default.Password;
            string ProviderString = sqlBuilder.ToString();

            EntityConnectionStringBuilder entBuilder = new EntityConnectionStringBuilder();
            entBuilder.Provider = "Devart.Data.Oracle";
            entBuilder.ProviderConnectionString = ProviderString;
            entBuilder.Metadata = @"res://*/emSyncServices.csdl|
                                        res://*/emSyncServices.ssdl|
                                        res://*/emSyncServices.msl";
            return entBuilder.ToString();
        }

        void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            //запукск сервиса
            try
            {
                // идем по списку сущностей которые нужно синхронизировать и запускаем синхронизацию
                using (var lData = new SyncServicesEntities(GetConnectionString()))
                {
                    lData.Connection.Open();

                    var query = from v in lData.VLinkSyncSerEntNeededs
                                select v;
                    foreach (var lItem in query)
                    {
                        _syncServiceManager.CallSyncData(lItem.ID);
                    }
                    lData.Connection.Close();
                }
            }
            catch (Exception ex)
            {
                this.WriteLogException(Newtonsoft.Json.JsonConvert.SerializeObject(ex), ex);
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                this.WriteLog(USLogLevel.Debug, "USInWinService: Инициализирован старт сервиса");

                // задаем адрес хоста из прараметров
                var lServiceHostAddress = new Uri(Properties.Settings.Default.ServiceHostAddress);
                // задаем адрес конечной точки из параметров
                var lServiceEndPointAddress = Properties.Settings.Default.ServiceEndPointAddress;
                this.WriteLog(USLogLevel.Trace, "USInWinService: lServiceHostAddress = {0}; lServiceEndPointAddress = {1}", lServiceHostAddress, lServiceEndPointAddress);

                // создаем хост
                _host = new ServiceHost(typeof(USInService), lServiceHostAddress);
                this.WriteLog(USLogLevel.Trace, "USInWinService: ServiceHost created");
                // добавляем точку доступа
                var lBinding = new TcpChunkingBinding();
                lBinding.OpenTimeout = Properties.Settings.Default.OpenTimeout;
                lBinding.ReceiveTimeout = Properties.Settings.Default.ReceiveTimeout;
                lBinding.SendTimeout = Properties.Settings.Default.SendTimeout;
                lBinding.CloseTimeout = Properties.Settings.Default.CloseTimeout;
                this.WriteLog(USLogLevel.Trace, "lBinding params lBinding.OpenTimeout = {0}; lBinding.ReceiveTimeout = {1}; lBinding.SendTimeout = {2}; lBinding.CloseTimeout = {3}", lBinding.OpenTimeout, lBinding.ReceiveTimeout, lBinding.SendTimeout, lBinding.CloseTimeout);

                _host.AddServiceEndpoint(typeof(IUSInService), lBinding, lServiceEndPointAddress);
                this.WriteLog(USLogLevel.Trace, "USInWinService: Service End Point created");

                var lBehavior = _host.Description.Behaviors.Find<ServiceDebugBehavior>();
                if (lBehavior != null)
                {
                    lBehavior.IncludeExceptionDetailInFaults = true;
                    this.WriteLog(USLogLevel.Trace, "USInWinService: Задано ServiceDebugBehavior.IncludeExceptionDetailInFaults = True");
                }
                else
                {
                    this.WriteLog(USLogLevel.Debug, "USInWinService: ServiceDebugBehavior не определен");
                }

                // добавляем mex точку доступа
                this.WriteLog(USLogLevel.Trace, "USInWinService: Создаем MEX точку доступа");
                _host.Description.Behaviors.Add(new ServiceMetadataBehavior());
                this.WriteLog(USLogLevel.Trace, "USInWinService: MEX ServiceMetadataBehavior added");
                var mexBinding = MetadataExchangeBindings.CreateMexTcpBinding();
                this.WriteLog(USLogLevel.Trace, "USInWinService: MEX TcpBinding added");
                _host.AddServiceEndpoint(typeof(IMetadataExchange), mexBinding, "MEX");
                this.WriteLog(USLogLevel.Trace, "USInWinService: MEX точка доступа создана");
                // открываем хост
                _host.Open();
                this.WriteLog(USLogLevel.Debug, "USInWinService: Сервис запущен успешно");
            }
            catch (Exception e)
            {
                this.WriteLogException(string.Format("USInWinService: Ошибка запуска сервиса. Ошибка: {0}", e), e);
                throw;
            }
               
            _timer.Start();
        }

        protected override void OnStop()
        {
            // остановка таймера
            _timer.Close();
            this.WriteLog(USLogLevel.Debug, "USInWinService: Остановка Win сервиса");
            try
            {
                // закрываем WCF-сервис если он открыт
                if ((_host != null) && (_host.State == CommunicationState.Opened)) _host.Close();
                this.WriteLog(USLogLevel.Debug, "USInWinService: Сервис остановлен успешно");                
            }
            catch (Exception e)
            {
                this.WriteLogException(string.Format("USInWinService: Ошибка остановки сервиса. Ошибка: {0}", e), e);
                throw;
            }

        }
    }
}

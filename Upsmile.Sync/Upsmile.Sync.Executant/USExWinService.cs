// installutil "D:\Upsmile.Sync\Upsmile.Sync.Executant\bin\Debug\Upsmile.Sync.Executant.exe"
using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using Microsoft.Samples.ChunkingChannel;
using Upsmile.Sync.BasicClasses.ExtensionMethods;

namespace Upsmile.Sync.Executant
{
    public partial class USExWinService : ServiceBase, IUSLogBasicClass
    {
        private ServiceHost _host;

        public USExWinService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            this.WriteLog(USLogLevel.Debug, "USExWinService: Инициализирован старт сервиса");
            try
            {
                // создаем и запускаем WCF-сервис
                
                // задаем адрес хоста из прараметров
                Uri lServiceHostAddress = new Uri(Properties.Settings.Default.ServiceHostAddress);
                // задаем адрес конечной точки из параметров
                string lServiceEndPointAddress = Properties.Settings.Default.ServiceEndPointAddress;
                this.WriteLog(USLogLevel.Trace, "USExWinService: lServiceHostAddress = {0}; lServiceEndPointAddress = {1}", lServiceHostAddress, lServiceEndPointAddress);

                // создаем хост
                _host = new ServiceHost(typeof(USExService), lServiceHostAddress);
                this.WriteLog(USLogLevel.Trace, "USExWinService: ServiceHost created");

                var lBinding =new TcpChunkingBinding();
                lBinding.OpenTimeout = Properties.Settings.Default.OpenTimeout;
                lBinding.ReceiveTimeout = Properties.Settings.Default.ReceiveTimeout;
                lBinding.SendTimeout = Properties.Settings.Default.SendTimeout;
                lBinding.CloseTimeout = Properties.Settings.Default.CloseTimeout;

                this.WriteLog(USLogLevel.Trace, "lBinding params lBinding.OpenTimeout = {0}; lBinding.ReceiveTimeout = {1}; lBinding.SendTimeout = {2}; lBinding.CloseTimeout = {3}", lBinding.OpenTimeout, lBinding.ReceiveTimeout, lBinding.SendTimeout, lBinding.CloseTimeout);
                
                // добавляем точку доступа
                _host.AddServiceEndpoint(typeof(IUSExService), lBinding, lServiceEndPointAddress);
                this.WriteLog(USLogLevel.Trace, "USExWinService: Service End Point created");
                
                var lBehavior = _host.Description.Behaviors.Find<ServiceDebugBehavior>();
                if (lBehavior != null)
                {
                    lBehavior.IncludeExceptionDetailInFaults = true;
                    this.WriteLog(USLogLevel.Trace, "USExWinService: Задано ServiceDebugBehavior.IncludeExceptionDetailInFaults = True");
                }
                else
                {
                    this.WriteLog(USLogLevel.Debug, "USExWinService: ServiceDebugBehavior не определен");
                }
                
                // добавляем mex точку доступа
                this.WriteLog(USLogLevel.Trace, "USExWinService: Создаем MEX точку доступа");
                _host.Description.Behaviors.Add(new ServiceMetadataBehavior());
                this.WriteLog(USLogLevel.Trace, "USExWinService: MEX ServiceMetadataBehavior added");
                var mexBinding = MetadataExchangeBindings.CreateMexTcpBinding();
                this.WriteLog(USLogLevel.Trace, "USExWinService: MEX TcpBinding added");
                _host.AddServiceEndpoint(typeof(IMetadataExchange), mexBinding, "MEX");
                this.WriteLog(USLogLevel.Trace, "USExWinService: MEX точка доступа создана");
                
                // открываем хост
                _host.Open();
                this.WriteLog(USLogLevel.Debug, "USExWinService: Сервис запущен успешно");
            }
            catch (Exception e)
            {
                this.WriteLogException(e.ToString(), e);
                throw;
            }
        }

        protected override void OnStop()
        {
            this.WriteLog(USLogLevel.Debug, "USExWinService: Инициализировано закрытие сервиса");
            try
            {
                // закрываем WCF-сервис если он открыт
                if ((_host != null) && (_host.State == CommunicationState.Opened)) _host.Close();
                this.WriteLog(USLogLevel.Debug, "USExWinService: Сервис остановлен успешно");
            }
            catch (Exception e)
            {
                this.WriteLogException(e.ToString(), e);
            }
        }   
    }
}

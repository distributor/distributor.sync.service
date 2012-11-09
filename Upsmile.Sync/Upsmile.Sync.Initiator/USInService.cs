namespace Upsmile.Sync.Initiator
{

    using BasicClasses.ExtensionMethods;
    using NUnit.Framework;
    using NLog;
    using BasicClasses;

    /// <summary>
    /// Синхронизация данных между БД
    /// Сервис вызываемый на ЦО (сервис-инициатор)
    /// </summary>
    public class USInService : IUSInService, IUSLogBasicClass
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public string EntitySync(double aLinkSyncServiceEntitiesId, bool aIsFullSync = false)
        {
            USServiceHelper.BeginSync += USServiceHelper_BeginSync;
            USServiceHelper.EndSync += USServiceHelper_EndSync;
            USServiceHelper.ErrorSync += USServiceHelper_ErrorSync;
            return USServiceHelper.EntitySync(aLinkSyncServiceEntitiesId, aIsFullSync);
        }

        /// <summary>
        /// Возникла ошибка выполнения синхронизации
        /// </summary>
        /// <param name="argument"></param>
        static void USServiceHelper_ErrorSync(SyncEventArgument argument)
        {
            argument.With(x => x.Exception)
                .Do(x => _logger.Debug(Newtonsoft.Json.JsonConvert.SerializeObject(x)));
            argument.With(x => x.Result)
                .Do(x => _logger.Debug(x.ToString()));
        }

        /// <summary>
        /// Синхронизация завершена
        /// </summary>
        /// <param name="argument"></param>
        static void USServiceHelper_EndSync(SyncEventArgument argument)
        {
            argument.With(x => x.Exception)
                .Do(x => _logger.Debug(Newtonsoft.Json.JsonConvert.SerializeObject(x)));
            argument.With(x => x.Result)
                .Do(x => _logger.Trace(x.ToString()));
        }

        /// <summary>
        /// Синхронизация началась
        /// </summary>
        /// <param name="argument"></param>
        static void USServiceHelper_BeginSync(SyncEventArgument argument)
        {
            argument.With(x => x.Exception)
                .Do(x => _logger.Debug(Newtonsoft.Json.JsonConvert.SerializeObject(x)));
            argument.With(x => x.Result)
                .Do(x => _logger.Trace(x.ToString()));
        }
    }

    [TestFixture]
    public class Test_USInService
    {
        /// <summary>
        /// Тест проверки синхронизации
        /// </summary>
        [Test]
        public void EntitySyncTest()
        {
            var result = USServiceHelper.EntitySync(64019);
            Assert.IsNotNullOrEmpty(result);
        }
    }
}

namespace Upsmile.Sync.Initiator
{
    using System;
    using System.Data.EntityClient;
    using System.Data.SqlClient;
    using System.IO;
    using System.ServiceModel;
    using System.Text;
    using Microsoft.Samples.ChunkingChannel;
    using NLog;
    using Newtonsoft.Json;
    using SyncServicesModel;
    using BasicClasses;

    /// <summary>
    /// Умправление процессом синхронизации
    /// </summary>
    public static class USServiceHelper
    {
        /// <summary>
        /// Событие начала запуска синхронизации
        /// </summary>
        public static event EventDelegate  BeginSync;

        /// <summary>
        /// Событие начала запуска синхронизации
        /// </summary>
        public static void OnBeginSync(SyncEventArgument argument)
        {
            var handler = BeginSync;
            if (handler != null) handler(argument);
        }
        
        /// <summary>
        /// Событие окончания синхронизации
        /// </summary>
        public static event EventDelegate EndSync;

        /// <summary>
        /// Событие окончания запуска синхронизации
        /// </summary>
        public static void OnEndSync(SyncEventArgument argument)
        {
            var handler = EndSync;
            if (handler != null) handler(argument);
        }

        /// <summary>
        /// Событие возникновение ошибки при сихронизации
        /// </summary>
        public static event EventDelegate ErrorSync;

        /// <summary>
        /// Событие возникновение ошибки при сихронизации
        /// </summary>
        public static void OnErrorSync(SyncEventArgument argument)
        {
            var handler = ErrorSync;
            if (handler != null) handler(argument);
        }

        /// <summary>
        /// Логирование операций
        /// </summary>
        public static Logger _logger = LogManager.GetCurrentClassLogger();

        // идентификатор строки из LINK_SYNC_SERVICES_ENTITIES
        private static double LinkSyncServEntId { get; set; }
        
        /// <summary>
        /// Параметры синхронизации
        /// </summary>
        private static USSyncParams _syncParams;

        /// <summary>
        /// Тип синхронизации (полная(true)/только изменения(false)
        /// </summary>
        private static bool IsFullSync { get; set; }

        public static string GetConnectionString()
        {
            var sqlBuilder = new SqlConnectionStringBuilder
                                 {
                                     MaxPoolSize = Properties.Settings.Default.MaxPoolSize,
                                     Pooling = Properties.Settings.Default.Pooling,
                                     UserID = Properties.Settings.Default.UserID,
                                     DataSource = Properties.Settings.Default.DataSource,
                                     Password = Properties.Settings.Default.Password
                                 };
            var ProviderString = sqlBuilder.ToString();

            var entBuilder = new EntityConnectionStringBuilder
                                 {
                                     Provider = "Devart.Data.Oracle",
                                     ProviderConnectionString = ProviderString,
                                     Metadata = @"res://*/emSyncServices.csdl|
                                        res://*/emSyncServices.ssdl|
                                        res://*/emSyncServices.msl"
                                 };
            return entBuilder.ToString();
        }

        /// <summary>
        /// Метод синхронизации
        /// </summary>
        /// <param name="aLinkSyncServiceEntitiesId"></param>
        /// <param name="aIsFullSync"></param>
        /// <returns></returns>
        public static string EntitySync(double aLinkSyncServiceEntitiesId, bool aIsFullSync = false)
        {
            var beginArgument = new SyncEventArgument {Exception = null, Result = aLinkSyncServiceEntitiesId};
            OnBeginSync(beginArgument);
            
            _logger.Trace("EntitySync: Начата синхронизация: {0}", aLinkSyncServiceEntitiesId);
            LinkSyncServEntId = aLinkSyncServiceEntitiesId;
            IsFullSync = aIsFullSync;
            var lResult = string.Empty;
            try
            {
                using (var lData = new SyncServicesEntities(GetConnectionString()))
                {
                    lData.Connection.Open();
                    try
                    {
                        var lSyncDateTime = lData.GET_SYSDATE();
                        _syncParams = lData.GetSyncServiceData(aLinkSyncServiceEntitiesId);
                        if (_syncParams == null)
                        {
                            var ex = new NullReferenceException(lResult);
                            OnEndSync(new SyncEventArgument{Exception =ex,Result = lResult});
                            return lResult;
                        }
                        var lIteratorNumber = 1;
                        var lErrorMsg = string.Empty;
                        var lJSonElements = string.Empty;
                        int lSyncResult;
                        do
                        {
                            switch (Convert.ToInt16(lData.GetLinkSyncServEntData(aLinkSyncServiceEntitiesId, lIteratorNumber, ref lJSonElements, ref lErrorMsg)))
                            {
                                case 0: // ошибка
                                    lSyncResult = 0;
                                    lResult = lErrorMsg;
                                    break;
                                case 1: // успешное получение данных. 
                                    lSyncResult = 1;
                                    if (lJSonElements != string.Empty)
                                    {
                                        // успешное получение данных. данные не пустые. запускаем синхронизацию
                                        // формируем данные для передачи на филиал
                                        var lInData = new USInServiceValues
                                                          {
                                                              EntityTypeId = _syncParams.ElsysTypeId,
                                                              JsonEntityData = Convert.ToString(lJSonElements)
                                                          };
                                        // серриализация данных для передачи на филиал
                                        var lJsonInData = JsonConvert.SerializeObject(lInData);
                                        _logger.Trace("EntitySync: Кодировка на клиенте {0}", Encoding.Default.EncodingName);
                                        // создаем поток который будет передан на филиал
                                        var DBEncoding = Encoding.GetEncoding(Properties.Settings.Default.DBEncodingName);
                                        var TransmissionEncoding = Encoding.GetEncoding(Properties.Settings.Default.TransmissionEncodingName);
                                        _logger.Trace("EntitySync: Кодировка на базе {0}", DBEncoding.EncodingName);
                                        _logger.Trace("EntitySync: Кодировка передачи {0}", TransmissionEncoding.EncodingName);
                                        using (var ms = new MemoryStream(Encoding.Convert(DBEncoding, TransmissionEncoding, DBEncoding.GetBytes(lJsonInData))))
                                        {
                                            var lBinding = new TcpChunkingBinding
                                                               {
                                                                   OpenTimeout = Properties.Settings.Default.OpenTimeout,
                                                                   ReceiveTimeout =
                                                                       Properties.Settings.Default.ReceiveTimeout,
                                                                   SendTimeout = Properties.Settings.Default.SendTimeout,
                                                                   CloseTimeout = Properties.Settings.Default.CloseTimeout
                                                               };

                                            var endPointAdress = _syncParams.EndPointAddress;
                                            //endPointAdress = string.Empty;
                                            using (var factory = new ChannelFactory<IUSExService>(lBinding, new EndpointAddress(endPointAdress)))
                                            {
                                                try
                                                {
                                                    var service = factory.CreateChannel();
                                                    var lRetValuesStr = service.EntitySync(ms);
                                                    factory.Close();
                                                    var lRetValues = JsonConvert.DeserializeObject<USInServiceRetValues>(lRetValuesStr);
                                                    lSyncResult = Convert.ToInt16(lRetValues.Result);
                                                    lResult = lRetValues.ErrorMessage;
                                                }
                                                catch (Exception e)
                                                {
                                                    lResult =
                                                        string.Format("EntitySync: Синхронизация LinkSyncServiceEntitiesId = {0} закончилась с ошибкой. Ошибка: {1}",
                                                                      aLinkSyncServiceEntitiesId, e);
                                                    OnErrorSync(new SyncEventArgument {Exception = e,Result = lResult});
                                                    continue;
                                                }
                                                factory.Abort();
                                            }
                                            ms.Close();
                                        }
                                    }
                                    break;
                                case 2: // ошибки не было но данные не были получены
                                    lSyncResult = 2;
                                    lResult = lErrorMsg;
                                    OnErrorSync(new SyncEventArgument {Exception = null,Result = lResult});
                                    continue;
                                    //break;
                                default: lSyncResult = 0;
                                    lResult = string.Format("EntitySync: Синхронизация LinkSyncServiceEntitiesId = {0} закончилась с ошибкой. Не прогнозируемый результат получения данных",
                                                            aLinkSyncServiceEntitiesId);
                                    OnErrorSync(new SyncEventArgument {Exception = null,Result = lResult});
                                    continue;
                                    //break;
                            }

                            lIteratorNumber += 1;
                        }
                        while ((lSyncResult == 1) && (lJSonElements != string.Empty));

                        switch (lSyncResult)
                        {
                            case 0:
                                var s0 = string.Format("Ошибка при синхронизация сущности EntitySync: aLinkSyncServiceEntitiesId = {0} Описание {1}; Сущность {2}: {3}; Филиал {4}; Ошибка {5}",
                                              aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                              _syncParams.ElsysTypeName, _syncParams.BranchName, lResult);
                                OnEndSync(new SyncEventArgument {Exception = null,Result = s0});
                                break;
                            case 1:
                                lData.SET_LINK_SYNC_SERV_CALL_DATA(Convert.ToDecimal(LinkSyncServEntId), lSyncDateTime);

                                var s1 =string.Format("Успешная синхронизация сущности EntitySync: aLinkSyncServiceEntitiesId = {0} Описание {1}; Сущность {2}: {3}; Филиал {4}",
                                              aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                              _syncParams.ElsysTypeName, _syncParams.BranchName);
                                OnEndSync(new SyncEventArgument {Exception = null,Result = s1});
                                break;
                            case 2:
                                var s2 = string.Format("Cинхронизация сущности EntitySync не завершена: aLinkSyncServiceEntitiesId = {0} Описание {1}; Сущность {2}: {3}; Филиал {4}. Причина {5}",
                                              aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                              _syncParams.ElsysTypeName, _syncParams.BranchName, lResult);
                                OnEndSync(new SyncEventArgument {Exception = null,Result = s2});
                                lResult = string.Empty;
                                break;
                        }
                    }
                    finally
                    {
                        lData.Connection.Close();
                        lData.Dispose();
                    }
                }
            }
            catch (Exception e)
            {
                lResult =
                    string.Format("EntitySync: Синхронизация LinkSyncServiceEntitiesId = {0} закончилась с ошибкой. Ошибка: {1}",
                                  aLinkSyncServiceEntitiesId, e);
                
                OnErrorSync(new SyncEventArgument {Exception = e,Result = lResult});
            }
            OnEndSync(new SyncEventArgument {Exception = null,Result = aLinkSyncServiceEntitiesId});
            return lResult;
        }
    }
}
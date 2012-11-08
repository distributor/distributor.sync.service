using System;
using System.IO;
using System.ServiceModel;
using System.Text;
using Microsoft.Samples.ChunkingChannel;
using SyncServicesModel;
using Upsmile.Sync.BasicClasses;
using Upsmile.Sync.BasicClasses.ExtensionMethods;
using System.Data.SqlClient;
using System.Data.EntityClient;

namespace Upsmile.Sync.Initiator
{
    /// <summary>
    /// Синхронизация ланных между БД
    /// Сервис вызываемый на ЦО (сервис-инициатор)
    /// </summary>
    public class USInService : IUSInService, IUSLogBasicClass
    {
        // идентификатор строки из LINK_SYNC_SERVICES_ENTITIES
        private double LinkSyncServEntId { get; set; }
        // параметры синхронизации
        private USSyncParams _syncParams;

        // тип синхронизации (полная(true)/только изменения(false)
        private bool IsFullSync { get; set; }

        public USInService()
        {
            LinkSyncServEntId = 0;
            IsFullSync = true;
        }

        private string GetConnectionString()
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

        public string EntitySync(double aLinkSyncServiceEntitiesId, bool aIsFullSync = false)
        {
            this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, "EntitySync: Начата синхронизация: {0}", aLinkSyncServiceEntitiesId);
            LinkSyncServEntId = aLinkSyncServiceEntitiesId;
            IsFullSync = aIsFullSync;
            string lResult = string.Empty;
            
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
                            lResult = string.Format("EntitySync: aLinkSyncServiceEntitiesId = {0} параметры синхронизации не определены", aLinkSyncServiceEntitiesId);
                            this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, lResult);
                            return lResult;
                        }

                        this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, "EntitySync: aLinkSyncServiceEntitiesId = {0} Описание {1}; Сущность {2}: {3}; Филиал {4}",
                                aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                _syncParams.ElsysTypeName, _syncParams.BranchName);
                        var lIteratorNumber = 1;
                        var lErrorMsg = string.Empty;
                        var lJSonElements = string.Empty;
                        int lSyncResult;

                        do
                        {
                            this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} данные по сущности получены и сериализованы. Шаг {1}", aLinkSyncServiceEntitiesId, lIteratorNumber);
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
                                        string lJsonInData = Newtonsoft.Json.JsonConvert.SerializeObject(lInData);
                                        if (Properties.Settings.Default.NeedLogSyncData)
                                        {
                                            this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} lJsonInData сформирован = {1}", aLinkSyncServiceEntitiesId, lJsonInData);
                                        }

                                        this.WriteLog(USLogLevel.Trace, "EntitySync: Кодировка на клиенте {0}", Encoding.Default.EncodingName);
                                        // создаем поток который будет передан на филиал
                                        var DBEncoding = Encoding.GetEncoding(Properties.Settings.Default.DBEncodingName);
                                        var TransmissionEncoding = Encoding.GetEncoding(Properties.Settings.Default.TransmissionEncodingName);
                                        this.WriteLog(USLogLevel.Trace, "EntitySync: Кодировка на базе {0}", DBEncoding.EncodingName);
                                        this.WriteLog(USLogLevel.Trace, "EntitySync: Кодировка передачи {0}", TransmissionEncoding.EncodingName);

                                        using (var ms = new MemoryStream(Encoding.Convert(DBEncoding, TransmissionEncoding, DBEncoding.GetBytes(lJsonInData))))
                                        {
                                            this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} MemoryStream создан", aLinkSyncServiceEntitiesId);

                                            var lBinding = new TcpChunkingBinding
                                                               {
                                                                   OpenTimeout = Properties.Settings.Default.OpenTimeout,
                                                                   ReceiveTimeout =
                                                                       Properties.Settings.Default.ReceiveTimeout,
                                                                   SendTimeout = Properties.Settings.Default.SendTimeout,
                                                                   CloseTimeout = Properties.Settings.Default.CloseTimeout
                                                               };
                                            this.WriteLog(USLogLevel.Trace, "lBinding params lBinding.OpenTimeout = {0}; lBinding.ReceiveTimeout = {1}; lBinding.SendTimeout = {2}; lBinding.CloseTimeout = {3}", lBinding.OpenTimeout, lBinding.ReceiveTimeout, lBinding.SendTimeout, lBinding.CloseTimeout);

                                            using (var factory = new ChannelFactory<IUSExService>(lBinding, new EndpointAddress(_syncParams.EndPointAddress)))
                                            {
                                                try
                                                {
                                                    this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} ChannelFactory created", aLinkSyncServiceEntitiesId);
                                                    IUSExService service = factory.CreateChannel();
                                                    this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} IUSExService service = factory.CreateChannel created", aLinkSyncServiceEntitiesId);
                                                    var lRetValuesStr = service.EntitySync(ms);
                                                    this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} service.EntitySync выполнен", aLinkSyncServiceEntitiesId);
                                                    factory.Close();
                                                    this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} ChannelFactory Closed", aLinkSyncServiceEntitiesId);

                                                    this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} Returned Value Deserialized", aLinkSyncServiceEntitiesId);
                                                    var lRetValues = Newtonsoft.Json.JsonConvert.DeserializeObject<USInServiceRetValues>(lRetValuesStr);
                                                    lSyncResult = Convert.ToInt16(lRetValues.Result);
                                                    lResult = lRetValues.ErrorMessage;
                                                }
                                                catch (Exception e)
                                                {
                                                    lResult =
                                                        string.Format("EntitySync: Синхронизация LinkSyncServiceEntitiesId = {0} закончилась с ошибкой. Ошибка: {1}",
                                                                      aLinkSyncServiceEntitiesId, e);
                                                    this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, lResult, e);
                                                    this.WriteLogException(lResult, e);
                                                }
                                                factory.Abort();
                                            }
                                        }
                                    }
                                    break;
                                case 2: // ошибки не было но данные не были получены
                                    lSyncResult = 2;
                                    lResult = lErrorMsg;
                                    break;
                                default: lSyncResult = 0;
                                    lResult = string.Format("EntitySync: Синхронизация LinkSyncServiceEntitiesId = {0} закончилась с ошибкой. Не прогнозируемый результат получения данных",
                                                              aLinkSyncServiceEntitiesId);
                                    break;
                            }

                            lIteratorNumber += 1;
                        }
                        while ((lSyncResult == 1) && (lJSonElements != string.Empty));

                        switch (lSyncResult)
                        {
                            case 0:
                                this.WriteLog(USLogLevel.Trace | USLogLevel.Debug | USLogLevel.Error, "Ошибка при синхронизация сущности EntitySync: aLinkSyncServiceEntitiesId = {0} Описание {1}; Сущность {2}: {3}; Филиал {4}; Ошибка {5}",
                                aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                _syncParams.ElsysTypeName, _syncParams.BranchName, lResult);
                                break;
                            case 1: 
                                lData.SET_LINK_SYNC_SERV_CALL_DATA(Convert.ToDecimal(LinkSyncServEntId), lSyncDateTime);

                                this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, "Успешная синхронизация сущности EntitySync: aLinkSyncServiceEntitiesId = {0} Описание {1}; Сущность {2}: {3}; Филиал {4}",
                                    aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                    _syncParams.ElsysTypeName, _syncParams.BranchName);
                                break;
                            case 2:
                                this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, "Cинхронизация сущности EntitySync не завершена: aLinkSyncServiceEntitiesId = {0} Описание {1}; Сущность {2}: {3}; Филиал {4}. Причина {5}",
                                    aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                    _syncParams.ElsysTypeName, _syncParams.BranchName, lResult);
                                lResult = string.Empty;
                                break;
                        }
                    }
                    finally
                    {
                        lData.Connection.Close();
                        this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} Соединение закрыто", aLinkSyncServiceEntitiesId);
                        lData.Dispose();
                        this.WriteLog(USLogLevel.Trace, "EntitySync: aLinkSyncServiceEntitiesId = {0} lData.Dispose done", aLinkSyncServiceEntitiesId);
                    }
                }
            }
            catch (Exception e)
            {
                lResult =
                    string.Format("EntitySync: Синхронизация LinkSyncServiceEntitiesId = {0} закончилась с ошибкой. Ошибка: {1}",
                                  aLinkSyncServiceEntitiesId, e);
                this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, lResult, e);
                this.WriteLogException(lResult, e);
            }
            this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, "EntitySync: Завершена синхронизация: {0}", aLinkSyncServiceEntitiesId);

            return lResult;
        }
    }
}

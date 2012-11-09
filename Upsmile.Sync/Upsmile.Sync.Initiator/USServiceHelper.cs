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
    /// ����������� ��������� �������������
    /// </summary>
    public static class USServiceHelper
    {
        /// <summary>
        /// ������� ������ ������� �������������
        /// </summary>
        public static event EventDelegate  BeginSync;

        /// <summary>
        /// ������� ������ ������� �������������
        /// </summary>
        public static void OnBeginSync(SyncEventArgument argument)
        {
            var handler = BeginSync;
            if (handler != null) handler(argument);
        }
        
        /// <summary>
        /// ������� ��������� �������������
        /// </summary>
        public static event EventDelegate EndSync;

        /// <summary>
        /// ������� ��������� ������� �������������
        /// </summary>
        public static void OnEndSync(SyncEventArgument argument)
        {
            var handler = EndSync;
            if (handler != null) handler(argument);
        }

        /// <summary>
        /// ������� ������������� ������ ��� ������������
        /// </summary>
        public static event EventDelegate ErrorSync;

        /// <summary>
        /// ������� ������������� ������ ��� ������������
        /// </summary>
        public static void OnErrorSync(SyncEventArgument argument)
        {
            var handler = ErrorSync;
            if (handler != null) handler(argument);
        }

        /// <summary>
        /// ����������� ��������
        /// </summary>
        public static Logger _logger = LogManager.GetCurrentClassLogger();

        // ������������� ������ �� LINK_SYNC_SERVICES_ENTITIES
        private static double LinkSyncServEntId { get; set; }
        
        /// <summary>
        /// ��������� �������������
        /// </summary>
        private static USSyncParams _syncParams;

        /// <summary>
        /// ��� ������������� (������(true)/������ ���������(false)
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
        /// ����� �������������
        /// </summary>
        /// <param name="aLinkSyncServiceEntitiesId"></param>
        /// <param name="aIsFullSync"></param>
        /// <returns></returns>
        public static string EntitySync(double aLinkSyncServiceEntitiesId, bool aIsFullSync = false)
        {
            var beginArgument = new SyncEventArgument {Exception = null, Result = aLinkSyncServiceEntitiesId};
            OnBeginSync(beginArgument);
            
            _logger.Trace("EntitySync: ������ �������������: {0}", aLinkSyncServiceEntitiesId);
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
                                case 0: // ������
                                    lSyncResult = 0;
                                    lResult = lErrorMsg;
                                    break;
                                case 1: // �������� ��������� ������. 
                                    lSyncResult = 1;
                                    if (lJSonElements != string.Empty)
                                    {
                                        // �������� ��������� ������. ������ �� ������. ��������� �������������
                                        // ��������� ������ ��� �������� �� ������
                                        var lInData = new USInServiceValues
                                                          {
                                                              EntityTypeId = _syncParams.ElsysTypeId,
                                                              JsonEntityData = Convert.ToString(lJSonElements)
                                                          };
                                        // ������������� ������ ��� �������� �� ������
                                        var lJsonInData = JsonConvert.SerializeObject(lInData);
                                        _logger.Trace("EntitySync: ��������� �� ������� {0}", Encoding.Default.EncodingName);
                                        // ������� ����� ������� ����� ������� �� ������
                                        var DBEncoding = Encoding.GetEncoding(Properties.Settings.Default.DBEncodingName);
                                        var TransmissionEncoding = Encoding.GetEncoding(Properties.Settings.Default.TransmissionEncodingName);
                                        _logger.Trace("EntitySync: ��������� �� ���� {0}", DBEncoding.EncodingName);
                                        _logger.Trace("EntitySync: ��������� �������� {0}", TransmissionEncoding.EncodingName);
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
                                                        string.Format("EntitySync: ������������� LinkSyncServiceEntitiesId = {0} ����������� � �������. ������: {1}",
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
                                case 2: // ������ �� ���� �� ������ �� ���� ��������
                                    lSyncResult = 2;
                                    lResult = lErrorMsg;
                                    OnErrorSync(new SyncEventArgument {Exception = null,Result = lResult});
                                    continue;
                                    //break;
                                default: lSyncResult = 0;
                                    lResult = string.Format("EntitySync: ������������� LinkSyncServiceEntitiesId = {0} ����������� � �������. �� �������������� ��������� ��������� ������",
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
                                var s0 = string.Format("������ ��� ������������� �������� EntitySync: aLinkSyncServiceEntitiesId = {0} �������� {1}; �������� {2}: {3}; ������ {4}; ������ {5}",
                                              aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                              _syncParams.ElsysTypeName, _syncParams.BranchName, lResult);
                                OnEndSync(new SyncEventArgument {Exception = null,Result = s0});
                                break;
                            case 1:
                                lData.SET_LINK_SYNC_SERV_CALL_DATA(Convert.ToDecimal(LinkSyncServEntId), lSyncDateTime);

                                var s1 =string.Format("�������� ������������� �������� EntitySync: aLinkSyncServiceEntitiesId = {0} �������� {1}; �������� {2}: {3}; ������ {4}",
                                              aLinkSyncServiceEntitiesId, _syncParams.Description, _syncParams.ElsysTypeId,
                                              _syncParams.ElsysTypeName, _syncParams.BranchName);
                                OnEndSync(new SyncEventArgument {Exception = null,Result = s1});
                                break;
                            case 2:
                                var s2 = string.Format("C������������ �������� EntitySync �� ���������: aLinkSyncServiceEntitiesId = {0} �������� {1}; �������� {2}: {3}; ������ {4}. ������� {5}",
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
                    string.Format("EntitySync: ������������� LinkSyncServiceEntitiesId = {0} ����������� � �������. ������: {1}",
                                  aLinkSyncServiceEntitiesId, e);
                
                OnErrorSync(new SyncEventArgument {Exception = e,Result = lResult});
            }
            OnEndSync(new SyncEventArgument {Exception = null,Result = aLinkSyncServiceEntitiesId});
            return lResult;
        }
    }
}
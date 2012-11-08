using System;
using System.Data;
using System.Data.EntityClient;
using System.Linq;
using Upsmile.Sync.BasicClasses;
using Upsmile.Sync.BasicClasses.ExtensionMethods;

namespace SyncServicesModel
{
    /// <summary>
    /// Класс работы с параметрами синхронитзации
    /// и для получения данных для отсылки на филоиалы
    /// </summary>
    public partial class SyncServicesEntities : IUSLogBasicClass
    {
        /// <summary>
        /// получение данных по текущей синхронизации
        /// </summary>
        /// <param name="aLinkSyncServiceEntitiesId">идентификатор строки из LINK_SYNC_SERVICES_ENTITIES</param>
        /// <returns>Возвращает элемент типа USSyncParams</returns>
        public USSyncParams GetSyncServiceData(double aLinkSyncServiceEntitiesId)
        {
            USSyncParams lResult = null;

            try
            {
                var query = from lse in Linksyncservicesentities
                            where (lse.ID == aLinkSyncServiceEntitiesId)
                            select
                                new USSyncParams
                                {
                                    Description = lse.ElsysSyncServic.DESCRIPTION,
                                    EndPointAddress = lse.ElsysSyncServic.ENDPOINTADDRESS,
                                    ElsysTypeId = lse.ELSYSTYPEID,
                                    ElsysTypeName = lse.ElsysType.TYPENAME,
                                    BranchId = lse.ElsysSyncServic.BRANCHID,
                                    BranchName = lse.ElsysSyncServic.DICPLACE.NAME
                                };

                if (query.ToList().Any())
                {
                    lResult = (query.ToList())[0];
                }
            }
            catch (Exception e)
            {
                this.WriteLogException(e.ToString(), e);
            }
            return lResult;
        }

        public string GetElsysTypeName(double aElsysTypeId)
        {
            string lResult = string.Empty;
            try
            {
                var q = (from et in ElsysTypes
                         where (et.ID == aElsysTypeId)
                         select et).ToList();
                foreach (var lItem in q)
                {
                    lResult = lItem.TYPENAME;
                }
            }
            catch (Exception e)
            {
                this.WriteLogException(e.ToString(), e);
            }
            return lResult;
        }

        /// <summary>
        /// Получение данных для синхронизации по сущности
        /// </summary>
        public decimal? GetLinkSyncServEntData(double? P_LINK_SYNC_SERV_ENT_ID, decimal? P_ITERATOR_NUMBER, ref string P_XML_DATA, ref string P_ERROR_MSG)
        {
            if (Connection.State != ConnectionState.Open)
                Connection.Open();
            var command = new EntityCommand();
            if (CommandTimeout.HasValue)
                command.CommandTimeout = CommandTimeout.Value;
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = @"SyncServicesEntities.GET_ENTITY_DATA";
            command.Connection = (EntityConnection)Connection;
            var P_LINK_SYNC_SERV_ENT_IDParameter = new EntityParameter("P_LINK_SYNC_SERV_ENT_ID", DbType.Double);
            if (P_LINK_SYNC_SERV_ENT_ID.HasValue)
                P_LINK_SYNC_SERV_ENT_IDParameter.Value = P_LINK_SYNC_SERV_ENT_ID;
            command.Parameters.Add(P_LINK_SYNC_SERV_ENT_IDParameter);
            var P_ITERATOR_NUMBERParameter = new EntityParameter("P_ITERATOR_NUMBER", DbType.Decimal);
            if (P_ITERATOR_NUMBER.HasValue)
                P_ITERATOR_NUMBERParameter.Value = P_ITERATOR_NUMBER;
            command.Parameters.Add(P_ITERATOR_NUMBERParameter);
            var P_XML_DATAParameter = new EntityParameter("P_XML_DATA", DbType.String);
            if (P_XML_DATA != null)
                P_XML_DATAParameter.Value = P_XML_DATA;
            command.Parameters.Add(P_XML_DATAParameter);
            var P_ERROR_MSGParameter = new EntityParameter("P_ERROR_MSG", DbType.String);
            if (P_ERROR_MSG != null)
                P_ERROR_MSGParameter.Value = P_ERROR_MSG;
            command.Parameters.Add(P_ERROR_MSGParameter);
            var result = (decimal?)command.ExecuteScalar();
            if (P_XML_DATAParameter.Value != null && !(P_XML_DATAParameter.Value is DBNull))
                P_XML_DATA = (string)P_XML_DATAParameter.Value;
            else
                P_XML_DATA = string.Empty;
            if (P_ERROR_MSGParameter.Value != null && !(P_ERROR_MSGParameter.Value is DBNull))
                P_ERROR_MSG = (string)P_ERROR_MSGParameter.Value;
            else
                P_ERROR_MSG = string.Empty;
            return result;
        }
    }
}

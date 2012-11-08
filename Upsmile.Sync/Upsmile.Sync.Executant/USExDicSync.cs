using System;
using System.Data.EntityClient;
using System.Data.SqlClient;
using SyncServicesModel;
using Upsmile.Sync.BasicClasses.ExtensionMethods;

namespace Upsmile.Sync.Executant
{
    // базовый класс синхронизации данных
    public class USExDicSync : IUSLogBasicClass
    {
        private static string GetConnectionString()
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
            return (entBuilder.ToString());
        }

        // синхронизация справочника 
        public int DicSync(double aEntityTypeId, string aJsonEntityData, ref string aErrorMessage)
        {
            if (aErrorMessage == null) throw new ArgumentNullException("aErrorMessage");
            int lResult;
            aErrorMessage = string.Empty;

            try
            {
                this.WriteLog(USLogLevel.Debug, "Старт синхронизации сущности {0}", aEntityTypeId);
                if (Properties.Settings.Default.NeedLogSyncData)
                {
                    this.WriteLog(USLogLevel.Trace, "Cинхронизации сущности {0}. Переданные данные {1}", aEntityTypeId, aJsonEntityData);                    
                }

                var lDbConnectionString = GetConnectionString();

                using (var lData = new SyncServicesEntities(lDbConnectionString))
                {
                    var lElsysTypeName = lData.GetElsysTypeName(aEntityTypeId);

                    lData.Connection.Open();
                    this.WriteLog(USLogLevel.Trace, "Cинхронизации сущности {0}:{1}. Start UpdateEntityData", aEntityTypeId, lElsysTypeName);                    

                    lResult = Convert.ToInt16(lData.UPDATE_ENTITY_DATA(aJsonEntityData, (long?)aEntityTypeId, ref aErrorMessage));
                    this.WriteLog(USLogLevel.Trace, "Cинхронизации сущности {0}:{1}. Finish UpdateEntityData. Result {2}", aEntityTypeId, lElsysTypeName, lResult);                    

                    switch (lResult)
                    { 
                        case 0:
                            this.WriteLog(USLogLevel.Trace | USLogLevel.Debug | USLogLevel.Error, "Синхронизации сущности {0}:{1} окончена с ошибкой {2}", aEntityTypeId, lElsysTypeName, aErrorMessage);                            
                            break;
                        case 1:
                            this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, "Синхронизации сущности {0}:{1} окончена успешно", aEntityTypeId, lElsysTypeName);                            
                            break;
                        case 2:
                            this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, "Синхронизации сущности {0}:{1} была прервана по причине {2}", aEntityTypeId, lElsysTypeName, aErrorMessage);                                                        
                            break;
                    }                    
                }
            }
            catch (Exception e)
            {
                aErrorMessage = string.Format("Ошибка синхронизации сущности {0}. Ошибка: {1}", aEntityTypeId, e);
                this.WriteLog(USLogLevel.Trace | USLogLevel.Debug, aErrorMessage);
                this.WriteLogException(aErrorMessage, e);
                throw;
            }
            return lResult;
        }
    }
}
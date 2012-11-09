using System;
using System.IO;
using System.Text;
using NLog;
using Upsmile.Sync.BasicClasses;
using Upsmile.Sync.BasicClasses.ExtensionMethods;

namespace Upsmile.Sync.Executant
{
    public static class USExserviceHelper
    {
        public static string ToEncodeString(this Stream stream,Encoding dbencoding,Encoding transmissionEncoding)
        {
            if(stream == null) throw new ArgumentNullException("stream");
            if(dbencoding == null) throw new ArgumentNullException("dbencoding");
            if(transmissionEncoding == null) throw new ArgumentNullException("transmissionEncoding");
             string result;
             using (var reader = new StreamReader(stream, transmissionEncoding))
             {
                result = reader.ReadToEnd();
                reader.Close();
             }
            return result;
        }
    }

    /// <summary>
    /// Сервис синхронизации данных. 
    /// Запускается на филиале
    /// </summary>
    public class USExService : IUSExService, IUSLogBasicClass
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// запуск синхронизации сущности на филиале
        /// </summary>
        /// <param name="aInValues">Поток с входными данными. Это строка = сериализованный класс Upsmile.Sync.BasicClasses.USInServiceValues</param>
        /// <returns>Строка. В случае успешной синхронизации строка пустая. В противном случае выводится ошибка</returns>
        public string EntitySync(Stream aInValues)
        {
            var lResult = new USInServiceRetValues { Result = 0, ErrorMessage = string.Empty };
            try
            {
                var DBEncoding = Encoding.GetEncoding(Properties.Settings.Default.DBEncodingName);
                var TransmissionEncoding = Encoding.GetEncoding(Properties.Settings.Default.TransmissionEncodingName);
                var lJsonInValues = aInValues.ToEncodeString(DBEncoding, TransmissionEncoding);
                aInValues.Dispose();
                var lInValues = Newtonsoft.Json.JsonConvert.DeserializeObject<USInServiceValues>(lJsonInValues);
                
                var ldicSync = new USExDicSync();
                ldicSync.BeginExecute += ldicSync_BeginExecute;
                ldicSync.ErrorExecute += ldicSync_ErrorExecute;
                ldicSync.EndExecute += ldicSync_EndExecute;

                var lSyncErrorMessage = string.Empty;
                var lSyncResult = ldicSync.DicSync(lInValues.EntityTypeId, lInValues.JsonEntityData, ref lSyncErrorMessage);
                lResult.Result = lSyncResult;
                lResult.ErrorMessage = lSyncErrorMessage;
            }
            catch (Exception e)
            {
                this.WriteLogException(e.ToString(), e);
                lResult.Result = 0;
                lResult.ErrorMessage = Newtonsoft.Json.JsonConvert.SerializeObject(e);
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject(lResult);
        }

        static void ldicSync_EndExecute(ExecutantArgument argument)
        {
            LogEvent(argument);
        }

        private static void LogEvent(ExecutantArgument argument)
        {
            argument.With(x => x.Result).Do(x => _logger.Trace(x));
            argument.With(x => x.Exception).Do(x => _logger.Debug(Newtonsoft.Json.JsonConvert.SerializeObject(x)));
        }

        static void ldicSync_ErrorExecute(ExecutantArgument argument)
        {
          LogEvent(argument);   
        }

        static void ldicSync_BeginExecute(ExecutantArgument argument)
        {
            LogEvent(argument);
        }
    }
}

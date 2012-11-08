using System;
using System.IO;
using System.Text;
using Upsmile.Sync.BasicClasses;
using Upsmile.Sync.BasicClasses.ExtensionMethods;

namespace Upsmile.Sync.Executant
{
    /// <summary>
    /// Сервис синхронизации данных. 
    /// Запускается на филиале
    /// </summary>
    public class USExService : IUSExService, IUSLogBasicClass
    {
        /// <summary>
        /// прелобразование потока в строку
        /// </summary>
        /// <param name="aStream">Поток для преобразования</param>
        /// <returns>Строка (преобразованный поток)</returns>
        private string StreamToStr(Stream aStream)
        {
            this.WriteLog(USLogLevel.Trace, "StreamToStr: начато преобразование");
            this.WriteLog(USLogLevel.Trace, "EntitySync: Кодировка на клиенте {0}", Encoding.Default.EncodingName);
            var DBEncoding = Encoding.GetEncoding(Properties.Settings.Default.DBEncodingName);
            var TransmissionEncoding = Encoding.GetEncoding(Properties.Settings.Default.TransmissionEncodingName);
            this.WriteLog(USLogLevel.Trace, "EntitySync: Кодировка на базе {0}", DBEncoding.EncodingName);
            this.WriteLog(USLogLevel.Trace, "EntitySync: Кодировка передачи {0}", TransmissionEncoding.EncodingName);
            
            string lResult;
            using (var reader = new StreamReader(aStream, TransmissionEncoding))
            {
                try
                {
                    lResult = reader.ReadToEnd();
                }
                catch (Exception e)
                {
                    this.WriteLogException(e.ToString(), e);
                    
                    lResult = string.Empty;
                }
            }
            lResult = DBEncoding.GetString(Encoding.Convert(TransmissionEncoding, DBEncoding, DBEncoding.GetBytes(lResult)));
            this.WriteLog(USLogLevel.Trace, "StreamToStr: закончено преобразование");
            
            return lResult;
        }
        
        /// <summary>
        /// запуск синхронизации сущности на филиале
        /// </summary>
        /// <param name="aInValues">Поток с входными данными. Это строка = сериализованный класс Upsmile.Sync.BasicClasses.USInServiceValues</param>
        /// <returns>Строка. В случае успешной синхронизации строка пустая. В противном случае выводится ошибка</returns>
        public string EntitySync(Stream aInValues)
        {

            var lResult = new USInServiceRetValues { Result = 1, ErrorMessage = string.Empty };

            // входные параметры элемент класса USInServiceValues
            // сериализованные JSON
            try
            {
                // получение потока и занесение его в строку
                string lJsonInValues = StreamToStr(aInValues);
                this.WriteLog(USLogLevel.Trace, "EntitySync: поток занесен в строку");
                aInValues.Dispose();
                
                // десериализация входных данных
                var lInValues = Newtonsoft.Json.JsonConvert.DeserializeObject<USInServiceValues>(lJsonInValues);
                this.WriteLog(USLogLevel.Trace, "EntitySync: EntityTypeId = {0} входные данные десериализованы", lInValues.EntityTypeId);
                
                // запуск синхронизации
                var ldicSync = new USExDicSync();
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

        public void ProcessMessage(System.ServiceModel.Channels.Message messsage)
        {
            //Console.WriteLine("Message received with action {0}", messsage.Headers.Action);
        }
    }
}

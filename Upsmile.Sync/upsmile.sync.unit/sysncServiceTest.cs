using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SyncServicesModel;
using System.Data.SqlClient;
using System.Data.EntityClient;
using Upsmile.Sync.BasicClasses;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Samples.ChunkingChannel;
using SyncServicesModel;
using Upsmile.Sync.BasicClasses.ExtensionMethods;
using System.ServiceModel;
using Upsmile.Sync.Initiator;
using Upsmile.Sync.Executant;



namespace upsmile.sync.unit
{
    [TestFixture]
    public class LoadDataTest
    {

        private string GetConnectionString(string branch)
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();
            sqlBuilder.MaxPoolSize = 100;
            sqlBuilder.Pooling = false;
            sqlBuilder.UserID = "distrdev";
            sqlBuilder.DataSource = branch;
            sqlBuilder.Password = "distrdev";
            string ProviderString = sqlBuilder.ToString();

            EntityConnectionStringBuilder entBuilder = new EntityConnectionStringBuilder();
            entBuilder.Provider = "Devart.Data.Oracle";
            entBuilder.ProviderConnectionString = ProviderString;
            entBuilder.Metadata = @"res://*/emSyncServices.csdl|
                                        res://*/emSyncServices.ssdl|
                                        res://*/emSyncServices.msl";
            return entBuilder.ToString();
        }

        private static double _LinkSyncServEnt = 87513.0;
        
        private static object TestParameters {get;set;}

        private static TimeSpan _openTimeoute = TimeSpan.Parse("00:10:00");

        /// <summary>
        /// Тестирование синхронизации - "Структура контракта" Донецкий филиал СЖ 
        /// </summary>     
        [Test]
        public void UpsmileSyncInitiatorTest()
        {
            var connection = GetConnectionString("DISTR_CO");
            var data = new SyncServicesEntities(connection);
            string xml = string.Empty;
            string error = string.Empty;
            var result = data.GetLinkSyncServEntData(_LinkSyncServEnt,1,ref xml,ref error);
            Assert.IsNotNullOrEmpty(xml);
            Assert.IsNotNull(error);
            var syncParams = data.GetSyncServiceData(_LinkSyncServEnt);
            Assert.IsNotNull(syncParams);
            Assert.IsTrue(syncParams.BranchName.ToUpper() =="Донецкий".ToUpper());
            var reciveData = new USInServiceValues
                {
                    EntityTypeId = syncParams.ElsysTypeId,
                    JsonEntityData = Convert.ToString(xml)
                };
            Assert.IsNotNull(reciveData);
            var sendJSON = JsonConvert.SerializeObject(reciveData);
            Assert.IsNotNullOrEmpty(sendJSON);
            var cfg = "windows-1251";
            var DBEncoding = Encoding.GetEncoding(cfg);
            var TransmissionEncoding = Encoding.GetEncoding(cfg);

            using (var ms = new MemoryStream(Encoding.Convert(DBEncoding, TransmissionEncoding, DBEncoding.GetBytes(sendJSON))))
            {
                //var size = (ms.Length/1024);
                ////sendJSON = String.Empty;
                //var lBinding = new TcpChunkingBinding();
                //lBinding.OpenTimeout= _openTimeoute;
                //lBinding.ReceiveTimeout = _openTimeoute;
                //lBinding.SendTimeout = _openTimeoute;
                //lBinding.CloseTimeout = _openTimeoute;
                //using (var factory = new ChannelFactory<IUSExService>(lBinding, new EndpointAddress(syncParams.EndPointAddress)))
                //{
                //    var service = factory.CreateChannel();
                //    var result1 = service.EntitySync(ms);
                //    var res = JsonConvert.DeserializeObject<USInServiceRetValues>(result1);
                //    Assert.AreEqual(res.ErrorMessage, null);
                //    factory.Close();
                //}

                var service = new USExService();
                var r = service.EntitySync(ms);
                var res = JsonConvert.DeserializeObject<USInServiceRetValues>(r);
                Assert.AreEqual(res.ErrorMessage, null);
            }
        }

        /// <summary>
        /// Получение параметров синхронизации
        /// </summary>
        [Test]
        public void GetSynckParam()
        {
            var connection = GetConnectionString("DISTR_CO");
            var data = new SyncServicesEntities(connection);
            var syncParams = data.GetSyncServiceData(_LinkSyncServEnt);
            Assert.IsNotNull(syncParams);
        }



    }
}

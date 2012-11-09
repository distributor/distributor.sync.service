

using System.Globalization;
using NLog;

namespace Upsmile.Sync.Executant
{
    using System;
    using System.Data.EntityClient;
    using System.Data.SqlClient;
    using SyncServicesModel;
    using BasicClasses.ExtensionMethods;

    /// <summary>
    /// Аргумент для коллбека событий 
    /// </summary>
    public class ExecutantArgument : EventArgs
    {
        /// <summary>
        /// Результат 
        /// </summary>
        public object Result { get; set; }

        /// <summary>
        /// Возникшая ошибка
        /// </summary>
        public object Exception { get; set; }
    }

    /// <summary>
    /// Делегат событий выполнения 
    /// </summary>
    /// <param name="argument"></param>
    public delegate void EventDelegateCall(ExecutantArgument argument);

    /// <summary>
    /// базовый класс синхронизации данных
    /// </summary>
    public class USExDicSync : IUSLogBasicClass
    {
        #region Event implement section

        /// <summary>
        /// Событие начала операции синхронизации
        /// </summary>
        public event EventDelegateCall BeginExecute;

        /// <summary>
        /// Событие начала операции синхронизации
        /// </summary>
        /// <param name="argument">Аргумент начала операции</param>
        public void OnBeginExecute(ExecutantArgument argument)
        {
            var handler = BeginExecute;
            if (handler != null) handler(argument);
        }

        /// <summary>
        /// Событие возникает при окончании операции синхронизации
        /// </summary>
        public event EventDelegateCall EndExecute;

        /// <summary>
        /// Событие возникает при окончании операции синхронизации
        /// </summary>
        /// <param name="argument">Аргумент начала операции</param>
        public void OnEndExecute(ExecutantArgument argument)
        {
            var handler = EndExecute;
            if (handler != null) handler(argument);
        }

        /// <summary>
        /// Событие возникает при ошибках выполнениии синхронизации
        /// </summary>
        public event EventDelegateCall ErrorExecute;

        /// <summary>
        /// Событие возникает при ошибках выполнениии синхронизации
        /// </summary>
        /// <param name="argument"></param>
        public void OnErrorExecute(ExecutantArgument argument)
        {
            var handler = ErrorExecute;
            if (handler != null) handler(argument);
        }

        #endregion


        /// <summary>
        /// Возвращает строку подключения к EntityModel
        /// </summary>
        /// <returns>connection string devart.entity.model</returns>
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

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();


        /// <summary>
        /// Запуск синхронизации справочника
        /// </summary>
        /// <param name="aEntityTypeId">Тип сущности Distributor</param>
        /// <param name="aJsonEntityData">Серриализированное значение</param>
        /// <param name="aErrorMessage">Строка информации об ошибке (Гриша погорячился - нужно будет переписать с следуещем заходе)</param>
        /// <returns>Код результата выполнения (Нужно заставить Игоря написать и разучить теги xml комментариев)</returns>
        public int DicSync(double aEntityTypeId, string aJsonEntityData, ref string aErrorMessage)
        {
            if (aErrorMessage == null) throw new ArgumentNullException("aErrorMessage");
            var lResult = 0;
            aErrorMessage = string.Empty;
            var startMessage = string.Format("Старт синхронизации сущности {0}", aEntityTypeId);
            if (Properties.Settings.Default.NeedLogSyncData)
                startMessage = string.Format("Cинхронизации сущности {0}. Переданные данные {1}",
                                             aEntityTypeId, aJsonEntityData);
            
            OnBeginExecute(new ExecutantArgument {Exception = null, Result = startMessage});

            var lDbConnectionString = GetConnectionString();

            #region Выполнение синхронизации

            using (var lData = new SyncServicesEntities(lDbConnectionString))
            {
                var lElsysTypeName = lData.GetElsysTypeName(aEntityTypeId);
                lData.Connection.Open();
                _logger.Trace("Cинхронизации сущности {0}:{1}. Start UpdateEntityData", aEntityTypeId, lElsysTypeName);
                //[Д.Гордиенко] объяснить всем раз и навсегда о вредности кода из прошлой реализации
                //похоже, что разработчики не понимают зачем нужен System.Nullable 
                var value = lData.UPDATE_ENTITY_DATA(aJsonEntityData, (long?) aEntityTypeId, ref aErrorMessage);
                if (value.HasValue)
                {
                    lResult = Convert.ToInt16(value);
                    _logger.Trace("Cинхронизации сущности {0}:{1}. Finish UpdateEntityData. Result {2}", aEntityTypeId,
                                  lElsysTypeName, lResult);

                    switch (lResult)
                    {
                        case 0:
                            var s0 = string.Format("Синхронизации сущности {0}:{1} окончена с ошибкой {2}",
                                                   aEntityTypeId, lElsysTypeName, aErrorMessage);
                            var e = new InvalidOperationException(s0);
                            OnErrorExecute(new ExecutantArgument {Exception = e, Result = null});
                            break;
                        case 1:
                            var message1 = string.Format("Синхронизации сущности {0}:{1} окончена успешно",
                                                         aEntityTypeId,
                                                         lElsysTypeName);
                            OnEndExecute(new ExecutantArgument {Exception = null, Result = message1});
                            break;
                        case 2:
                            var message2 = string.Format("Синхронизации сущности {0}:{1} была прервана по причине {2}",
                                                         aEntityTypeId, lElsysTypeName, aErrorMessage);
                            var e1 = new InvalidOperationException(message2);
                            OnErrorExecute(new ExecutantArgument {Exception = e1, Result = message2});
                            break;
                    }
                    lData.Connection.Close();
                }
                else
                {
                    lData.Connection.Close();
                    var e = new InvalidOperationException("Результат выполнения UPDATE_ENTITY_DATA равен null");
                    OnErrorExecute(new ExecutantArgument {Exception = e, Result = e.Message});
                    OnEndExecute(new ExecutantArgument {Exception = e, Result = e.Message});
                }
            }

            #endregion
            return lResult;
        }

    }
}
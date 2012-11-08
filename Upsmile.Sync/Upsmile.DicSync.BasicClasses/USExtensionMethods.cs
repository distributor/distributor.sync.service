using System;
using NLog;

namespace Upsmile.Sync.BasicClasses.ExtensionMethods
{
    /// <summary>
    /// уровни логирования
    /// </summary>
    [Flags]
    public enum USLogLevel
    {
        None = 0,
        Trace = 1,
        Debug = 2,
        Error = 4,
        All = Trace | Debug | Error
    }

    /// <summary>
    /// интерфейс логирования данных
    /// </summary> 
    public interface IUSLogBasicClass
    {
        //void WriteLog(USLogLevel levels, string message, object[] args);
        //void WriteLogException(string message, Exception ex);
    }

    /// <summary>
    /// класс с методами-расширениями по логированию данных
    /// </summary>
    public static class USLogBasicClass
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        // пишем лог
        public static void WriteLog(this IUSLogBasicClass item, USLogLevel levels, string message, params object[] args)
        {
            if ((levels & USLogLevel.Trace) != 0)
            {
                // Trace
                _logger.Trace(message, args);
            }

            if ((levels & USLogLevel.Debug) != 0)
            {
                // Debug
                _logger.Debug(message, args);
            }

            if ((levels & USLogLevel.Error) != 0)
            {
                // Error
                _logger.Error(message, args);
            }
        }
        // пишем лог с Exception
        public static void WriteLogException(this IUSLogBasicClass item, string message, Exception ex)
        {
            // Error
            _logger.ErrorException(message, ex);
        }
    }
}

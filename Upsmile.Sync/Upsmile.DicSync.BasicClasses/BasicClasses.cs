using Newtonsoft.Json;

namespace Upsmile.Sync.BasicClasses
{
    /// <summary>
    /// Данные, передаваемые от иницивтора исполнителю
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class USInServiceValues
    {
        /// <summary>
        /// Идентификатор сущности
        /// </summary>
        [JsonProperty]
        public double EntityTypeId { get; set; }
        /// <summary>
        /// данные по сущности, сериализованные Json
        /// </summary>
        [JsonProperty]
        public string JsonEntityData { get; set; }
    }

    /// <summary>
    /// Данные, возвращенные испогнителем инициатору
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class USInServiceRetValues
    {
        /// <summary>
        /// Результат
        /// 0 - Ошибка
        /// 1 - Успешная синхронизация
        /// 2 - Синхронизация не была выполнена. Необходим повтор
        /// </summary>
        [JsonProperty]
        public decimal Result { get; set; }
        /// <summary>
        /// сообзение об ошибке
        /// </summary>
        [JsonProperty]
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    //// Класс с параметрами запускаемого режима синхронизации
    /// </summary>
    public class USSyncParams
    {
        /// <summary>
        /// Описание
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Конечная точка доступа 
        /// </summary>
        public string EndPointAddress { get; set; }
        /// <summary>
        /// Идентификатор сущности
        /// </summary>
        public double ElsysTypeId { get; set; }
        /// <summary>
        /// Наименование сущности
        /// </summary>
        public string ElsysTypeName { get; set; }
        /// <summary>
        /// Идентификатор филиала
        /// </summary>
        public double? BranchId { get; set; }
        /// <summary>
        /// Наименование филиала
        /// </summary>
        public string BranchName { get; set; }
    }    
}

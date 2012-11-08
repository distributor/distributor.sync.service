using System.ServiceModel;

namespace Upsmile.Sync.Initiator
{
    [ServiceContract]
    public interface IUSInService
    {
        [OperationContract]
        string EntitySync(double aLinkSyncServiceEntitiesId, bool aIsFullSync = false);
    }
}

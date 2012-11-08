using System.ServiceModel;

namespace Upsmile.Sync.Initiator
{
    [ServiceContract]
    public interface IUSInService
    {
        [OperationContract]
        string EntitySync(int aLinkSyncServiceEntitiesId, bool aIsFullSync = false);
    }
}

using System.ServiceModel;
using Microsoft.Samples.ChunkingChannel;
using System.IO;

namespace Upsmile.Sync.Executant
{
    [ServiceContract]
    public interface IUSExService
    {
        [OperationContract]
        [ChunkingBehavior(ChunkingAppliesTo.InMessage)]
        string EntitySync(Stream aInValues);
    }
}

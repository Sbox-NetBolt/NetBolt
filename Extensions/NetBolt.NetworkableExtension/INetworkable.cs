using NetBolt.Messaging;

namespace NetBolt.NetworkableExtension;

public interface INetworkable
{
    void Serialize( NetworkMessageWriter writer );
    void Deserialize( NetworkMessageReader reader );
}

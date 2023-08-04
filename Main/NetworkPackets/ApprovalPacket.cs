using Lidgren.Network;
using ThronefallMP.Components;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets;

public class ApprovalPacket : IPacket
{
    private const string ApprovalString = $"thronefall_mp_{PluginInfo.PLUGIN_VERSION}";
    
    public const PacketId PacketID = PacketId.ApprovalPacket;

    public bool Approved { get; private set; }
    public long? Secret;
    public int PlayerId;

    public PacketId TypeID => PacketID;
    public NetDeliveryMethod Delivery => NetDeliveryMethod.ReliableOrdered;
    public int Channel => 0;
    
    public void Send(NetBuffer writer)
    {
        writer.Write(ApprovalString);
        if (Secret.HasValue)
        {
            writer.Write(Secret.Value);
            writer.Write(PlayerId);
        }
    }

    public void Receive(NetBuffer reader)
    {
        var value = reader.ReadString();
        Approved = value == ApprovalString;
        if (reader.PositionInBytes != reader.LengthBytes)
        {
            Secret = reader.ReadInt64();
            PlayerId = reader.ReadInt32();
        }
        else
        {
            Secret = null;
            PlayerId = 0;
        }
    }
}
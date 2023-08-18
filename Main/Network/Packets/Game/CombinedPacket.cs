using System.Collections.Generic;

namespace ThronefallMP.Network.Packets.Game;

public class CombinedPacket : BasePacket
{
    public const PacketId PacketID = PacketId.Combined;
    public override PacketId TypeID => PacketID;
    public override Channel Channel => Packets[0].Channel;
    public override int DeliveryMask => Packets[0].DeliveryMask;

    public PacketId InnerPacketType;
    public List<BasePacket> Packets = new();
    
    public override void Send(Buffer writer)
    {
        writer.Write(InnerPacketType);
        writer.Write((ushort)Packets.Count);
        foreach (var packet in Packets)
        {
            packet.Send(writer);
        }
    }

    public override void Receive(Buffer reader)
    {
        InnerPacketType = reader.ReadPacketId();
        var count = reader.ReadUInt16();
        for (var i = 0; i < count; ++i)
        {
            var packet = Plugin.Instance.Network.CreatePacket(InnerPacketType);
            if (packet == null)
            {
                Plugin.Log.LogWarning($"Unable to parse Combined packet, could not create packet of type '{InnerPacketType}'");
                return;
            }
            
            packet.Receive(reader);
            Packets.Add(packet);
        }
    }
}
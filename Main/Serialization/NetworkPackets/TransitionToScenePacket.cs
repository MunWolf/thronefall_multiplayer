using LiteNetLib;
using LiteNetLib.Utils;

namespace ThronefallMP.NetworkPackets;

public class TransitionToScenePacket : IPacket
{
    public enum TransitionType
    {
        LevelSelectToLevel
    }
    
    public const int PacketID = 3;

    public TransitionType Type;
    public string Level;

    public int TypeID()
    {
        return PacketID;
    }

    public void Send(ref NetDataWriter writer)
    {
        writer.Put((int)Type);
        writer.Put(Level);
    }

    public void Receive(ref NetPacketReader reader)
    {
        Type = (TransitionType)reader.GetInt();
        Level = reader.GetString();
    }
}
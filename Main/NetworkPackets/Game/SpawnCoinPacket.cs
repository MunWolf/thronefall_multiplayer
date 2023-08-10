using Steamworks;
using ThronefallMP.Network;
using UnityEngine;

namespace ThronefallMP.NetworkPackets.Game;

public class SpawnCoinPacket : IPacket
{
    public const PacketId PacketID = PacketId.SpawnCoinPacket;

    public int Player;
    public Vector3 Position;
    public Quaternion Rotation;
    
    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;
    
    public void Send(Buffer writer)
    {
        writer.Write(Player);
        writer.Write(Position);
        writer.Write(Rotation);
    }

    public void Receive(Buffer reader)
    {
        Player = reader.ReadInt32();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
    }
}
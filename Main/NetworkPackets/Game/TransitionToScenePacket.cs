using System.Collections.Generic;
using Steamworks;
using ThronefallMP.Network;

namespace ThronefallMP.NetworkPackets.Game;

public class TransitionToScenePacket : IPacket
{
    public const PacketId PacketID = PacketId.TransitionToScenePacket;

    public string ComingFromGameplayScene;
    public string Level;
    public List<string> Perks = new();

    public PacketId TypeID => PacketID;
    public int DeliveryMask => Constants.k_nSteamNetworkingSend_Reliable;
    public int Channel => 0;

    public void Send(Buffer writer)
    {
        writer.Write(ComingFromGameplayScene);
        writer.Write(Level);
        writer.Write(Perks.Count);
        foreach (var perk in Perks)
        {
            writer.Write(perk);
        }
    }

    public void Receive(Buffer reader)
    {
        ComingFromGameplayScene = reader.ReadString();
        Level = reader.ReadString();
        var count = reader.ReadInt32();
        Perks.Clear();
        for (var i = 0; i < count; ++i)
        {
            Perks.Add(reader.ReadString());
        }
    }
}
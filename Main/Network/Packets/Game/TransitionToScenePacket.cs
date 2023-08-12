using System.Collections.Generic;
using Steamworks;

namespace ThronefallMP.Network.Packets.Game;

public class TransitionToScenePacket : BasePacket
{
    public const PacketId PacketID = PacketId.TransitionToScene;

    public string ComingFromGameplayScene;
    public string Level;
    public List<string> Perks = new();

    public override PacketId TypeID => PacketID;
    public override Channel Channel => Channel.Player;
    public override bool ShouldPropagate => true;

    public override void Send(Buffer writer)
    {
        writer.Write(ComingFromGameplayScene);
        writer.Write(Level);
        writer.Write(Perks.Count);
        foreach (var perk in Perks)
        {
            writer.Write(perk);
        }
    }

    public override void Receive(Buffer reader)
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
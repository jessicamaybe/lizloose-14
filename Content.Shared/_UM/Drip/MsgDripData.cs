using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._UM.Drip;


/// <summary>
/// Sent server -> client to inform the client of available drip
/// </summary>
public sealed class MsgDripData : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public Dictionary<string, int> AvailableDrip = new();


    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        AvailableDrip.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            AvailableDrip.Add(buffer.ReadString(), buffer.ReadInt32());
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(AvailableDrip.Count);

        foreach (var (role, rounds) in AvailableDrip)
        {
            buffer.Write(role);
            buffer.Write(rounds);
        }
    }
}

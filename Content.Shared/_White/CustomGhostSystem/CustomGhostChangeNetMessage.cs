using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._White.CustomGhostSystem;

//LP edit: этот файл я сделал, чтобы заменить обновление гостов через команды консоли на NetMessage

public sealed class ChangeCustomGhostMsg : NetMessage
{
    public string id = "";
    public string uuid = "";

    public override MsgGroups MsgGroup => MsgGroups.Command;


    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        id = buffer.ReadString();
        uuid = buffer.ReadString();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(id);
        buffer.Write(uuid);
    }
}

public sealed class CustomGhostAnswer : NetMessage
{
    public Dictionary<string, List<string>?>? Reasons;

    public override MsgGroups MsgGroup => MsgGroups.Command;


    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        Reasons = count > 0 ? new() : null;
        if (Reasons == null)
            return;

        for (var i = 0; i < count; i++)
        {
            string proto = buffer.ReadString();
            var num = buffer.ReadVariableInt32();

            if (num <= 0)
            {
                Reasons[proto] = null;
                continue;
            }

            var reasons = new List<string>(num);
            for (var j = 0; j < num; j++)
            {
                reasons.Add(buffer.ReadString());
            }

            Reasons.Add(proto, reasons);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (Reasons == null || Reasons.Count == 0)
        {
            buffer.WriteVariableInt32(0);
            return;
        }

        buffer.WriteVariableInt32(Reasons.Count);

        foreach (var (proto, reasons) in Reasons)
        {
            buffer.Write(proto);

            if (reasons == null || reasons.Count == 0)
            {
                buffer.WriteVariableInt32(0);
                continue;
            }

            buffer.WriteVariableInt32(reasons.Count);
            foreach (var reason in reasons)
            {
                buffer.Write(reason);
            }
        }
    }

}

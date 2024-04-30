using Unity.Collections;
using Unity.Networking.Transport;

internal class NetRematch : NetMessage
{
    public int TeamId;
    public byte WantRematch;

    public NetRematch()
    {
        Code = OpCode.REMATCH;
    }

    public NetRematch(DataStreamReader reader)
    {
        Code = OpCode.REMATCH;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(TeamId);
        writer.WriteByte(WantRematch);
    }

    public override void Deserialize(DataStreamReader reader)
    {
        TeamId = reader.ReadInt();
        WantRematch = reader.ReadByte();
    }

    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.SERVER_REMATCH?.Invoke(this, cnn);
    }

    public override void ReceivedOnClient()
    {
        NetUtility.CLIENT_REMATCH?.Invoke(this);
    }
}
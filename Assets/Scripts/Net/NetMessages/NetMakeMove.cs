using Unity.Collections;
using Unity.Networking.Transport;

internal class NetMakeMove : NetMessage
{
    public int OriginalX;
    public int OriginalY;
    public int DestinationX;
    public int DestinationY;
    public int TeamId;

    public NetMakeMove()
    {
        Code = OpCode.MAKE_MOVE;
    }

    public NetMakeMove(DataStreamReader reader)
    {
        Code = OpCode.MAKE_MOVE;
        Deserialize(reader);
    }

    public override void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)Code);
        writer.WriteInt(OriginalX);
        writer.WriteInt(OriginalY);
        writer.WriteInt(DestinationX);
        writer.WriteInt(DestinationY);
        writer.WriteInt(TeamId);
    }
    public override void Deserialize(DataStreamReader reader)
    {
        OriginalX = reader.ReadInt();
        OriginalY = reader.ReadInt();
        DestinationX = reader.ReadInt();
        DestinationY = reader.ReadInt();
        TeamId = reader.ReadInt();
    }

    public override void ReceivedOnServer(NetworkConnection cnn)
    {
        NetUtility.SERVER_MAKE_MOVE?.Invoke(this, cnn);
    }

    public override void ReceivedOnClient()
    {
        NetUtility.CLIENT_MAKE_MOVE?.Invoke(this);
    }

}
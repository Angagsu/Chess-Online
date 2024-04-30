using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public enum OpCode
{
    KEEP_ALIVE = 1,
    WELCOME = 2,
    START_GAME = 3,
    MAKE_MOVE = 4,
    REMATCH = 5
}

public static class NetUtility 
{
    // Net Messages
    public static Action<NetMessage> CLIENT_KEEP_ALIVE;
    public static Action<NetMessage> CLIENT_WELCOME;
    public static Action<NetMessage> CLIENT_START_GAME;
    public static Action<NetMessage> CLIENT_MAKE_MOVE;
    public static Action<NetMessage> CLIENT_REMATCH;

    public static Action<NetMessage, NetworkConnection> SERVER_KEEP_ALIVE;
    public static Action<NetMessage, NetworkConnection> SERVER_WELCOME;
    public static Action<NetMessage, NetworkConnection> SERVER_START_GAME;
    public static Action<NetMessage, NetworkConnection> SERVER_MAKE_MOVE;
    public static Action<NetMessage, NetworkConnection> SERVER_REMATCH;

    public static void OnData(DataStreamReader stream, NetworkConnection cnn, Server server = null)
    {
        NetMessage msg = null;
        OpCode opCode = (OpCode)stream.ReadByte();
        switch (opCode)
        {
            case OpCode.KEEP_ALIVE:
                msg = new NetKeepAlive(stream);
                break;
            case OpCode.WELCOME:
                msg = new NetWelcome(stream);
                break;
            case OpCode.START_GAME:
                msg = new NetStartGame(stream);
                break;
            case OpCode.MAKE_MOVE:
                msg = new NetMakeMove(stream);
                break;
            case OpCode.REMATCH:
                msg = new NetRematch(stream);
                break;
            default:
                Debug.LogError("Message received had no OpCode");
                break;
        }

        if (server != null)
        {
            msg.ReceivedOnServer(cnn);
        }
        else
        {
            msg.ReceivedOnClient();
        }
    }
}

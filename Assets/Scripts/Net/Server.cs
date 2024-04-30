using UnityEngine;
using Unity.Collections;
using Unity.Networking.Transport;
using System;


public class Server : MonoBehaviour
{
    public static Server Instance { get; private set; }

    public Action ConnectionDropped;
    public NetworkDriver Driver;

    private NativeList<NetworkConnection> connections;

    private bool isActive = false;
    private const float keepAliveTickRate = 20f;
    private float lastKeepAlive;


    private void Awake()
    {
        Instance = this;
    }
    private void Update()
    {
        if (!isActive)
            return;

        KeepAlive();

        Driver.ScheduleUpdate().Complete();
        CleanupConnections();
        AcceptNewConnections();
        UpdateMessagePump();
    }
    private void OnDestroy()
    {
        Shutdown();
    }

    public void Initialize(ushort port)
    {
        Driver = NetworkDriver.Create();
        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;
        endpoint.Port = port;

        if (Driver.Bind(endpoint) != 0)
        {
            Debug.Log("Unable to bind on port " + endpoint.Port);
            return;
        }
        else
        {
            Driver.Listen();
            Debug.Log("Currently listening on port " + endpoint.Port);
        }

        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
        isActive = true;
    }

    public void Shutdown()
    {
        if (isActive)
        {
            Driver.Dispose();
            connections.Dispose();
            isActive = false;
        }
    }

    private void KeepAlive()
    {
        if (Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            Broadcast(new NetKeepAlive());
        }
    }

    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        for (int i = 0; i < connections.Length; i++)
        {
            NetworkEvent.Type cmd;
            while ((cmd = Driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    NetUtility.OnData(stream, connections[i], this);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[i] = default(NetworkConnection);
                    ConnectionDropped?.Invoke();
                    Shutdown();
                }
            }
        }
    }

    private void AcceptNewConnections()
    {
        NetworkConnection nc;
        while((nc = Driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(nc);
        }
    }

    private void CleanupConnections()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
    }

    // Server specific
    public void SendToClient(NetworkConnection connection, NetMessage msg)
    {
        DataStreamWriter writer;
        Driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        Driver.EndSend(writer);
    }

    public void Broadcast(NetMessage msg)
    {
        for (int i = 0; i < connections.Length; i++)
            if(connections[i].IsCreated)
            {
               // Debug.Log($"Sending  {msg.Code} to : {connections[i]}");
                SendToClient(connections[i], msg);
            }
    }
}

using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client Instance { get; private set; }


    public Action ConnectionDropped;

    public NetworkDriver Driver;

    private NetworkConnection connection;

    private bool isActive = false;



    private void Awake()
    {
        Instance = this;
    }
    private void Update()
    {
        if (!isActive)
            return;

        Driver.ScheduleUpdate().Complete();

        CheckAlive();
        UpdateMessagePump();
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    public void Initialize(string ip, ushort port)
    {
        Driver = NetworkDriver.Create();
        NetworkEndpoint endpoint = NetworkEndpoint.Parse(ip, port);

        connection = Driver.Connect(endpoint);

        Debug.Log("Attempting to connect to Server on " + endpoint.Address);

        isActive = true;

        RegisterToEvent();
    }

    public void Shutdown()
    {
        if (isActive)
        {
            UnregisterToEvent();
            Driver.Dispose();
            isActive = false;
            connection = default(NetworkConnection);
        }
    }

    private void CheckAlive()
    {
        if (!connection.IsCreated && isActive)
        {
            Debug.Log("Someting went wrong, lost connection to server");
            ConnectionDropped?.Invoke();
            Shutdown();
        }
    }

    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = connection.PopEvent(Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                SendToServer(new NetWelcome());
                Debug.Log("We're connected !");
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                NetUtility.OnData(stream, default(NetworkConnection));
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server !");
                connection = default(NetworkConnection);
                ConnectionDropped?.Invoke();
                Shutdown();
            }
        }
        
    }

    public void SendToServer(NetMessage msg)
    {
        DataStreamWriter writer;
        Driver.BeginSend(connection, out writer);
        msg.Serialize(ref writer);
        Driver.EndSend(writer);
    }

    // Event Parsing
    private void RegisterToEvent()
    {
        NetUtility.CLIENT_KEEP_ALIVE += OnKeepAlive;
    }
    private void UnregisterToEvent()
    {
        NetUtility.CLIENT_KEEP_ALIVE -= OnKeepAlive;
    }
    private void OnKeepAlive(NetMessage nm)
    {
        SendToServer(nm);
    }
}

using UnityEngine;
using TMPro;
using System;

public enum CameraAngle
{
    Menu = 0,
    WhiteTeam = 1,
    BlackTeam = 2
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    public Action<bool> SetLocalGame;

    public Server server;
    public Client client;

    [SerializeField] private Animator animator;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private GameObject[] cameraAngles;

    #region AnimationHash

    private int InGameMenuHash = Animator.StringToHash("InGameMenu");
    private int StartMenuHash = Animator.StringToHash("StartMenu");
    private int OnlineMenuHash = Animator.StringToHash("OnlineMenu");
    private int HostMenuHash = Animator.StringToHash("HostMenu");

    #endregion

    private void Awake()
    {
        Instance = this;

        ResgisterEvents();
    }

    private void OnDisable()
    {
        UnregisterEvents();
    }

    // Cameras
    public void ChangeCamera(CameraAngle index)
    {
        for (int i = 0; i < cameraAngles.Length; i++)
        {
            cameraAngles[i].SetActive(false);
        }

        cameraAngles[(int)index].SetActive(true);
    }

    // Buttons
    public void OnLocalGameButton()
    {
        animator.SetTrigger(InGameMenuHash);
        SetLocalGame?.Invoke(true);
        server.Initialize(8007);
        client.Initialize("127.0.0.1", 8007);
    }
    public void OnOnlineGameButton()
    {
        animator.SetTrigger(OnlineMenuHash);
    }
    public void OnOnlineHostButton()
    {
        SetLocalGame?.Invoke(false);
        server.Initialize(8007);
        client.Initialize("127.0.0.1", 8007);
        animator.SetTrigger(HostMenuHash);
    }
    public void OnOnlineConnectButton()
    {
        SetLocalGame?.Invoke(false);
        client.Initialize(addressInput.text, 8007);
    }
    public void OnOnlineBackButton()
    {
        animator.SetTrigger(StartMenuHash);
    }
    public void OnHostBackButton()
    {
        server.Shutdown();
        client.Shutdown();
        animator.SetTrigger(OnlineMenuHash);
    }
    public void OnLeaveFromGameMenu()
    {
        ChangeCamera(CameraAngle.Menu);
        animator.SetTrigger(StartMenuHash);
    }

    #region Register_Unregister_Events

    private void ResgisterEvents()
    {
        NetUtility.CLIENT_START_GAME += OnStartGameClient;
    }

    private void UnregisterEvents()
    {

        NetUtility.CLIENT_START_GAME -= OnStartGameClient;
    }

    private void OnStartGameClient(NetMessage obj)
    {
        animator.SetTrigger(InGameMenuHash);
    }

    #endregion
}

using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance { get; private set; }

    [SerializeField] private Animator animator;

    #region AnimationHash

    private int InGameMenuHash = Animator.StringToHash("InGameMenu");
    private int StartMenuHash = Animator.StringToHash("StartMenu");
    private int OnlineMenuHash = Animator.StringToHash("OnlineMenu");
    private int HostMenuHash = Animator.StringToHash("HostMenu");

    #endregion

    private void Awake()
    {
        Instance = this;
    }

    public void OnLocalGameButton()
    {
        animator.SetTrigger(InGameMenuHash);
    }
    public void OnOnlineGameButton()
    {
        animator.SetTrigger(OnlineMenuHash);
    }
    public void OnOnlineHostButton()
    {
        animator.SetTrigger(HostMenuHash);
    }
    public void OnOnlineConnectButton()
    {
        Debug.Log("Connect !");
    }
    public void OnOnlineBackButton()
    {
        animator.SetTrigger(StartMenuHash);
    }
    public void OnHostBackButton()
    {
        animator.SetTrigger(OnlineMenuHash);
    }
}

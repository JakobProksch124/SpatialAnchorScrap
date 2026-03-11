using UnityEngine;

public class IMessageTransitionCue : MonoBehaviour
{
    [Header("IMessage Cue Infos")]
    [Tooltip("Name of the child transform in the FBX model where the cue should appear")]
    [SerializeField] private string iMessageAnchorName = "iMessageAnchor";
    [SerializeField] private Color iMessagePrimaryColor = new Color(0.8f, 0.4f, 0f);
    [SerializeField] private string iMessageLabel = "VR";
    [SerializeField] private Texture2D iMessageScreenshotDisplayed;
    [SerializeField] private string iMessageDescription = "Welcome to the VR lecture!";
    [SerializeField] private string iMessageButtonText = "Start Video";
    [SerializeField] private bool iMessageAlwaysExpand = false;
    [Tooltip("only used, when start arrival cue is set to blunt")]
    [SerializeField] private Transform iMessageAnchor;
    [SerializeField] private Collider iMessageTrigger;
    [SerializeField] private AudioSource notificationSoundPlayer;
    [SerializeField] private AudioSource readNotificationPlayer;
    [SerializeField] private float iMessageDisappearDelay = 12f;
    [SerializeField] private float readNotificationDelay = 2f;
    
    private bool willReadNotification = true;
    
    private GameObject iMessageCue;
    private bool hasTriggered = false;
    private bool iMessageIsBland = false;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        Debug.Log("collision with player; creating i Message");
        hasTriggered = true;
        CreateIMessageArrivalCue(iMessageAnchor);
        
    }

    void CreateIMessageArrivalCue(Transform iMessageAnchor)
    {
        TransitionCueConfig iMessageCueConfig = TransitionCueConfig.CreateARConfig(
            parent: iMessageAnchor,
            onInteract: () =>
             {
                iMessageCue.SetActive(false);
                 readNotification();
             }
        );


        iMessageCueConfig.onCollide = (collision) =>
        {
            iMessageCue.SetActive(false);
            readNotification();
        };

        iMessageCueConfig.alwaysExpanded = iMessageAlwaysExpand;
        iMessageCueConfig.isBland = iMessageIsBland;
        iMessageCueConfig.isArrival = true;
        iMessageCueConfig.primaryColor = iMessagePrimaryColor;
        iMessageCueConfig.expandedDescription = iMessageDescription;
        iMessageCueConfig.screenshotTexture = iMessageScreenshotDisplayed;
        iMessageCueConfig.label = iMessageLabel;
        iMessageCueConfig.buttonText = iMessageButtonText;

        iMessageCue = TransitionCueFactory.CreateCue(iMessageCueConfig);
        UnityEngine.Debug.Log("iMessage cue created!");
        notificationSoundPlayer.Play();

        if (!iMessageIsBland)
        {
            Invoke(nameof(readNotification), readNotificationDelay);
        }
        else
        {
            willReadNotification = false;
        }
    }

    void setCueInvisible()
    {
        iMessageCue.SetActive(false);
    }

    void readNotification()
    {
        if (willReadNotification)
        {
            readNotificationPlayer.Play();
            this.willReadNotification = false;
        }
    }

    public void SetIsBland(bool iMessageIsBland)
    {
        this.iMessageIsBland = iMessageIsBland;
    }

}

using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FriendInfo1 : MonoBehaviour
{
    [HideInInspector] public string userID;
    
    [SerializeField] private TMP_Text user;
    [SerializeField] private Image online;
    [SerializeField] private Button chat, profile;
    [SerializeField] private RawImage icon;

    public TMP_Text Name { get => user; }
    public RawImage Icon { get => icon; }
    public Image Online { get => online; }
    public Button Chat { get => chat; }
    public Button Profile { get => profile; }

    public void SetProfile()
    {
        if (DatabaseManager.instance.prevProfile == "") DatabaseManager.instance.prevProfile = userID;
        else DatabaseManager.instance.prevProfile = DatabaseManager.instance.profileToLoad;
        DatabaseManager.instance.profileToLoad = userID;
    }
}
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FriendInfo2 : MonoBehaviour
{
    [HideInInspector] public string userID;
    
    [SerializeField] private TMP_Text user;
    [SerializeField] private RawImage icon;
    [SerializeField] private Button profile;

    public TMP_Text Name { get => user; }
    public RawImage Icon { get => icon; }
    public Button Profile { get => profile; }

    public void SetProfile()
    {
        DatabaseManager.instance.profileToLoad = userID;
        if (DatabaseManager.instance.prevProfile == "") DatabaseManager.instance.prevProfile = userID;
    }
}
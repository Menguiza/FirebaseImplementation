using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationInfo : MonoBehaviour
{
    [HideInInspector] public string userID;
    
    [SerializeField] private TMP_Text user;
    [SerializeField] private RawImage icon;
    [SerializeField] private Button accept, decline;
    
    public TMP_Text Name { get => user; }
    public RawImage Icon { get => icon; }
    public Button Accept { get => accept; }
    public Button Decline { get => decline; }

    public void DestroyDelayed()
    {
        DatabaseManager.instance.profilePending = userID;
        Destroy(gameObject, 2f);
    }
}

using UnityEngine;
using TMPro;

public class UpdateFriendCount : MonoBehaviour
{
    [SerializeField] private TMP_Text count;
    
    void Update()
    {
        count.text = transform.childCount.ToString();
    }
}

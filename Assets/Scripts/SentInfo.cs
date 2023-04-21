using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SentInfo : MonoBehaviour
{
    [HideInInspector] public string userID;
    
    [SerializeField] private TMP_Text user;
    [SerializeField] private RawImage icon;

    public TMP_Text Name { get => user; }
    public RawImage Icon { get => icon; }
}

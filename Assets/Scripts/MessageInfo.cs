using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MessageInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text message;
    [SerializeField] private RawImage icon;

    public TMP_Text Message { get => message; }
    public RawImage Icon { get => icon; }
}
using UnityEngine;
using UnityEngine.UI;

public class UpdatePendingNot : MonoBehaviour
{
    [SerializeField] private Image point;
    [SerializeField] private RectTransform content, content2;
    
    void Update()
    {
        if (content.childCount > 0 || content2.childCount > 0) point.enabled = true;
        else point.enabled = false;
    }
}

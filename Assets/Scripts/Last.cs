using UnityEngine;

public class Last : MonoBehaviour
{
    [SerializeField] private Back back;

    private bool selected;
    
    // Update is called once per frame
    void Update()
    {
        if (gameObject.activeSelf && !selected)
        {
            selected = true;
            back.last = gameObject;
        }
        else
        {
            selected = false;
        }
    }
}

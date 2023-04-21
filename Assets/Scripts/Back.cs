using UnityEngine;

public class Back : MonoBehaviour
{
    [HideInInspector] public GameObject last;

    public void Activate()
    {
        last.SetActive(true);
    }
}

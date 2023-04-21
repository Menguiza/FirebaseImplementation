using UnityEngine;
using TMPro;

public class Search : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private AuthManager authManager;

    public void OnSearch()
    {
        int searchLength = inputField.text.Length;

        foreach (FriendInfo2 element in DatabaseManager.instance.SearchUsers)
        {
            if (element.Name.text.Length >= searchLength)
            {
                if (element.Name.text == "" || element.userID == authManager.user.UserId)
                {
                    element.gameObject.SetActive(false);
                }
                else if (inputField.text.ToLower() == element.Name.text.Substring(0, searchLength).ToLower())
                {
                    element.gameObject.SetActive(true);
                }
                else
                {
                    element.gameObject.SetActive(false);
                }
            }
        }
    }
}

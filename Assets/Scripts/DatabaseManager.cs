using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Extensions;
using Firebase.Storage;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Networking;
using SimpleFileBrowser;

[RequireComponent(typeof(AuthManager))]
public class DatabaseManager : MonoBehaviour
{
    public string profileToLoad, prevProfile, profilePending;

    public static DatabaseManager instance;
    
    [Header("GamerTag")]
    [SerializeField] private TMP_InputField gtInputField;
    [SerializeField] private TMP_Text warningGT, succesGT;

    [Header("UserFriends-Messages")]
    [SerializeField] private RectTransform friendsContent, chatContent;
    [SerializeField] private GameObject friendPrefab, sendMessagePrefab, receiveMessagePrefab;
    [SerializeField] private Color online, offline;
    
    [Header("UserProfile")]
    [SerializeField] private TMP_Text gamerTag, nameField;
    [SerializeField] private RawImage profilePic;

    [Header("Chat")]
    [SerializeField] private TMP_Text chatUser;
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private RawImage chatPic;
    
    [Header("FriendProfile")]
    [SerializeField] private TMP_Text friendGamerTag, friendName;
    [SerializeField] private Image onlineIcon;
    [SerializeField] private RectTransform friendFriendsContent;
    [SerializeField] private GameObject userPrefab, mePrefab;
    [SerializeField] private RawImage friendPic;
    
    [Header("NotFriendProfile")]
    [SerializeField] private TMP_Text userGamerTag, userName;
    [SerializeField] private RectTransform userFriendsContent;
    [SerializeField] private Button addButton;
    [SerializeField] private RawImage notFriendPic;

    [Header("Notification")] 
    [SerializeField] private RectTransform notificationContent, sentContent;
    [SerializeField] private GameObject notificationPrefab, sentPrefab;

    [Header("Search")] 
    [SerializeField] private RectTransform searchContent;

    private AuthManager authManager;
    private Dictionary<string, string> gamerTags = new Dictionary<string, string>();
    private List<string> usersOnline = new List<string>(), friends = new List<string>(), friendsOfFriend = new List<string>(), friendsOfNotFriend = new List<string>(), sent = new List<string>(), pending = new List<string>(), messages = new List<string>();
    private List<FriendInfo1> friendsDisplay = new List<FriendInfo1>();
    private List<FriendInfo2> searchUsers = new List<FriendInfo2>();
    private List<SentInfo> sentList = new List<SentInfo>();
    private List<NotificationInfo> pendingList = new List<NotificationInfo>();
    private List<GameObject> friendsOfFriendDisplay = new List<GameObject>(), friendsOfNotFriendDisplay = new List<GameObject>();

    public UnityEvent FirsTime, NotFirsTime, TagSuccess, ChatRequested, ChatOpened, ProfileRequested, ProfileLoad,
        FriendRemoved, UpdateSearch;

    public List<FriendInfo2> SearchUsers { get => searchUsers; }

    private void Awake()
    {
        instance = this;
        authManager = GetComponent<AuthManager>();

        Application.quitting += Offline;
        
        FileBrowser.SetFilters(true, new FileBrowser.Filter("Images", ".png", ".jpg"));

        FileBrowser.SetDefaultFilter(".png");
        
        FileBrowser.SetExcludedExtensions(".lnk", ".tmp", ".zip", ".rar", ".exe", ".webp");
    }

    #region Methods

    public void InitializeUser()
    {
        StartCoroutine(UserSetDatabase());
    }

    public void CheckFirstTime()
    {
        StartCoroutine(CheckForFirstTime());
    }

    public void LoadInit()
    {
        authManager.dbReference.Child("Online").ChildAdded += HandleOnlineAdded;
        authManager.dbReference.Child("Online").ChildRemoved += HandleOnlineRemoved;
        
        authManager.dbReference.Child("Users").ChildAdded += HandleChildAdded;
        authManager.dbReference.Child("Users").ChildRemoved += HandleChildRemoved;
        
        friends.Clear();
        friendsDisplay.Clear();
        sent.Clear();
        sentList.Clear();
        pending.Clear();
        pendingList.Clear();
        
        UpdateSearch.Invoke();
    }

    public void AssignTag()
    {
        StartCoroutine(UpdateGamerTag(gtInputField.text));
    }

    public void Online()
    {
        StartCoroutine(SetUserOnline());
    }

    public void Offline()
    {
        StartCoroutine(SetUserOffline());
    }

    public void LoadChat()
    {
        StartCoroutine(SetChatInfo());
    }

    public void SendAMessage()
    {
        StartCoroutine(SendMessage());
    }

    public void LoadFriendProfile()
    {
        if (friends.Contains(profileToLoad)) StartCoroutine(SetFriendProfileInfo());
        else StartCoroutine(SetProfileInfo());
    }

    public void RemoveFriend()
    {
        StartCoroutine(RemovingFriend());
    }
    
    public void AddFriend()
    {
        StartCoroutine(AddSent());
    }
    
    public void FileBrowserShow(){
        StartCoroutine(UploadImageBrowser());
    }

    private void RemovePending()
    {
        StartCoroutine(RemovingPending());
    }

    private void AddFriendPending()
    {
        StartCoroutine(AddingFriend());
    }
    
    private void TagEvent()
    {
        TagSuccess.Invoke();
    }
    
    private void ChatReqEvent()
    {
        ChatRequested.Invoke();
    }
    
    private void ChatOpenEvent()
    {
        ChatOpened.Invoke();
    }
    
    private void ProfileEvent()
    {
        ProfileRequested.Invoke();
    }

    private void ProfileLoadEvent()
    {
        ProfileLoad.Invoke();
    }

    private void RemoveFriendEvent()
    {
        FriendRemoved.Invoke();
    }

    private bool CheckTagAviable(string tag)
    {
        bool result = true;

        foreach (KeyValuePair<string, string> kvp in gamerTags)
        {
            if (kvp.Value.ToUpper() == tag.ToUpper())
            {
                result = false;
                break;
            }
        }
        
        return result;
    }

    private void UpdateFriendStatus(string key, Color status)
    {
        foreach (FriendInfo1 friend in friendsDisplay)
        {
            if (friend.userID == key)
            {
                friend.Online.color = status;
                break;
            }
        }
    }

    private FriendInfo1 RemoveFriendFromList(string key)
    {
        foreach (FriendInfo1 friend in friendsDisplay)
        {
            if (friend.userID == key) return friend;
        }

        return null;
    }

    private GameObject RemoveFriendOfFriendList(string key, List<GameObject> friends)
    {
        foreach (GameObject friend in friends)
        {
            FriendInfo1 info;
            FriendInfo2 info2;
            
            if (friend.TryGetComponent(out info))
            {
                if (info.userID == key) return friend;
            }
            else if (friend.TryGetComponent(out info2))
            {
                if (info2.userID == key) return friend;
            }
        }

        return null;
    }

    private void ClearChilds(RectTransform transformTarget)
    {
        foreach (RectTransform child in transformTarget)
        {
            Destroy(child.gameObject);
        }
    }

    private void AssignFriendListeners(string key)
    {
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildAdded += HandleFriendAddedFriend;
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildRemoved += HandleFriendRemovedFriend;
    }

    private void RemoveFriendListeners(string key)
    {
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildAdded -= HandleFriendAddedFriend;
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildRemoved -= HandleFriendRemovedFriend;
    }
    
    private void AssignNotFriendListeners(string key)
    {
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildAdded += HandleNotFriendAddedFriend;
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildRemoved += HandleNotFriendRemovedFriend;
    }

    private void RemoveNotFriendListeners(string key)
    {
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildAdded -= HandleNotFriendAddedFriend;
        authManager.dbReference.Child("Users").Child(key).Child("Friends").ChildRemoved -= HandleNotFriendRemovedFriend;
    }

    private void AssignChatListeners(string key)
    {
        authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Chats").Child(key).ChildAdded += HandleMessageAdded;
    }
    
    private void RemoveChatListeners(string key)
    {
        authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Chats").Child(key).ChildAdded -= HandleMessageAdded;
    }
    
    private void StorageRequest(string key, RawImage icon)
    {
        authManager.storageReference.Child(key).Child(key +".png").GetDownloadUrlAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {task.Exception}");
            }
            else
            {
                StartCoroutine(LoadImage(key, icon, task.Result.ToString()));
            }
        });
    }

    private void UploadDefault()
    {
        var newMetadata = new MetadataChange();
        newMetadata.ContentType = "image/png";
        
        StorageReference uploadRef = authManager.storageReference.Child(authManager.user.UserId)
            .Child(authManager.user.UserId + ".png");
        
        uploadRef.PutFileAsync(Application.streamingAssetsPath + "/DefaultIcon.png", newMetadata)
            .ContinueWith((Task<StorageMetadata> task) =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.Log(task.Exception.ToString());
                }
                else
                {
                    Debug.Log("Finished uploading...");
                }
            });
    }

    #endregion

    #region Handlers

    #region GamerTags

    void HandleAddedGamerTag(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Key == "GamerTag")
        {
            authManager.dbReference.Child("Users").Child(args.Snapshot.Reference.Parent.Key).ChildAdded -=
                HandleAddedGamerTag;
            authManager.dbReference.Child("Users").ChildAdded += HandleChildAdded;
        }
    }

    void HandleChildAdded(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Child("GamerTag").Value == null)
        {
            authManager.dbReference.Child("Users").Child(args.Snapshot.Key).ChildAdded += HandleAddedGamerTag;
            return;
        }
        
        if (string.IsNullOrWhiteSpace(args.Snapshot.Child("GamerTag").Value.ToString()) || string.IsNullOrEmpty(args.Snapshot.Child("GamerTag").Value.ToString()))
        {
            authManager.dbReference.Child("Users").Child(args.Snapshot.Key).ChildChanged += HandleValueChanged;
            return;
        }

        if (args.Snapshot.Key != authManager.user.UserId && !gamerTags.ContainsKey(args.Snapshot.Key))
        {
            gamerTags.Add(args.Snapshot.Key, args.Snapshot.Child("GamerTag").Value.ToString());

            FriendInfo2 info = Instantiate(userPrefab, searchContent).GetComponent<FriendInfo2>();
            
            info.userID = args.Snapshot.Key;
            StartCoroutine(SetFriendOfFriendInfo(info.userID, info.Name, info.Icon));
            info.Profile.onClick.AddListener(ProfileEvent);
            searchUsers.Add(info);
            
            UpdateSearch.Invoke();
        }
    }
    
    void HandleValueChanged(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        gamerTags.Add(args.Snapshot.Reference.Parent.Key, args.Snapshot.Value.ToString());

        FriendInfo2 info = Instantiate(userPrefab, searchContent).GetComponent<FriendInfo2>();
            
        info.userID = args.Snapshot.Reference.Parent.Key;
        StartCoroutine(SetFriendOfFriendInfo(info.userID, info.Name, info.Icon));
        info.Profile.onClick.AddListener(ProfileEvent);
        searchUsers.Add(info);

        UpdateSearch.Invoke();
        
        authManager.dbReference.Child("Users").Child(args.Snapshot.Key).ChildChanged -= HandleValueChanged;
    }
    
    void HandleChildRemoved(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (gamerTags.Count > 0 && args.Snapshot.Key != authManager.user.UserId)
        {
            FriendInfo2 searchUser = null;
            
            foreach (FriendInfo2 element in searchUsers)
            {
                if (element.userID == args.Snapshot.Reference.Parent.Key)
                {
                    searchUser = element;
                    break;
                }
            }
            
            if (searchUser != null && gamerTags.ContainsKey(args.Snapshot.Key))
            {
                searchUsers.Remove(searchUser);
                Destroy(searchUser.gameObject);
            }
            
            gamerTags.Remove(args.Snapshot.Key);

            UpdateSearch.Invoke();
        }
    }

    #endregion

    #region OnlineUsers

    void HandleOnlineAdded(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Key != authManager.user.UserId && !usersOnline.Contains(args.Snapshot.Key))
        { 
            usersOnline.Add(args.Snapshot.Key);
            
            if(friends.Contains(args.Snapshot.Key)) UpdateFriendStatus(args.Snapshot.Key, online);
        }
    }
    
    void HandleOnlineRemoved(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        
        usersOnline.Remove(args.Snapshot.Key);
        
        if(friends.Contains(args.Snapshot.Key)) UpdateFriendStatus(args.Snapshot.Key, offline);
    }

    #endregion

    #region UserFriends

    void HandleFriendAdded(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Reference.Parent.Reference.Parent.Key != authManager.user.UserId) return;
        
        if (args.Snapshot.Key != authManager.user.UserId && !friends.Contains(args.Snapshot.Key))
        {
            GameObject friend = Instantiate(friendPrefab, friendsContent);
            FriendInfo1 info;
            if (friend.TryGetComponent(out info))
            {
                info.userID = args.Snapshot.Key;
                StartCoroutine(SetFriendInfo(info.userID, info.Name, info.Online, info.Icon));
                friendsDisplay.Add(info);
                friends.Add(info.userID);
                if(info.Chat != null) info.Chat.onClick.AddListener(ChatReqEvent);
                info.Profile.onClick.AddListener(ProfileEvent);
            }
            else
            {
                Destroy(friend);
            }
        }
    }
    
    void HandleFriendRemoved(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Reference.Parent.Reference.Parent.Key != authManager.user.UserId) return;
        
        FriendInfo1 info = RemoveFriendFromList(args.Snapshot.Key);

        if (info != null && friends.Contains(args.Snapshot.Key))
        {
            friendsDisplay.Remove(info);
            Destroy(info.gameObject);
            friends.Remove(args.Snapshot.Key);
        }
    }

    #endregion
    
    #region Chat

    void HandleMessageAdded(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Reference.Parent.Reference.Parent.Reference.Parent.Key != authManager.user.UserId) return;
        
        if (messages.Contains(args.Snapshot.Key)) return;
        
        messages.Add(args.Snapshot.Key);
        
        GameObject message = args.Snapshot.Key.EndsWith("SENT") ? Instantiate(sendMessagePrefab, chatContent) : Instantiate(receiveMessagePrefab, chatContent);
        
        message.transform.SetAsFirstSibling();
        
        string key = args.Snapshot.Key.EndsWith("SENT") ? authManager.user.UserId : profileToLoad;
        MessageInfo info;
            
        if (message.TryGetComponent(out info))
        {
            info.Message.text = args.Snapshot.Value.ToString();
            StorageRequest(key, info.Icon);
        }
        else
        {
            Destroy(message);
        }
    }
    
    #endregion

    #region FriendFriends

    void HandleFriendAddedFriend(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Reference.Parent.Reference.Parent.Key != profileToLoad) return;
        
        if (!friendsOfFriend.Contains(args.Snapshot.Key))
        {
            if (args.Snapshot.Key == authManager.user.UserId)
            {
                GameObject me = Instantiate(mePrefab, friendFriendsContent);

                MeIcon meInfo;
                if (me.TryGetComponent(out meInfo))
                {
                    friendsOfFriendDisplay.Add(me.gameObject);
                    friendsOfFriend.Add(authManager.user.UserId);
                    StorageRequest(authManager.user.UserId, meInfo.Icon);
                }
                
                return;
            }
            
            GameObject friend = friends.Contains(args.Snapshot.Key) ? Instantiate(friendPrefab, friendFriendsContent) : Instantiate(userPrefab, friendFriendsContent);
            FriendInfo1 info;
            FriendInfo2 info2;
            
            if (friend.TryGetComponent(out info))
            {
                info.userID = args.Snapshot.Key;
                StartCoroutine(SetFriendInfo(info.userID, info.Name, info.Online, info.Icon));
                friendsOfFriendDisplay.Add(info.gameObject);
                friendsOfFriend.Add(info.userID);
                if(info.Chat != null) info.Chat.onClick.AddListener(ChatReqEvent);
                info.Profile.onClick.AddListener(ProfileEvent);
            }
            else if (friend.TryGetComponent(out info2))
            {
                info2.userID = args.Snapshot.Key;
                StartCoroutine(SetFriendOfFriendInfo(info2.userID, info2.Name, info2.Icon));
                friendsOfFriendDisplay.Add(info2.gameObject);
                friendsOfFriend.Add(info2.userID);
                info2.Profile.onClick.AddListener(ProfileEvent);
            }
            else
            {
                Destroy(friend);
            }
        }
    }
    
    void HandleFriendRemovedFriend(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        
        if(args.Snapshot.Reference.Parent.Reference.Parent.Key != profileToLoad) return;

        GameObject friend = RemoveFriendOfFriendList(args.Snapshot.Key, friendsOfFriendDisplay);

        if (friend != null && friendsOfFriend.Contains(args.Snapshot.Key))
        {
            friendsOfFriendDisplay.Remove(friend);
            Destroy(friend);
            friendsOfFriend.Remove(args.Snapshot.Key);

            if (args.Snapshot.Key == authManager.user.UserId) StartCoroutine(SetProfileInfo());
        }
    }

    #endregion

    #region NotFriendFriends

    void HandleNotFriendAddedFriend(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        
        if (args.Snapshot.Reference.Parent.Reference.Parent.Key != profileToLoad) return;
        
        if (!friendsOfNotFriend.Contains(args.Snapshot.Key))
        {
            if (args.Snapshot.Key == authManager.user.UserId)
            {
                GameObject me = Instantiate(mePrefab, userFriendsContent);

                MeIcon meInfo;
                if (me.TryGetComponent(out meInfo))
                {
                    friendsOfNotFriendDisplay.Add(me.gameObject);
                    friendsOfNotFriend.Add(authManager.user.UserId);
                    StorageRequest(authManager.user.UserId, meInfo.Icon);
                }

                LoadFriendProfile();
                
                return;
            }
            
            GameObject friend = friends.Contains(args.Snapshot.Key) ? Instantiate(friendPrefab, userFriendsContent) : Instantiate(userPrefab, userFriendsContent);
            FriendInfo1 info;
            FriendInfo2 info2;
            
            if (friend.TryGetComponent(out info))
            {
                info.userID = args.Snapshot.Key;
                StartCoroutine(SetFriendInfo(info.userID, info.Name, info.Online, info.Icon));
                friendsOfNotFriendDisplay.Add(info.gameObject);
                friendsOfNotFriend.Add(info.userID);
                if(info.Chat != null) info.Chat.onClick.AddListener(ChatReqEvent);
                info.Profile.onClick.AddListener(ProfileEvent);
            }
            else if (friend.TryGetComponent(out info2))
            {
                info2.userID = args.Snapshot.Key;
                StartCoroutine(SetFriendOfFriendInfo(info2.userID, info2.Name, info2.Icon));
                friendsOfNotFriendDisplay.Add(info2.gameObject);
                friendsOfNotFriend.Add(info2.userID);
                info2.Profile.onClick.AddListener(ProfileEvent);
            }
            else
            {
                Destroy(friend);
            }
        }
    }
    
    void HandleNotFriendRemovedFriend(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if(args.Snapshot.Reference.Parent.Reference.Parent.Key != profileToLoad) return;

        GameObject friend = RemoveFriendOfFriendList(args.Snapshot.Key, friendsOfNotFriendDisplay);
        
        if (friend != null && friendsOfNotFriend.Contains(args.Snapshot.Key))
        {
            friendsOfNotFriendDisplay.Remove(friend);
            Destroy(friend);
            friendsOfNotFriend.Remove(args.Snapshot.Key);
        }
    }

    #endregion

    #region Notifications

    void HandlePendingAdded(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Reference.Parent.Reference.Parent.Reference.Parent.Key != authManager.user.UserId) return;
        
        if (args.Snapshot.Key != authManager.user.UserId && !pending.Contains(args.Snapshot.Key))
        {
            pending.Add(args.Snapshot.Key);
            NotificationInfo info = Instantiate(notificationPrefab, notificationContent)
                .GetComponent<NotificationInfo>();

            info.userID = args.Snapshot.Key;
            StartCoroutine(SetFriendOfFriendInfo(info.userID, info.Name, info.Icon));
            info.Accept.onClick.AddListener(AddFriendPending);
            info.Accept.onClick.AddListener(RemovePending);
            info.Decline.onClick.AddListener(RemovePending);
            pendingList.Add(info);
        }
    }
    
    void HandlePendingRemoved(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Reference.Parent.Reference.Parent.Reference.Parent.Key != authManager.user.UserId) return;
        
        if (pending.Count > 0 && args.Snapshot.Key != authManager.user.UserId)
        {
            pending.Remove(args.Snapshot.Key);
            foreach (NotificationInfo element in pendingList)
            {
                if (element.userID == args.Snapshot.Key)
                {
                    pendingList.Remove(element);
                    Destroy(element.gameObject);
                    break;
                }
            }
        }
    }
    
    void HandleSentAdded(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        
        if (args.Snapshot.Reference.Parent.Reference.Parent.Reference.Parent.Key != authManager.user.UserId) return;

        if (args.Snapshot.Key != authManager.user.UserId && !sent.Contains(args.Snapshot.Key))
        {
            sent.Add(args.Snapshot.Key);
            SentInfo info = Instantiate(sentPrefab, sentContent)
                .GetComponent<SentInfo>();

            info.userID = args.Snapshot.Key;
            StartCoroutine(SetFriendOfFriendInfo(info.userID, info.Name, info.Icon));
            sentList.Add(info);
        }
    }
    
    void HandleSentRemoved(object sender, ChildChangedEventArgs args) {
        if (args.DatabaseError != null) {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        if (args.Snapshot.Reference.Parent.Reference.Parent.Reference.Parent.Key != authManager.user.UserId) return;
        
        if (sent.Count > 0 && args.Snapshot.Key != authManager.user.UserId)
        {
            sent.Remove(args.Snapshot.Key);
            foreach (SentInfo element in sentList)
            {
                if (element.userID == args.Snapshot.Key)
                {
                    sentList.Remove(element);
                    Destroy(element.gameObject);
                    break;
                }
            }
        }
    }

    #endregion
    
    #endregion
    
    #region Coroutines

    private IEnumerator UserSetDatabase()
    {
        UploadDefault();
        
        var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Name")
            .SetValueAsync(authManager.user.DisplayName);
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        
        dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("GamerTag")
            .SetValueAsync("");
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
    }

    private IEnumerator CheckForFirstTime()
    {
        var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("GamerTag").GetValueAsync();
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = dbTask.Result;

            if (snapshot.Value.ToString() == "")
            {
                FirsTime.Invoke();
            }
            else
            {
                StartCoroutine(SetUserInfo());
                NotFirsTime.Invoke();
            }
        }
    }

    private IEnumerator UpdateGamerTag(string tag)
    {
        warningGT.text = "";
        
        if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrEmpty(tag))
        {
            warningGT.text = "Missing GamerTag";
        }
        else if (!CheckTagAviable(tag))
        {
            warningGT.text = "GamerTag Already In Use";
        }
        else
        {
            var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("GamerTag")
                .SetValueAsync(tag);
        
            yield return new WaitUntil(predicate: () => dbTask.IsCompleted);
        
            if (dbTask.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
            }

            succesGT.text = "GamerTag Successfully!";
            
            StartCoroutine(SetUserInfo());
            
            Invoke("TagEvent", 2f);
        }
    }

    private IEnumerator SetUserOnline()
    {
        var dbTask = authManager.dbReference.Child("Online").Child(authManager.user.UserId).SetValueAsync("");
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
    }
    
    private IEnumerator SetUserOffline()
    {
        if (authManager.user != null)
        {
            var dbTask = authManager.dbReference.Child("Online").Child(authManager.user.UserId).SetValueAsync(null);
        
            yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

            if (dbTask.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
            }
        
            ClearChilds(notificationContent);
            ClearChilds(sentContent);
            ClearChilds(friendsContent);
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Friends").ChildAdded -= HandleFriendAdded;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Friends").ChildRemoved -= HandleFriendRemoved;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Pending").ChildAdded -= HandlePendingAdded;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Pending").ChildRemoved -= HandlePendingRemoved;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Sent").ChildAdded -= HandleSentAdded;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Sent").ChildRemoved -= HandleSentRemoved;
        
            authManager.auth.SignOut();
        }
    }

    private IEnumerator SetFriendInfo(string key, TMP_Text nameContainer, Image onlineIcon, RawImage icon)
    {
        var dbTask = authManager.dbReference.Child("Users").Child(key).GetValueAsync();
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = dbTask.Result;

            nameContainer.text = snapshot.Child("Name").Value.ToString();
            StorageRequest(key, icon);
            if (usersOnline.Contains(key)) onlineIcon.color = online;
            else onlineIcon.color = offline;
        }
    }
    
    private IEnumerator SetUserInfo()
    {
        var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).GetValueAsync();
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = dbTask.Result;

            gamerTag.text = snapshot.Child("GamerTag").Value.ToString();
            nameField.text = snapshot.Child("Name").Value.ToString();
            StorageRequest(authManager.user.UserId, profilePic);

            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Friends").ChildAdded += HandleFriendAdded;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Friends").ChildRemoved += HandleFriendRemoved;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Pending").ChildAdded += HandlePendingAdded;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Pending").ChildRemoved += HandlePendingRemoved;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Sent").ChildAdded += HandleSentAdded;
            authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Sent").ChildRemoved += HandleSentRemoved;
        }
    }

    private IEnumerator SetChatInfo()
    {
        ClearChilds(chatContent);
        
        messages.Clear();
        
        RemoveChatListeners(prevProfile);
        
        var dbTask = authManager.dbReference.Child("Users").Child(profileToLoad).GetValueAsync();
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = dbTask.Result;

            chatUser.text = snapshot.Child("GamerTag").Value + $" ({snapshot.Child("Name").Value})";
            StorageRequest(profileToLoad, chatPic);
            
            AssignChatListeners(profileToLoad);

            Invoke("ChatOpenEvent", 1f);
        }
    }

    private IEnumerator SendMessage()
    {
        if (!string.IsNullOrWhiteSpace(chatInputField.text))
        {
            string key = DateTime.Now.ToString("MM:dd:yyyyHH:mm:ss");

            var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Chats").Child(profileToLoad).Child(key + "SENT").SetValueAsync(chatInputField.text);
        
            yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

            if (dbTask.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
            }
            else
            {
                var dbTask1 = authManager.dbReference.Child("Users").Child(profileToLoad).Child("Chats").Child(authManager.user.UserId).Child(key).SetValueAsync(chatInputField.text);
        
                yield return new WaitUntil(predicate: () => dbTask1.IsCompleted);

                if (dbTask1.Exception != null)
                {
                    Debug.LogWarning(message: $"Failed to register task with {dbTask1.Exception}");
                }
            }
        }

        chatInputField.text = "";
    }
    
    private IEnumerator SetFriendProfileInfo()
    {
        ClearChilds(friendFriendsContent);
        
        friendsOfFriend.Clear();
        friendsOfFriendDisplay.Clear();

        if (prevProfile != "" && prevProfile != profileToLoad) RemoveFriendListeners(prevProfile);

        var dbTask = authManager.dbReference.Child("Users").Child(profileToLoad).GetValueAsync();
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = dbTask.Result;

            AssignFriendListeners(profileToLoad);
            
            friendGamerTag.text = snapshot.Child("GamerTag").Value.ToString();
            friendName.text = snapshot.Child("Name").Value.ToString();
            StorageRequest(profileToLoad, friendPic);
            
            if (usersOnline.Contains(profileToLoad)) onlineIcon.color = online;
            else onlineIcon.color = offline;

            Invoke("ProfileLoadEvent", 1f);
        }
    }
    
    private IEnumerator SetFriendOfFriendInfo(string key, TMP_Text nameContainer, RawImage icon)
    {
        var dbTask = authManager.dbReference.Child("Users").Child(key).GetValueAsync();
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = dbTask.Result;

            nameContainer.text = snapshot.Child("GamerTag").Value.ToString();

            StorageRequest(key, icon);
        }
    }

    private IEnumerator RemovingFriend()
    {
        ClearChilds(friendFriendsContent);

        ClearChilds(friendFriendsContent);
        
        friendsOfFriend.Clear();
        friendsOfFriendDisplay.Clear();

        if (prevProfile != "" && prevProfile != profileToLoad) RemoveFriendListeners(profileToLoad);
        
        var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Friends").Child(profileToLoad).SetValueAsync(null);
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            var dbTask1 = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Chats").Child(profileToLoad).SetValueAsync(null);
        
            yield return new WaitUntil(predicate: () => dbTask1.IsCompleted);
            
            if (dbTask1.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask1.Exception}");
            }
            
            var dbTask3 = authManager.dbReference.Child("Users").Child(profileToLoad).Child("Friends").Child(authManager.user.UserId).SetValueAsync(null);
        
            yield return new WaitUntil(predicate: () => dbTask3.IsCompleted);
            
            if (dbTask3.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask3.Exception}");
            }
            
            var dbTask4 = authManager.dbReference.Child("Users").Child(profileToLoad).Child("Chats").Child(authManager.user.UserId).SetValueAsync(null);
        
            yield return new WaitUntil(predicate: () => dbTask4.IsCompleted);
            
            if (dbTask4.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask4.Exception}");
            }
            
            StartCoroutine(SetProfileInfo());
        }
    }
    
    private IEnumerator AddingFriend()
    {
        var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Friends").Child(profilePending).SetValueAsync("");
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            var dbTask1 = authManager.dbReference.Child("Users").Child(profilePending).Child("Friends").Child(authManager.user.UserId).SetValueAsync("");
        
            yield return new WaitUntil(predicate: () => dbTask1.IsCompleted);

            if (dbTask1.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask1.Exception}");
            }
        }
    }
    
    private IEnumerator SetProfileInfo()
    {
        ClearChilds(userFriendsContent);

        friendsOfNotFriend.Clear();
        friendsOfNotFriendDisplay.Clear();

        if (prevProfile != "" && prevProfile != profileToLoad) RemoveNotFriendListeners(prevProfile);

        var dbTask = authManager.dbReference.Child("Users").Child(profileToLoad).GetValueAsync();
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            DataSnapshot snapshot = dbTask.Result;

            AssignNotFriendListeners(profileToLoad);
            
            userGamerTag.text = snapshot.Child("GamerTag").Value.ToString();
            userName.text = snapshot.Child("Name").Value.ToString();
            StorageRequest(profileToLoad, notFriendPic);
            
            if(sent.Contains(profileToLoad)) addButton.gameObject.SetActive(false);
            else addButton.gameObject.SetActive(true);

            RemoveFriendEvent();
        }
    }

    private IEnumerator RemovingPending()
    {
        var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Pending").Child(profilePending).SetValueAsync(null);
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            var dbTask1 = authManager.dbReference.Child("Users").Child(profilePending).Child("Requests").Child("Sent").Child(authManager.user.UserId).SetValueAsync(null);
        
            yield return new WaitUntil(predicate: () => dbTask1.IsCompleted);

            if (dbTask1.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask1.Exception}");
            }
        }
    }

    private IEnumerator AddSent()
    {
        var dbTask = authManager.dbReference.Child("Users").Child(authManager.user.UserId).Child("Requests").Child("Sent").Child(profileToLoad).SetValueAsync("");
        
        yield return new WaitUntil(predicate: () => dbTask.IsCompleted);

        if (dbTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {dbTask.Exception}");
        }
        else
        {
            var dbTask1 = authManager.dbReference.Child("Users").Child(profileToLoad).Child("Requests").Child("Pending").Child(authManager.user.UserId).SetValueAsync("");
        
            yield return new WaitUntil(predicate: () => dbTask1.IsCompleted);

            if (dbTask1.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {dbTask1.Exception}");
            }
        }
    }

    private IEnumerator LoadImage(string key, RawImage icon, string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(message: $"Failed to register request with {request.error}");
        }
        else
        {
            icon.texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
        }
    }

    private IEnumerator UploadImageBrowser()
    {
        yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.FilesAndFolders, true, null, null, "Load Files and Folders", "Load");

        Debug.Log(FileBrowser.Success);

        if (FileBrowser.Success)
        {
            // Print paths of the selected files (FileBrowser.Result) (null, if FileBrowser.Success is false)
            for (int i = 0; i < FileBrowser.Result.Length; i++)
                Debug.Log(FileBrowser.Result[i]);

            Debug.Log("File Selected");
            byte[] bytes = FileBrowserHelpers.ReadBytesFromFile(FileBrowser.Result[0]);
            //Editing Metadata
            var newMetadata = new MetadataChange();
            newMetadata.ContentType = "image/png";

            //Create a reference to where the file needs to be uploaded
            StorageReference uploadRef = authManager.storageReference.Child(authManager.user.UserId).Child(authManager.user.UserId + ".png");
            
            Debug.Log("File upload started");
            uploadRef.PutBytesAsync(bytes, newMetadata).ContinueWithOnMainThread((task) => { 
                if(task.IsFaulted || task.IsCanceled){
                    Debug.Log(task.Exception.ToString());
                }
                else{
                    Debug.Log("File Uploaded Successfully!");
                    
                    StorageRequest(authManager.user.UserId, profilePic);
                }
            });
        }
    }
    
    #endregion
}
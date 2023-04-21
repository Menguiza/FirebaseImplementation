using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Storage;
using TMPro;
using UnityEngine.Events;

public class AuthManager : MonoBehaviour
{
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    
    public FirebaseAuth auth { get; private set; }
    public FirebaseUser user { get; private set; }
    public DatabaseReference dbReference { get; private set; }
    public StorageReference storageReference { get; private set; }
    private FirebaseStorage storage;

    [Header("Login")] 
    [SerializeField] private TMP_InputField emailLogin, passwordLogin;
    [SerializeField] private TMP_Text warningLogin, confirmLogin;
    
    [Header("Register")] 
    [SerializeField] private TMP_InputField nameRegister, emailRegister, passwordRegister, confirmPassRegister;
    [SerializeField] private TMP_Text warningRegister, confirmRegister;

    public UnityEvent PrevLog, Logged, Registered;

    private void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                print("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }
    
    #region Methods

    private void InitializeFirebase()
    {
        Debug.Log("Setting up Firebase");
        
        auth = FirebaseAuth.DefaultInstance;
        dbReference = FirebaseDatabase.DefaultInstance.RootReference;
        storage = FirebaseStorage.DefaultInstance;
        storageReference = storage.GetReferenceFromUrl("gs://fir-implementation-cab14.appspot.com");
    }

    public void LoginButton()
    {
        StartCoroutine(Login(emailLogin.text, passwordLogin.text));
    }

    public void RegisterButton()
    {
        warningRegister.text = "";
        StartCoroutine(Register(nameRegister.text, emailRegister.text, passwordRegister.text, confirmPassRegister.text));
    }

    private void LoggedEvent()
    {
        Logged.Invoke();
    }
    
    private void RegisteredEvent()
    {
        Registered.Invoke();
    }

    #endregion

    #region Coroutines

    IEnumerator Login(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(predicate: () => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            Debug.LogWarning(message: $"Failed to register task with {loginTask.Exception}");
            
            FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;

            AuthError errorCode = (AuthError)firebaseException.ErrorCode;

            string message = "Login failed!";

            switch (errorCode)
            {
                case AuthError.MissingEmail:
                    message = "Missing Email";
                    break;
                case AuthError.MissingPassword:
                    message = "Missing Password";
                    break;
                case AuthError.InvalidEmail:
                    message = "Invalid Email";
                    break;
                case AuthError.WrongPassword:
                    message = "Wrong Password";
                    break;
                case AuthError.UserNotFound:
                    message = "Account doesn't exist";
                    break;
            }

            warningLogin.text = message;
        }
        else
        {
            user = loginTask.Result;
            Debug.LogFormat($"User signed in successfully: {user.DisplayName}, {user.Email}");

            warningLogin.text = "";
            confirmLogin.text = "Logged In";
            PrevLog.Invoke();
            Invoke("LoggedEvent", 2f);
        }
    }

    IEnumerator Register(string name, string email, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrEmpty(name))
        {
            warningRegister.text = "Missing Name";
        }
        else if (password != confirmPassword)
        {
            warningRegister.text = "Password Doesn't Match";
            print("password: " + password + ", confirm: " + confirmPassword);
        }
        else
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

            yield return new WaitUntil(predicate: () => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                Debug.LogWarning(message: $"Failed to register task with {registerTask.Exception}");
            
                FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException;

                AuthError errorCode = (AuthError)firebaseException.ErrorCode;
                
                string message = "Register failed!";

                switch (errorCode)
                {
                    case AuthError.MissingEmail:
                        message = "Missing Email";
                        break;
                    case AuthError.MissingPassword:
                        message = "Missing Password";
                        break;
                    case AuthError.InvalidEmail:
                        message = "Invalid Email";
                        break;
                    case AuthError.WeakPassword:
                        message = "Weak Password";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        message = "Email Already In Use";
                        break;
                }

                warningRegister.text = message;
            }
            else
            {
                user = registerTask.Result;

                if (user != null)
                {
                    UserProfile profile = new UserProfile { DisplayName = name };
                    
                    var profileTask = user.UpdateUserProfileAsync(profile);

                    yield return new WaitUntil(predicate: () => profileTask.IsCompleted);

                    if (profileTask.Exception != null)
                    {
                        Debug.LogWarning(message: $"Failed to register task with {registerTask.Exception}");

                        warningRegister.text = "Name Set Failed!";
                    }
                    else
                    {
                        warningRegister.text = "";
                        confirmRegister.text = "Registered Successfully!";
                        Invoke("RegisteredEvent", 2f);
                    }
                }
            }
        }
    }

    #endregion
}

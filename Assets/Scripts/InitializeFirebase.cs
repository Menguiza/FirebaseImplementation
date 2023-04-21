using UnityEngine;
using Firebase;
using UnityEngine.Events;

public class InitializeFirebase : MonoBehaviour
{
    public DependencyStatus dependencyStatus;
    public UnityEvent Initialize;
    
    private void Awake()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                Initialize.Invoke();
                print("Called Initialize");
            }
            else
            {
                print("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }
        });
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            print("pressed");
            
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
            {
                dependencyStatus = task.Result;

                if (dependencyStatus == DependencyStatus.Available)
                {
                    Initialize.Invoke();
                    print("Called Initialize");
                }
                else
                {
                    print("Could not resolve all Firebase dependencies: " + dependencyStatus);
                }
            });
        }
    }
}

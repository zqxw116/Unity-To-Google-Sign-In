using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using Google;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FirebaseManager : MonoBehaviour
{

    // Firebase ���� �ν��Ͻ�
    private FirebaseAuth _auth; // Firebase ���� ��ü
    private FirebaseUser _user; // �α��ε� ����� ����

    // UI ����
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private TextMeshProUGUI userEmailText;
    [SerializeField] private GameObject loginScreen;
    [SerializeField] private GameObject profileScreen;
    [SerializeField] private Button btnSign;



    private void Start()
    {
        btnSign.onClick.AddListener(OnClickGoogleSign);
        loginScreen.SetActive(true);
        profileScreen.SetActive(false);

        // GoogleSignIn.Configuration:
        // GoogleSignIn�� ���������� ����� ����. �ݵ�� �� ���� �ʱ⿡ �����ؾ� ��.

        // GoogleSignInConfiguration:
        // Google �α����� �����ϴ� ���� ��ü. WebClientId�� Firebase �ֿܼ��� ������ ���̾�� ��.

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            //WebClientId = "���⿡_WebClientID_�Է��ϼ���", // Firebase �ֿܼ��� �߱޹��� WebClientId
            WebClientId = "616520914294-8ukccj4d0rl8jrtff76v40m1lq06hkd1.apps.googleusercontent.com", 
            RequestIdToken = true,                        // ID Token ��û (Firebase ������ �ʿ�
            RequestEmail = true,                          // ����� �̸��� ��û
            UseGameSignIn = false                         // ���� ���� ������ ������� ���� (�Ϲ� �α��ο�)
        };
        InitializeFirebase();
    }

    /// <summary>
    /// Firebase ������ Ȯ�� �� ���� �ν��Ͻ� �ʱ�ȭ
    /// </summary>
    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                _auth = FirebaseAuth.DefaultInstance;
                Log("O Firebase Auth Initializer.");
            }
            else
            {
                LogError($"X Firebase dependency error: {task.Result}");
            }
        });
    }

    /// <summary>
    /// Google Sign-In ��ư Ŭ�� �� ����
    /// </summary>
    public void OnClickGoogleSign()
    {
        // ���� Google �������� �α��� ��û�� ����. ����� �񵿱� Task<GoogleSignInUser>�� ��ȯ��.
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleSignInCompleted);
    }

    /// <summary>
    /// Google Sign-In ��� ó��
    /// </summary>
    private void OnGoogleSignInCompleted(Task<GoogleSignInUser> task)
    {
        if (task.IsCanceled)
        {
            LogError("! Google Sign-In was canceled.");
            return;
        }

        //if (task.IsFaulted)
        //{
        //    LogError($"X Google Sign-In failed:\n{task.Exception}");
        //    return;
        //}

        if (task.Exception != null)
        {
            //foreach (var ex in task.Exception.Flatten().InnerExceptions)
            //{
            //    LogError($"Google Sign-In Exception: {ex.GetType()} - {ex.Message}\n{ex.StackTrace}");
            //}

            // �������ϰ�
            foreach (var ex in task.Exception.Flatten().InnerExceptions)
            {
                LogError($"[EX TYPE] {ex.GetType()}");
                LogError($"[EX MESSAGE] {ex.Message}");

                if (ex is Google.GoogleSignIn.SignInException gEx)
                {
                    // ���÷������� ��� �Ӽ� ���
                    var props = gEx.GetType().GetProperties();
                    foreach (var prop in props)
                    {
                        try
                        {
                            var value = prop.GetValue(gEx);
                            LogError($"[PROP] {prop.Name} = {value}");
                        }
                        catch { }
                    }
                }
            }
        }

        // GoogleSignInUser:
        // Google �α��ο� �������� �� ��ȯ�Ǵ� ����� ���� ��ü.
        // �� ��ü���� Firebase ������ �ʿ��� IdToken�� ������ �� ����.
        var googleUser = task.Result; // Google ����� ������ ��ū

        // Google IdToken�� Firebase���� ����� �� �ִ� Credential ��ü�� ��ȯ.
        // ���� FirebaseAuth�� �� Credential�� ����� �α��� ����.
        var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

        // Firebase ���� �ý��ۿ� ������ ������ Credential�� �α��� �õ�.
        _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(OnFirebaseSignInCompleted);
    }

    /// <summary>
    /// Firebase ���� ��� ó��
    /// </summary>
    private void OnFirebaseSignInCompleted(Task<FirebaseUser> task)
    {
        if (task.IsCanceled)
        {
            LogError("! Firebase sign-in was canceled.");
            return;
        }

        if (task.IsFaulted)
        {
            LogError($"X Firebase sign-in failed:\n{task.Exception}");
            return;
        }

        _user = _auth.CurrentUser;

        if (_user != null)
        {
            usernameText.text = _user.DisplayName ?? "No Name";
            userEmailText.text = _user.Email ?? "No Email";

            loginScreen.SetActive(false);
            profileScreen.SetActive(true);

            Log($"O Logged in as: {_user.DisplayName} ({_user.Email})");
        }
    }

    #region Logging Helpers

    private void Log(string message)
    {
        Debug.Log(message);
        debugText.text = message;
    }

    private void LogError(string message)
    {
        Debug.LogError(message);
        debugText.text = $"<color=red>{message}</color>";
    }

    #endregion
}
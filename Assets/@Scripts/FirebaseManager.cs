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

    // Firebase 인증 인스턴스
    private FirebaseAuth _auth; // Firebase 인증 객체
    private FirebaseUser _user; // 로그인된 사용자 정보

    // UI 연결
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
        // GoogleSignIn이 내부적으로 사용할 설정. 반드시 앱 실행 초기에 설정해야 함.

        // GoogleSignInConfiguration:
        // Google 로그인을 설정하는 구성 객체. WebClientId는 Firebase 콘솔에서 복사한 값이어야 함.

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            //WebClientId = "여기에_WebClientID_입력하세요", // Firebase 콘솔에서 발급받은 WebClientId
            WebClientId = "616520914294-8ukccj4d0rl8jrtff76v40m1lq06hkd1.apps.googleusercontent.com", 
            RequestIdToken = true,                        // ID Token 요청 (Firebase 인증에 필요
            RequestEmail = true,                          // 사용자 이메일 요청
            UseGameSignIn = false                         // 게임 서비스 연동은 사용하지 않음 (일반 로그인용)
        };
        InitializeFirebase();
    }

    /// <summary>
    /// Firebase 의존성 확인 및 인증 인스턴스 초기화
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
    /// Google Sign-In 버튼 클릭 시 실행
    /// </summary>
    public void OnClickGoogleSign()
    {
        // 실제 Google 계정으로 로그인 요청을 보냄. 결과는 비동기 Task<GoogleSignInUser>로 반환됨.
        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(OnGoogleSignInCompleted);
    }

    /// <summary>
    /// Google Sign-In 결과 처리
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

            // 디테일하게
            foreach (var ex in task.Exception.Flatten().InnerExceptions)
            {
                LogError($"[EX TYPE] {ex.GetType()}");
                LogError($"[EX MESSAGE] {ex.Message}");

                if (ex is Google.GoogleSignIn.SignInException gEx)
                {
                    // 리플렉션으로 모든 속성 출력
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
        // Google 로그인에 성공했을 때 반환되는 사용자 정보 객체.
        // 이 객체에서 Firebase 인증에 필요한 IdToken을 가져올 수 있음.
        var googleUser = task.Result; // Google 사용자 인증용 토큰

        // Google IdToken을 Firebase에서 사용할 수 있는 Credential 객체로 변환.
        // 이후 FirebaseAuth로 이 Credential을 사용해 로그인 가능.
        var credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, null);

        // Firebase 인증 시스템에 위에서 생성한 Credential로 로그인 시도.
        _auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(OnFirebaseSignInCompleted);
    }

    /// <summary>
    /// Firebase 인증 결과 처리
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
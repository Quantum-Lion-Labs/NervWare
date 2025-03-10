#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using ModIO;
using NervWareSDK.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public class WelcomePanel : EditorWindow
{
    static WelcomePanel()
    {
        if (!SessionState.GetBool("WelcomePanelEnabled", false))
        {
            EditorApplication.update += EditorUpdate;
        }
    }

    private static void EditorUpdate()
    {
        ShowExample();
        SessionState.SetBool("WelcomePanelEnabled", true);
        EditorApplication.update -= EditorUpdate;
    }
    
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    [MenuItem("NervWare/Welcome", false, 10)]
    public static void ShowExample()
    {
        WelcomePanel wnd = GetWindow<WelcomePanel>();
        wnd.minSize = new Vector2(600, 500);
        wnd.titleContent = new GUIContent("Welcome");
    }


    private bool uiInitialized = false;
    
    private TextElement loginLabel;
    private Button logoutButton;
    
    private TextField emailTextField;
    private Button submitEmailButton;
    private TextField authTextField;
    private Button submitAuthButton;
    private Button validateButton;
    
    private VisualElement authWindow;
    private VisualElement emailContainer;
    private VisualElement authContainer;

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        // VisualElement label = new Label("Hello World! From C#");
        // root.Add(label);

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        CheckFonts(labelFromUXML);
        root.Add(labelFromUXML);
        
        loginLabel = root.Q<TextElement>("LoginLabel");
        logoutButton = root.Q<Button>("LogoutButton");
        validateButton = root.Q<Button>("ValidateButton");
        // Get containers
        authWindow = root.Q<VisualElement>("AuthContainer");
        emailContainer = root.Q<VisualElement>("CodeRequestContainer");
        authContainer = root.Q<VisualElement>("CodeSubmitContainer");
        
        
        // Get the text box
        emailTextField = root.Q<TextField>("EmailTextInput");
        submitEmailButton = root.Q<Button>("SubmitEmail");
        
        authTextField = root.Q<TextField>("AuthCodeTextInput");
        submitAuthButton = root.Q<Button>("SubmitCode");

        submitEmailButton.clicked += RequestAuthCode;
        submitAuthButton.clicked += SubmitAuthCode;
        logoutButton.clicked += Logout;
        validateButton.clicked += ValidateProject;

        ResetUI();

        uiInitialized = true;
    }

    private void ValidateProject()
    {
        NervWareValidator.ValidateAll();
    }

    private void CheckFonts(VisualElement element)
    {
        foreach (var child in element.Children())
        {
            if (child.style.unityFont == null)
            {
                Debug.LogError("Missing font!");
            }
            CheckFonts(child);
        }
    }

    private void ResetUI()
    {
        loginLabel.text = $"Log in to Mod.io";
        emailContainer.style.display = DisplayStyle.None;
        authContainer.style.display = DisplayStyle.None;
        logoutButton.style.display = DisplayStyle.None;
    }

    private void OnEnable()
    {
        Result result = ModIOUnity.InitializeForUser("default");
        if (!result.Succeeded()) {
            Debug.LogError($"ModIO plugin failed to initialize. {result.message}");
            return;
        }

        Debug.Log("ModIO plugin initialized!");
        
        OnInit();
    }

    async void OnInit()
    {
        Result result = await ModIOUnityAsync.IsAuthenticated();

        while (uiInitialized == false) {
            await Task.Yield();
        }
        
        if (result.Succeeded())
        {
            OnAuth();
            return;
        }
        else {
            emailContainer.style.display = DisplayStyle.Flex;
        }
    }
   
    async void RequestAuthCode()
    {
        Result result = await ModIOUnityAsync.RequestAuthenticationEmail(emailTextField.value);
        
        if (!result.Succeeded())
        {
            return;
        }
        
        Debug.Log($"Authentication email sent to: {emailTextField.value}");
        emailContainer.style.display = DisplayStyle.None;
        authContainer.style.display = DisplayStyle.Flex;
    }

    async void SubmitAuthCode()
    {
        Result result = await ModIOUnityAsync.SubmitEmailSecurityCode(authTextField.text);

        if (!result.Succeeded())
        {
            ResetUI();
            return;
        }
    
        OnAuth();
    }
   
    async void OnAuth()
    {
        ResultAnd<UserProfile> result = await ModIOUnityAsync.GetCurrentUser();
        if (!result.result.Succeeded())
        {
            Debug.LogError($"GetCurrentUser failed: {result.result.message}");
        }

        Debug.Log($"Authenticated user: {result.value.username}");
        emailContainer.style.display = DisplayStyle.None;
        authContainer.style.display = DisplayStyle.None;
        loginLabel.text = $"Logged in to Mod.io as {result.value.username}";
        logoutButton.style.display = DisplayStyle.Flex;
    }
    
    private void Logout()
    {
        Debug.Log("Logging out.");
        ModIOUnity.LogOutCurrentUser();
        ResetUI();
        emailContainer.style.display = DisplayStyle.Flex;
    }
}
#endif
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MP_FPS.Client
{
    [RequireComponent(typeof(UIDocument))]
    public class MainMenu : MonoBehaviour
    {
        static class UIElementNames
        {
            public const string HidingBackground = "HidingBackground";
            public const string NameInputField = "PlayerNameField";
            public const string ChooseCharacterOption = "ChooseCharacterOption";
            public const string ConnectionModeOption = "ConnetionModeOption";
            public const string SessionNameLabel = "SessionName";
            public const string SessionInputField = "SessionNameField";
            public const string CreateGame = "CreateJoinGame";
            public const string StartHost = "StartHost";
            public const string ConnectToServer = "ConnectToServer";
            public const string QuitGame = "QuitButton";
        }

        VisualElement m_MainMenu;
        VisualElement m_NinjaThumb;
        VisualElement m_ShogunThumb;
        RadioButtonGroup m_ChosseCharacterGroup;
        RadioButtonGroup m_ConnectionModeGroup;
        Label m_SessionNameLabel;
        TextField m_SessionNameField;
        Button m_CreateGameButton;
        Button m_StartHostButton;
        Button m_ConnectToServerButton;
        Button m_QuitButton;

        void OnEnable()
        {
            m_MainMenu = GetComponent<UIDocument>().rootVisualElement;

            m_MainMenu.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.MainMenuStylePropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            var nameInputField = m_MainMenu.Q<TextField>(UIElementNames.NameInputField);
            nameInputField.SetBinding("value", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(GameSettings.PlayerName)),
                bindingMode = BindingMode.TwoWay,
            });

            m_SessionNameLabel = m_MainMenu.Q<Label>(UIElementNames.SessionNameLabel);
            var connectionMode = m_ConnectionModeGroup = m_MainMenu.Q<RadioButtonGroup>(UIElementNames.ConnectionModeOption);
            connectionMode.SetBinding("value", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(GameSettings.ConnectionMode)),
                bindingMode = BindingMode.TwoWay,
            });
            m_ConnectionModeGroup.RegisterValueChangedCallback(OnConnectionModeChanged);

            var sessionInputField = m_SessionNameField = m_MainMenu.Q<TextField>(UIElementNames.SessionInputField);
            sessionInputField.SetBinding("value", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(nameof(GameSettings.SessionName)),
                bindingMode = BindingMode.TwoWay,
            });

            m_CreateGameButton = m_MainMenu.Q<Button>(UIElementNames.CreateGame);
            m_CreateGameButton.clicked += OnCreateGamePressed;

            m_StartHostButton = m_MainMenu.Q<Button>(UIElementNames.StartHost);
            m_StartHostButton.clicked += OnStartHostPressed;

            m_ConnectToServerButton = m_MainMenu.Q<Button>(UIElementNames.ConnectToServer);
            m_ConnectToServerButton.clicked += OnConnectToServerPressed;

            m_QuitButton = m_MainMenu.Q<Button>(UIElementNames.QuitGame);
            m_QuitButton.clicked += OnQuitPressed;

            var hidingBackground = m_MainMenu.Q<VisualElement>(UIElementNames.HidingBackground);
            hidingBackground.SetBinding("style.display", new DataBinding
            {
                dataSource = GameSettings.Instance,
                dataSourcePath = new PropertyPath(GameSettings.MainMenuSceneLoadedPropertyName),
                bindingMode = BindingMode.ToTarget,
            });

            // Character cards — click a card to choose; the selected one is highlighted.
            m_NinjaThumb = m_MainMenu.Q<VisualElement>("NinjaThumb");
            m_ShogunThumb = m_MainMenu.Q<VisualElement>("ShogunThumb");
            var ninjaCard = m_MainMenu.Q<VisualElement>("NinjaCard");
            var shogunCard = m_MainMenu.Q<VisualElement>("ShogunCard");
            var ninjaTex = Resources.Load<Texture2D>("Characters/ninja");
            var shogunTex = Resources.Load<Texture2D>("Characters/shogun");
            if (m_NinjaThumb != null && ninjaTex != null) m_NinjaThumb.style.backgroundImage = new StyleBackground(ninjaTex);
            if (m_ShogunThumb != null && shogunTex != null) m_ShogunThumb.style.backgroundImage = new StyleBackground(shogunTex);
            ninjaCard?.RegisterCallback<ClickEvent>(_ => SelectCharacter(0));
            shogunCard?.RegisterCallback<ClickEvent>(_ => SelectCharacter(1));
            UpdateCharacterHighlight();

            ToggleConnectionModeDisplay();
        }

        void OnDisable()
        {
            m_CreateGameButton.clicked -= OnCreateGamePressed;
            m_ConnectionModeGroup.UnregisterValueChangedCallback(OnConnectionModeChanged);
            m_ConnectToServerButton.clicked -= OnConnectToServerPressed;
            m_QuitButton.clicked -= OnQuitPressed;
        }

        void OnConnectionModeChanged(ChangeEvent<int> evt)
        {
            GameSettings.Instance.ConnectionMode = evt.newValue;
            ToggleConnectionModeDisplay();
        }

        void SelectCharacter(int index)
        {
            GameSettings.Instance.PlayerCharacter = index;
            UpdateCharacterHighlight();
        }

        void UpdateCharacterHighlight()
        {
            int sel = GameSettings.Instance.PlayerCharacter;
            SetThumbHighlight(m_NinjaThumb, sel == 0);
            SetThumbHighlight(m_ShogunThumb, sel == 1);
        }

        static void SetThumbHighlight(VisualElement thumb, bool selected)
        {
            if (thumb == null) return;
            var color = selected ? new Color(1f, 0.84f, 0.25f) : new Color(0.47f, 0.86f, 0.63f);
            thumb.style.borderLeftColor = color;
            thumb.style.borderRightColor = color;
            thumb.style.borderTopColor = color;
            thumb.style.borderBottomColor = color;
            float w = selected ? 5f : 2f;
            thumb.style.borderLeftWidth = w;
            thumb.style.borderRightWidth = w;
            thumb.style.borderTopWidth = w;
            thumb.style.borderBottomWidth = w;
        }

        void ToggleConnectionModeDisplay()
        {
            bool single = GameSettings.Instance.ConnectionMode == 0; // "Single"

            // The online relay/session flow is no longer exposed; everything uses direct host/join.
            m_SessionNameLabel.style.display = m_SessionNameField.style.display = DisplayStyle.None;
            m_CreateGameButton.style.display = DisplayStyle.None;

            m_StartHostButton.style.display = DisplayStyle.Flex;
            m_StartHostButton.text = single ? "Start Game" : "Host Game";
            m_ConnectToServerButton.style.display = single ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void Start()
        {

        }

        static void OnCreateGamePressed() => GameManager.Instance.StartGameAsync(CreationType.CreateOrJoin);

        static void OnStartHostPressed() => GameManager.Instance.StartGameAsync(CreationType.Host);

        static void OnConnectToServerPressed() => GameManager.Instance.StartGameAsync(CreationType.ConnectAndJoin);

        static void OnQuitPressed() => GameManager.Instance.QuitAsync();
    }
}

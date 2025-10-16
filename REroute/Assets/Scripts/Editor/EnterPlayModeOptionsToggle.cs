using UnityEditor;

public static class EnterPlayModeOptionsToggle
    {
        private const string MenuName = "PlayModeOptions";
        private const string EnableMenu = MenuName + "/Enable";
        private const string DomainReloadMenu = MenuName + "/Reload Domain";
        private const string SceneReloadMenu = MenuName + "/Reload Scene";
        
        [MenuItem(EnableMenu, priority = 0)]
        public static void Enable()
        {
            EditorSettings.enterPlayModeOptionsEnabled = !EditorSettings.enterPlayModeOptionsEnabled;
        }

        [MenuItem(EnableMenu, true)]
        public static bool EnableValidation()
        {
            UnityEditor.Menu.SetChecked(EnableMenu, EditorSettings.enterPlayModeOptionsEnabled);
            return true;
        }

        private static bool domainReloadEnabled => (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableDomainReload) !=
                                                   EnterPlayModeOptions.DisableDomainReload;
        
        [MenuItem(DomainReloadMenu, priority = 20)]
        public static void EnableDomainReload()
        {
            if (domainReloadEnabled)
                EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableDomainReload;
            else
                EditorSettings.enterPlayModeOptions &= ~EnterPlayModeOptions.DisableDomainReload;
        }

        [MenuItem(DomainReloadMenu, true)]
        public static bool EnableDomainReloadValidation()
        {
            UnityEditor.Menu.SetChecked(DomainReloadMenu, domainReloadEnabled);
            return EditorSettings.enterPlayModeOptionsEnabled;
        }
        
        private static bool sceneReloadEnabled => (EditorSettings.enterPlayModeOptions & EnterPlayModeOptions.DisableSceneReload) !=
                                                  EnterPlayModeOptions.DisableSceneReload;

        [MenuItem(SceneReloadMenu, priority = 40)]
        public static void EnableSceneReload()
        {
            if (sceneReloadEnabled)
                EditorSettings.enterPlayModeOptions |= EnterPlayModeOptions.DisableSceneReload;
            else
                EditorSettings.enterPlayModeOptions &= ~EnterPlayModeOptions.DisableSceneReload;
        }

        [MenuItem(SceneReloadMenu, true)]
        public static bool EnableSceneReloadValidation()
        {
            UnityEditor.Menu.SetChecked(SceneReloadMenu, sceneReloadEnabled);
            return EditorSettings.enterPlayModeOptionsEnabled;
        }
    }

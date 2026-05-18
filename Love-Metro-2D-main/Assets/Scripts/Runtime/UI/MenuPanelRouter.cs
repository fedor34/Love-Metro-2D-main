using UnityEngine;

namespace LoveMetro.UI
{
    internal sealed class MenuPanelRouter
    {
        private readonly GameObject _mainMenuPanel;
        private readonly GameObject _charactersPanel;
        private readonly GameObject _settingsPanel;

        public MenuPanelRouter(
            GameObject mainMenuPanel,
            GameObject charactersPanel,
            GameObject settingsPanel)
        {
            _mainMenuPanel = mainMenuPanel;
            _charactersPanel = charactersPanel;
            _settingsPanel = settingsPanel;
            CurrentPanel = MenuPanelId.Main;
        }

        public MenuPanelId CurrentPanel { get; private set; }

        public void Show(MenuPanelId panelId)
        {
            SetActive(_mainMenuPanel, false);
            SetActive(_charactersPanel, false);
            SetActive(_settingsPanel, false);

            SetActive(ResolvePanel(panelId), true);
            CurrentPanel = panelId;
        }

        public bool IsVisible(MenuPanelId panelId)
        {
            GameObject panel = ResolvePanel(panelId);
            return panel != null && panel.activeInHierarchy;
        }

        private GameObject ResolvePanel(MenuPanelId panelId)
        {
            switch (panelId)
            {
                case MenuPanelId.Characters:
                    return _charactersPanel;
                case MenuPanelId.Settings:
                    return _settingsPanel;
                default:
                    return _mainMenuPanel;
            }
        }

        private static void SetActive(GameObject panel, bool active)
        {
            if (panel != null)
                panel.SetActive(active);
        }
    }
}

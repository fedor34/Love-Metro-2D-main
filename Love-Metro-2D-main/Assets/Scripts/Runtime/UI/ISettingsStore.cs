namespace LoveMetro.UI
{
    internal interface ISettingsStore
    {
        SettingsSnapshot Load();
        void Save(SettingsSnapshot settings);
    }
}

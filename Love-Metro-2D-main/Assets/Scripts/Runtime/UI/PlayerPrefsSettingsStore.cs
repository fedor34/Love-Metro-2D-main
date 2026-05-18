using UnityEngine;

namespace LoveMetro.UI
{
    internal sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        public const string MasterVolumeKey = "MasterVolume";
        public const string MusicVolumeKey = "MusicVolume";
        public const string SfxVolumeKey = "SFXVolume";
        public const string MuteKey = "Mute";
        public const string QualityKey = "Quality";
        public const string FullscreenKey = "Fullscreen";
        public const string VSyncKey = "VSync";
        public const string GameSpeedKey = "GameSpeed";
        public const string DebugModeKey = "DebugMode";

        public SettingsSnapshot Load()
        {
            SettingsSnapshot defaults = SettingsSnapshot.Defaults;
            return new SettingsSnapshot(
                PlayerPrefs.GetFloat(MasterVolumeKey, defaults.MasterVolume),
                PlayerPrefs.GetFloat(MusicVolumeKey, defaults.MusicVolume),
                PlayerPrefs.GetFloat(SfxVolumeKey, defaults.SfxVolume),
                PlayerPrefs.GetInt(MuteKey, defaults.Mute ? 1 : 0) == 1,
                PlayerPrefs.GetInt(QualityKey, defaults.Quality),
                PlayerPrefs.GetInt(FullscreenKey, defaults.Fullscreen ? 1 : 0) == 1,
                PlayerPrefs.GetInt(VSyncKey, defaults.VSync ? 1 : 0) == 1,
                PlayerPrefs.GetFloat(GameSpeedKey, defaults.GameSpeed),
                PlayerPrefs.GetInt(DebugModeKey, defaults.DebugMode ? 1 : 0) == 1);
        }

        public void Save(SettingsSnapshot settings)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, settings.MasterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, settings.MusicVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, settings.SfxVolume);
            PlayerPrefs.SetInt(MuteKey, settings.Mute ? 1 : 0);
            PlayerPrefs.SetInt(QualityKey, settings.Quality);
            PlayerPrefs.SetInt(FullscreenKey, settings.Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(VSyncKey, settings.VSync ? 1 : 0);
            PlayerPrefs.SetFloat(GameSpeedKey, settings.GameSpeed);
            PlayerPrefs.SetInt(DebugModeKey, settings.DebugMode ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}

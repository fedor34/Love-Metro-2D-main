using UnityEngine;

namespace LoveMetro.UI
{
    internal class SettingsApplier
    {
        public virtual void Apply(SettingsSnapshot settings)
        {
            if (!Application.isPlaying)
                return;

            AudioListener.volume = settings.MasterVolume;
            QualitySettings.SetQualityLevel(settings.Quality);
            Screen.fullScreen = settings.Fullscreen;
            QualitySettings.vSyncCount = settings.VSync ? 1 : 0;
        }
    }
}

using System;

namespace LoveMetro.UI
{
    internal readonly struct SettingsSnapshot : IEquatable<SettingsSnapshot>
    {
        public SettingsSnapshot(
            float masterVolume,
            float musicVolume,
            float sfxVolume,
            bool mute,
            int quality,
            bool fullscreen,
            bool vSync,
            float gameSpeed,
            bool debugMode)
        {
            MasterVolume = masterVolume;
            MusicVolume = musicVolume;
            SfxVolume = sfxVolume;
            Mute = mute;
            Quality = quality;
            Fullscreen = fullscreen;
            VSync = vSync;
            GameSpeed = gameSpeed;
            DebugMode = debugMode;
        }

        public static SettingsSnapshot Defaults => new SettingsSnapshot(
            1f,
            0.8f,
            1f,
            false,
            2,
            true,
            true,
            1f,
            false);

        public float MasterVolume { get; }
        public float MusicVolume { get; }
        public float SfxVolume { get; }
        public bool Mute { get; }
        public int Quality { get; }
        public bool Fullscreen { get; }
        public bool VSync { get; }
        public float GameSpeed { get; }
        public bool DebugMode { get; }

        public bool Equals(SettingsSnapshot other)
        {
            return MasterVolume.Equals(other.MasterVolume) &&
                   MusicVolume.Equals(other.MusicVolume) &&
                   SfxVolume.Equals(other.SfxVolume) &&
                   Mute == other.Mute &&
                   Quality == other.Quality &&
                   Fullscreen == other.Fullscreen &&
                   VSync == other.VSync &&
                   GameSpeed.Equals(other.GameSpeed) &&
                   DebugMode == other.DebugMode;
        }

        public override bool Equals(object obj)
        {
            return obj is SettingsSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = MasterVolume.GetHashCode();
                hash = (hash * 397) ^ MusicVolume.GetHashCode();
                hash = (hash * 397) ^ SfxVolume.GetHashCode();
                hash = (hash * 397) ^ Mute.GetHashCode();
                hash = (hash * 397) ^ Quality;
                hash = (hash * 397) ^ Fullscreen.GetHashCode();
                hash = (hash * 397) ^ VSync.GetHashCode();
                hash = (hash * 397) ^ GameSpeed.GetHashCode();
                hash = (hash * 397) ^ DebugMode.GetHashCode();
                return hash;
            }
        }
    }
}

using UnityEngine;

namespace LoveMetro.Input
{
    public interface IManualPairingService
    {
        bool HandleClick(Vector2 screenPosition);
    }
}

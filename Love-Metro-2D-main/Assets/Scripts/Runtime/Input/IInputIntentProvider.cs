using System;

namespace LoveMetro.Input
{
    public interface IInputIntentProvider
    {
        PointerIntent CurrentIntent { get; }
        event Action<PointerIntent> IntentChanged;
    }
}

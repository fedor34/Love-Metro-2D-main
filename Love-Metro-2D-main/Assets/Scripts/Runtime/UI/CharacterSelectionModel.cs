using System;

namespace LoveMetro.UI
{
    internal sealed class CharacterSelectionModel
    {
        public CharacterSelectionModel(int characterCount)
        {
            Count = Math.Max(0, characterCount);
            CurrentIndex = Count > 0 ? 0 : -1;
        }

        public int Count { get; }
        public int CurrentIndex { get; private set; }
        public bool HasSelection => CurrentIndex >= 0 && CurrentIndex < Count;

        public bool TrySelect(int index, out int selectedIndex)
        {
            if (index < 0 || index >= Count)
            {
                selectedIndex = CurrentIndex;
                return false;
            }

            CurrentIndex = index;
            selectedIndex = CurrentIndex;
            return true;
        }

        public bool TrySelectPrevious(out int selectedIndex)
        {
            if (Count == 0)
            {
                selectedIndex = -1;
                return false;
            }

            CurrentIndex = CurrentIndex <= 0 ? Count - 1 : CurrentIndex - 1;
            selectedIndex = CurrentIndex;
            return true;
        }

        public bool TrySelectNext(out int selectedIndex)
        {
            if (Count == 0)
            {
                selectedIndex = -1;
                return false;
            }

            CurrentIndex = CurrentIndex >= Count - 1 ? 0 : CurrentIndex + 1;
            selectedIndex = CurrentIndex;
            return true;
        }
    }
}

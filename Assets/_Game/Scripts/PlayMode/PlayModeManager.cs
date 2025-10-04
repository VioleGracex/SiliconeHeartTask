using UnityEngine;
using System;

namespace Ouiki.SiliconeHeart.PlayGameMode
{
    public enum GamePlayMode
    {
        None,
        Place,
        Remove
    }

    public class PlayModeManager : MonoBehaviour
    {
        public GamePlayMode CurrentMode { get; private set; } = GamePlayMode.None;
        public event Action<GamePlayMode> OnPlayModeChanged;

        public void SetMode(GamePlayMode mode)
        {
            if (CurrentMode == mode)
                return;

            CurrentMode = mode;
            OnPlayModeChanged?.Invoke(CurrentMode);
        }

        // Optionally, methods for setting to specific modes for UI buttons etc.
        public void SetNoneMode() => SetMode(GamePlayMode.None);
        public void SetPlaceMode() => SetMode(GamePlayMode.Place);
        public void SetRemoveMode() => SetMode(GamePlayMode.Remove);
    }
}
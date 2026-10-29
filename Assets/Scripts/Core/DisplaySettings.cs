using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MoriEchoes
{
    public sealed class DisplaySettings : MonoBehaviour
    {
        private int windowWidth = 1280;
        private int windowHeight = 720;
        public void ToggleFullscreen()
        {
            if (Screen.fullScreen)
                Screen.SetResolution(windowWidth, windowHeight, FullScreenMode.Windowed);
            else
            {
                windowWidth = Screen.width; windowHeight = Screen.height;
                Resolution display = Screen.currentResolution;
                Screen.SetResolution(display.width, display.height, FullScreenMode.FullScreenWindow);
            }
        }
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.f11Key.wasPressedThisFrame ||
                keyboard.enterKey.wasPressedThisFrame && (keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed))) ToggleFullscreen();
#endif
        }
    }
}

using UnityEngine;

namespace MoriEchoes
{
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        public GameConfig config;
        public bool startInMenu = true;

        private void Start()
        {
#if !ENABLE_INPUT_SYSTEM
            Debug.LogError("Mori Echoes requires Input System. Install com.unity.inputsystem and set Active Input Handling to Input System Package (New), then restart the Editor.", this);
            return;
#else
            if (config == null)
            {
                Debug.LogError("Assign MoriEchoesConfig to GameBootstrap before playing. Use Tools > Mori Echoes > Prepare Prototype Scenes.", this);
                return;
            }
            string reason;
            if (!config.Validate(out reason))
            {
                Debug.LogError("Invalid Mori Echoes config: " + reason, config);
                return;
            }
            if (FindAnyObjectByType<GameManager>() != null)
            {
                Debug.LogError("Only one Mori Echoes bootstrap may run in a scene.", this);
                return;
            }
            var manager = gameObject.AddComponent<GameManager>();
            manager.Initialize(config, startInMenu);
#endif
        }
    }
}

using UnityEngine;

namespace MoriEchoes
{
    public sealed class VerticalShaftParallax : MonoBehaviour
    {
        public Camera Camera;
        [Range(0, 1)] public float Factor = 0.12f;

        private void LateUpdate()
        {
            if (Camera == null) return;
            Vector3 cameraPosition = Camera.transform.position;
            transform.position = new Vector3(cameraPosition.x * (1 - Factor), cameraPosition.y * (1 - Factor), 0);
        }
    }
}

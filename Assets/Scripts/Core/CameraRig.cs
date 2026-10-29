using UnityEngine;

namespace MoriEchoes
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        public Camera Camera { get; private set; }
        public Vector2 ViewForward { get; private set; } = Vector2.up;
        public Vector2 ViewRight => new Vector2(ViewForward.y, -ViewForward.x);
        public float AdsAmount { get; private set; }
        public bool IsAds { get; private set; }
        public float Recoil => recoil;
        public SideCameraDirector Side { get; private set; }
        private GameManager game;
        private Vector2 focus;
        private float heading;
        private float shake;
        private float recoil;
        private bool snap;

        public void Initialize(GameManager owner)
        {
            game = owner;
            Camera = GetComponent<Camera>();
            Camera.orthographic = true;
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 200f;
            Camera.transparencySortMode = TransparencySortMode.CustomAxis;
            Camera.backgroundColor = ProceduralArt.Ink;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            if (game.Config.sideScroller) Side = new SideCameraDirector(game, Camera);
            ResetFollow();
        }

        public void ResetFollow()
        {
            if (Side != null) { Side.Reset(); shake = recoil = AdsAmount = 0; IsAds = false; return; }
            heading = 0; ViewForward = Vector2.up;
            shake = recoil = AdsAmount = 0; IsAds = false;
            snap = true;
            Camera.orthographicSize = game.Config.camBaseSize;
            UpdatePose(0);
        }

        public void SetAds(bool active) { IsAds = active && game.IsPlaying; }

        public void ClearTransient()
        {
            IsAds = false;
            shake = recoil = 0;
            Side?.Cancel();
        }

        public void Shake(float intensity)
        {
            if (game.IsPlaying) shake = Mathf.Min(0.5f, shake + intensity * game.Config.screenShake);
        }

        public void AddRecoil(float intensity)
        {
            if (game.IsPlaying) recoil = Mathf.Min(game.Config.camRecoilMax, recoil + Mathf.Max(0, intensity));
        }

        public bool GroundPoint(Vector2 screenPoint, out Vector2 point)
        {
            Ray ray = Camera.ScreenPointToRay(screenPoint);
            var ground = new Plane(Vector3.forward, Vector3.zero);
            if (ground.Raycast(ray, out float distance))
            {
                point = ray.GetPoint(distance);
                return true;
            }
            point = Vector2.zero;
            return false;
        }

        public Vector2 AimDirection(Vector2 origin)
        {
            if (GroundPoint(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), out Vector2 target))
            {
                Vector2 direction = target - origin;
                if (direction.sqrMagnitude > 0.01f) return direction.normalized;
            }
            return ViewForward;
        }

        public int SortOrder(Vector3 position) => Side != null ? 100 : 100 - Mathf.RoundToInt(Vector2.Dot(position, ViewForward) * 10);

        private void Update()
        {
            if (Side != null)
            {
                recoil = Mathf.MoveTowards(recoil, 0, game.Config.camRecoilDecay * Time.unscaledDeltaTime);
                shake = Mathf.MoveTowards(shake, 0, Time.unscaledDeltaTime * 0.7f);
                Side.Tick(recoil, shake); IsAds = Side.IsAds; AdsAmount = Side.AdsAmount;
                return;
            }
            if (game == null || !game.IsPlaying) return;
            SetAds(game.ControlsReady && game.Input.AimDownSights);
            if (!game.ControlsReady) return;
            if (game.Input.UsingGamepad)
            {
                Vector2 stick = game.Input.AimStick;
                if (stick.sqrMagnitude > 0.04f) heading = Mathf.Atan2(-stick.x, stick.y) * Mathf.Rad2Deg;
            }
            else heading -= game.Input.LookDelta.x * game.Config.mouseLookSensitivity * (IsAds ? 0.55f : 1f);
            heading = Mathf.Repeat(heading + 180f, 360f) - 180f;
            ViewForward = Quaternion.Euler(0, 0, heading) * Vector2.up;
            UpdatePose(Time.unscaledDeltaTime);
        }

        private void LateUpdate()
        {
            if (Side != null) return;
            if (game == null) return;
            if (!game.IsPlaying) UpdatePose(Time.unscaledDeltaTime);
        }

        private void UpdatePose(float delta)
        {
            GameConfig config = game.Config;
            AdsAmount = Mathf.Lerp(AdsAmount, IsAds ? 1 : 0, 1f - Mathf.Exp(-config.camZoomSpeed * delta));
            Camera.orthographicSize = Mathf.Lerp(config.camBaseSize, config.camAdsSize, AdsAmount);
            recoil = Mathf.MoveTowards(recoil, 0, config.camRecoilDecay * delta);
            shake = Mathf.MoveTowards(shake, 0, 0.7f * delta);
            float pitch = Mathf.Min(57f, config.camPitchDeg + recoil * config.camRecoilPitch);
            Quaternion rotation = Quaternion.Euler(0, 0, heading) * Quaternion.Euler(-pitch, 0, 0);
            Vector2 playerPosition = game.Player != null ? (Vector2)game.Player.transform.position : Vector2.zero;
            float lead = Mathf.Min(config.camLead - config.camBack, Camera.orthographicSize * 0.62f);
            Vector2 desired = playerPosition + ViewForward * lead + ViewRight * config.camShoulderOffset;
            float extentRight = Camera.orthographicSize * Camera.aspect;
            float extentForward = Camera.orthographicSize / Mathf.Cos(pitch * Mathf.Deg2Rad);
            Vector2 extent = new Vector2(Mathf.Abs(ViewRight.x) * extentRight + Mathf.Abs(ViewForward.x) * extentForward,
                Mathf.Abs(ViewRight.y) * extentRight + Mathf.Abs(ViewForward.y) * extentForward);
            Vector2 limit = Vector2.Max(Vector2.zero, config.arenaHalfSize - extent);
            Vector2 bounded = new Vector2(Mathf.Clamp(desired.x, -limit.x, limit.x), Mathf.Clamp(desired.y, -limit.y, limit.y));
            Vector2 relative = bounded - playerPosition;
            float forward = Mathf.Clamp(Vector2.Dot(relative, ViewForward), lead * 0.7f, lead);
            float sideways = Mathf.Clamp(Vector2.Dot(relative, ViewRight), -Camera.orthographicSize * 0.35f, Camera.orthographicSize * 0.35f);
            desired = playerPosition + ViewForward * forward + ViewRight * sideways;
            focus = snap ? desired : Vector2.Lerp(focus, desired, 1f - Mathf.Exp(-config.camFollow * delta));
            snap = false;
            Vector2 fromPlayer = focus - playerPosition;
            float minimumForward = lead * 0.5f;
            if (Vector2.Dot(fromPlayer, ViewForward) < minimumForward)
                focus += ViewForward * (minimumForward - Vector2.Dot(fromPlayer, ViewForward));
            float maximumSide = Camera.orthographicSize * Camera.aspect * 0.65f;
            float side = Vector2.Dot(focus - playerPosition, ViewRight);
            focus += ViewRight * (Mathf.Clamp(side, -maximumSide, maximumSide) - side);
            Vector2 jitter = game.IsPlaying ? Random.insideUnitCircle * shake : Vector2.zero;
            Vector2 groundFocus = focus + ViewForward * recoil + jitter;
            transform.SetPositionAndRotation((Vector3)groundFocus - rotation * Vector3.forward * (24f + config.camHeight), rotation);
            Camera.transparencySortAxis = ViewForward;
        }
    }
}

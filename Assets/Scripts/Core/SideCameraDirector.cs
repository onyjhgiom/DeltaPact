using UnityEngine;

namespace MoriEchoes
{
    public sealed class SideCameraDirector
    {
        public ViewMode Mode { get; private set; }
        public bool IsAds { get; private set; }
        public float AdsAmount { get; private set; }
        public string ModeLabel => Mode == ViewMode.Tactical ? "战术远景" : Mode == ViewMode.Shoulder ? "第三人称近景" : "第一人称瞄准表现 · 2D";
        private readonly GameManager game;
        private readonly Camera camera;
        private Vector2 focus;
        private Vector2 focusVelocity;
        private float followDirection = 1;
        private float pendingDirection;
        private float reversalTime;
        private float reversalOrigin;
        private float lookAhead;
        private float lookAheadVelocity;
        private bool snap;
        public SideCameraDirector(GameManager owner, Camera source) { game = owner; camera = source; Reset(); }
        public void Reset()
        {
            Mode = game.Config.defaultView; IsAds = false; AdsAmount = 0; snap = true;
            focusVelocity = Vector2.zero; lookAheadVelocity = 0;
            followDirection = game.Player != null && game.Player.Aim.x < -0.1f ? -1 : 1;
            pendingDirection = followDirection; reversalTime = 0;
            lookAhead = followDirection * (Mode == ViewMode.Tactical ? 2.2f : 0.8f);
            focus = game.Player != null ? (Vector2)game.Player.transform.position + new Vector2(2.2f, 2) : new Vector2(8, 3.5f);
            camera.orthographicSize = game.Config.viewTacticalSize;
            camera.transform.SetPositionAndRotation(new Vector3(focus.x, focus.y, -24), Quaternion.identity);
        }
        public void Cancel() { IsAds = false; }
        public void Tick(float recoil, float shake)
        {
            float delta = Mathf.Clamp(Time.unscaledDeltaTime, 0.0001f, 0.05f);
            bool playing = game.IsPlaying && game.Player != null;
            if (playing && game.ControlsReady && game.Input.ChangeView) Mode = (ViewMode)(((int)Mode + 1) % 3);
            IsAds = playing && (Mode == ViewMode.IronSight || game.ControlsReady && game.Input.AimDownSights);
            AdsAmount = Mathf.Lerp(AdsAmount, IsAds ? 1 : 0, 1 - Mathf.Exp(-game.Config.camZoomSpeed * Time.unscaledDeltaTime));
            float normal = Mode == ViewMode.Tactical ? game.Config.viewTacticalSize : game.Config.viewShoulderSize;
            float size = Mathf.Lerp(normal, game.Config.viewIronSightSize, AdsAmount);
            if (playing && Mode == ViewMode.Tactical && !IsAds && game.Level != null
                && game.Level.Boss != null && !game.Level.Boss.Health.IsDead)
            {
                Vector2 separation = game.Level.Boss.transform.position - game.Player.transform.position;
                // Long arenas need room for both silhouettes and readable attack boundaries.
                float encounterSize = Mathf.Max((Mathf.Abs(separation.x) * 0.5f + 4f) / Mathf.Max(0.5f, camera.aspect),
                    Mathf.Abs(separation.y) * 0.5f + 3.5f);
                if (game.Level.IsVerticalStage) encounterSize = Mathf.Max(encounterSize, (Mathf.Abs(separation.y) + 4.4f) / 1.3f + 0.5f);
                size = Mathf.Max(size, encounterSize);
            }
            camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, size, 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
            if (playing && game.Level != null && game.Level.SecretRoutes != null && game.Level.SecretRoutes.Inside)
            {
                Rect room = game.Level.SecretRoutes.CameraBounds;
                camera.orthographicSize = Mathf.Min(size, room.height * 0.5f);
                float extent = camera.orthographicSize * camera.aspect;
                Vector2 player = game.Player.transform.position;
                Vector2 desiredRoom = new Vector2(room.width > extent * 2
                    ? Mathf.Clamp(player.x, room.xMin + extent, room.xMax - extent) : room.center.x, room.center.y);
                focus = snap ? desiredRoom : Vector2.SmoothDamp(focus, desiredRoom, ref focusVelocity, 0.18f, Mathf.Infinity, delta);
                snap = false;
                Vector2 roomJitter = Random.insideUnitCircle * shake;
                camera.transform.SetPositionAndRotation(new Vector3(focus.x + roomJitter.x, focus.y + recoil + roomJitter.y, -24), Quaternion.identity);
                return;
            }
            if (playing && game.Level != null && game.Level.IsVerticalStage)
            {
                TickVertical(recoil, shake, delta);
                return;
            }
            Vector2 desired = new Vector2(8, 3.5f);
            if (game.Player != null)
            {
                Vector2 player = game.Player.transform.position;
                float direction = playing && game.ControlsReady && Mathf.Abs(game.Input.Move.x) > 0.15f ? Mathf.Sign(game.Input.Move.x)
                    : playing && game.ControlsReady && IsAds && Mathf.Abs(game.Player.Aim.x) > 0.35f ? Mathf.Sign(game.Player.Aim.x) : followDirection;
                if (direction != followDirection)
                {
                    if (pendingDirection != direction) { pendingDirection = direction; reversalOrigin = player.x; reversalTime = 0; }
                    reversalTime += delta;
                    if (Mathf.Abs(player.x - reversalOrigin) >= 0.65f || reversalTime >= (IsAds ? 0.18f : 0.28f))
                    { followDirection = direction; reversalTime = 0; }
                }
                else { pendingDirection = followDirection; reversalTime = 0; }
                float lead = Mode == ViewMode.Tactical ? 2.2f : 0.8f;
                lookAhead = Mathf.SmoothDamp(lookAhead, followDirection * lead, ref lookAheadVelocity, 0.28f, Mathf.Infinity, delta);
                desired = player + new Vector2(lookAhead, Mode == ViewMode.Tactical ? 1.6f : 0.55f);
                if (Mode == ViewMode.Tactical && game.Level != null && game.Level.Boss != null && !game.Level.Boss.Health.IsDead)
                {
                    Vector2 boss = game.Level.Boss.transform.position;
                    desired.x = Mathf.Lerp(player.x, boss.x, 0.5f);
                    desired.y = Mathf.Lerp(player.y, boss.y, 0.3f) + 1.4f;
                }
                if (!snap)
                {
                    float horizontalZone = IsAds ? 0.18f : Mode == ViewMode.Tactical ? 0.65f : 0.3f;
                    desired.x = focus.x + Mathf.Sign(desired.x - focus.x) * Mathf.Max(0, Mathf.Abs(desired.x - focus.x) - horizontalZone);
                    desired.y = focus.y + Mathf.Sign(desired.y - focus.y) * Mathf.Max(0, Mathf.Abs(desired.y - focus.y) - 0.7f);
                }
                if (game.Level != null)
                {
                    float extent = camera.orthographicSize * camera.aspect;
                    desired.x = Mathf.Clamp(desired.x, extent, Mathf.Max(extent, game.Level.Length - extent));
                }
                if (game.Level != null && game.Level.IsUnderwaterStage && game.Level.Swim != null)
                {
                    // 水下：相机在垂直水柱内跟随玩家浮沉，不再钉死在地面高度
                    SwimStageDirector swim = game.Level.Swim;
                    float minCam = swim.MinY + camera.orthographicSize - 1f;
                    float maxCam = swim.MaxY - camera.orthographicSize + 1f;
                    desired.y = maxCam > minCam ? Mathf.Clamp(desired.y, minCam, maxCam) : (minCam + maxCam) * 0.5f;
                }
                else if (game.Level != null && game.Level.Swim != null)
                {
                    float seaY = game.Level.Swim.SurfaceY;
                    desired.y = Mathf.Clamp(player.y + 0.8f, seaY - 2.2f, seaY + 2.6f);
                }
                else desired.y = Mathf.Max(1.9f, desired.y);
            }
            focus = snap ? desired : Vector2.SmoothDamp(focus, desired, ref focusVelocity, Mathf.Max(0.08f, 1.6f / game.Config.camFollow), Mathf.Infinity, delta);
            snap = false;
            Vector2 jitter = playing ? Random.insideUnitCircle * shake : Vector2.zero;
            camera.transform.SetPositionAndRotation(new Vector3(focus.x + jitter.x, focus.y + recoil + jitter.y, -24), Quaternion.identity);
            camera.transparencySortAxis = Vector3.forward;
        }

        private void TickVertical(float recoil, float shake, float delta)
        {
            Vector2 player = game.Player.transform.position;
            float extentX = camera.orthographicSize * camera.aspect;
            float xLimit = Mathf.Max(0, 8.1f - extentX);
            float aimLead = Mathf.Clamp(game.Player.Aim.x, -1, 1) * (Mode == ViewMode.Tactical ? 0.65f : 0.35f);
            Vector2 desired = new Vector2(Mathf.Clamp(player.x + aimLead, -xLimit, xLimit),
                player.y + (Mode == ViewMode.Tactical ? 2.2f : Mode == ViewMode.Shoulder ? 1.7f : 1.2f));
            if (Mode == ViewMode.Tactical && !IsAds && game.Level.Boss != null && !game.Level.Boss.Health.IsDead)
            {
                float bossY = game.Level.Boss.transform.position.y;
                float bottom = Mathf.Min(player.y - 0.9f, bossY - 1.6f);
                float top = Mathf.Max(player.y + 1, bossY + 2.8f);
                desired.y = (bottom + top) * 0.5f + camera.orthographicSize * 0.12f;
            }
            float lower, upper;
            if (game.Level.Vertical.Ascending)
            {
                lower = game.Level.Vertical.StartPoint.y + camera.orthographicSize - 1.4f;   // 井底
                upper = game.Level.Vertical.DepthLength - 3.2f + camera.orthographicSize;    // 天顶
            }
            else
            {
                lower = -game.Level.Vertical.DepthLength + 3.2f + camera.orthographicSize;
                upper = game.Level.Vertical.StartPoint.y + camera.orthographicSize - 1.4f;
            }
            desired.y = Mathf.Clamp(desired.y, lower, upper);
            focus = snap ? desired : Vector2.SmoothDamp(focus, desired, ref focusVelocity,
                Mode == ViewMode.IronSight ? 0.12f : 0.2f, Mathf.Infinity, delta);
            snap = false;
            Vector2 jitter = Random.insideUnitCircle * shake;
            camera.transform.SetPositionAndRotation(new Vector3(focus.x + jitter.x, focus.y + recoil + jitter.y, -24), Quaternion.identity);
            camera.transparencySortAxis = Vector3.forward;
        }
    }
}

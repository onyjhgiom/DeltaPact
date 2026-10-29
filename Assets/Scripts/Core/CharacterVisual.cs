using UnityEngine;

namespace MoriEchoes
{
    public sealed class CharacterVisual : MonoBehaviour
    {
        private GameManager game;
        private SpriteRenderer sprite;
        private SpriteRenderer halo;
        private SpriteRenderer healthFill;
        private SpriteRenderer healthBack;
        private Health health;
        private Rigidbody2D body;
        private PlayerController player;
        private Vector3 baseScale;
        private Color tint;
        private float flash;
        private float phase;
        private float healthWidth;
        private float healthHeight;
        private Sprite frontSprite;

        public void Initialize(GameManager owner, Health hitPoints, EnemyKind kind = EnemyKind.Chaser)
        {
            if (owner.Config.sideScroller)
            {
                gameObject.AddComponent<SideCharacterVisual>().Initialize(owner, hitPoints, kind);
                enabled = false;
                return;
            }
            game = owner; health = hitPoints;
            body = GetComponent<Rigidbody2D>();
            player = GetComponent<PlayerController>();
            bool friendly = hitPoints.Team == Team.Player;
            bool boss = !friendly && kind == EnemyKind.Boss;
            float size = boss ? 2.8f : kind == EnemyKind.Elite && !friendly ? 1.55f : 1f;
            game.Art.Renderer("Shadow", transform, game.Art.Disc, new Color(0, 0, 0, 0.35f), new Vector2(size, size * 0.4f), -700)
                .transform.localPosition = Vector3.zero;
            halo = game.Art.Renderer("TeamHalo", transform, game.Art.Glow,
                new Color(friendly ? 0.1f : 1f, friendly ? 0.7f : 0.15f, friendly ? 1f : 0.08f, 0.18f),
                new Vector2(size * 1.8f, size * 1.3f), -1);
            Sprite custom = friendly ? game.Config.playerSprite : game.Config.enemySprites != null && game.Config.enemySprites.Length > (int)kind
                ? game.Config.enemySprites[(int)kind] : null;
            sprite = game.Art.Renderer("Body", transform, custom != null ? custom : friendly ? game.Art.Hero : game.Art.Enemies[(int)kind],
                Color.white, custom != null ? Vector2.one * size : new Vector2(size * 1.1f, size * 1.4f), 100);
            baseScale = sprite.transform.localScale;
            frontSprite = sprite.sprite;
            tint = Color.white;
            phase = Random.value * 6;
            if (!friendly && !boss)
            {
                healthWidth = size * 0.7f;
                healthHeight = size * 0.85f;
                healthBack = game.Art.Renderer("HealthBack", transform, game.Art.Square, new Color(0, 0, 0, 0.7f), new Vector2(size * 0.7f, 0.055f), 500);
                healthFill = game.Art.Renderer("HealthFill", transform, game.Art.Square, ProceduralArt.Danger, new Vector2(size * 0.7f, 0.035f), 501);
                healthFill.transform.localPosition = new Vector3(0, size * 0.85f, 0);
            }
            health.Damaged += OnDamaged;
        }

        private void OnDamaged(float damage) { flash = 0.09f; }

        private void LateUpdate()
        {
            if (game == null || health == null) return;
            float speed = body != null ? body.linearVelocity.magnitude : 0;
            phase += Time.deltaTime * (speed > 0.1f ? 14 : 2);
            Quaternion rotation = game.CameraRig.transform.rotation;
            Vector3 up = game.CameraRig.transform.up;
            sprite.transform.rotation = rotation;
            halo.transform.rotation = rotation;
            sprite.transform.position = transform.position + up * (0.2f + Mathf.Sin(phase) * (speed > 0.1f ? 0.035f : 0.008f));
            sprite.transform.localScale = baseScale * (1 + Mathf.Abs(Mathf.Sin(phase)) * 0.015f);
            sprite.sortingOrder = game.CameraRig.SortOrder(transform.position);
            halo.sortingOrder = sprite.sortingOrder - 2;
            if (player != null)
            {
                sprite.sprite = Vector2.Dot(player.Aim, game.CameraRig.ViewForward) > 0.1f
                    ? game.Config.playerBackSprite != null ? game.Config.playerBackSprite
                    : game.Config.playerSprite == null ? game.Art.HeroBack : frontSprite : frontSprite;
                sprite.flipX = Vector2.Dot(player.Aim, game.CameraRig.ViewRight) < -0.1f;
            }
            else if (body != null && body.linearVelocity.sqrMagnitude > 0.01f)
                sprite.flipX = Vector2.Dot(body.linearVelocity, game.CameraRig.ViewRight) < 0;
            flash = Mathf.Max(0, flash - Time.deltaTime);
            sprite.color = flash > 0 ? new Color(1f, 0.48f, 0.4f) : tint;
            if (healthFill != null)
            {
                healthBack.transform.rotation = healthFill.transform.rotation = rotation;
                healthBack.transform.position = healthFill.transform.position = transform.position + up * healthHeight;
                Vector3 scale = healthFill.transform.localScale;
                scale.x = healthWidth * health.Current / health.Maximum;
                healthFill.transform.localScale = scale;
            }
        }
    }
}

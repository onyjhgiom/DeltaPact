using System.Collections.Generic;
using UnityEngine;

namespace MoriEchoes
{
    public sealed class BossTechniqueController : MonoBehaviour
    {
        private readonly List<BossField> fields = new List<BossField>();
        private GameManager game;
        private EnemyAI enemy;
        private BossController boss;
        private Rigidbody2D body;
        private float finishAt;
        private int nextNativeCount = 2;
        private int cursor;
        private int previousPhase;
        private int currentSlot;
        private BossTechniqueProfile profile;
        private Vector2 liveTarget;
        public BossField TargetStrike { get; private set; }
        public bool TargetCommitted { get; private set; }
        public bool Active { get; private set; }
        public int CastCount { get; private set; }
        public int LastSlot => currentSlot;
        public string SkillName => profile != null ? profile.name : string.Empty;
        public IReadOnlyList<BossField> Fields => fields;
        private int Phase => boss.Side != null ? boss.Side.Phase : boss.Vertical != null ? boss.Vertical.Phase : boss.Swim != null ? boss.Swim.Phase : 1;
        private int NativeCount => boss.Side != null ? boss.Side.AttacksResolved : boss.Vertical != null ? boss.Vertical.AttacksResolved : boss.Swim != null ? boss.Swim.AttacksResolved : 0;

        public void Initialize(GameManager owner, EnemyAI actor, BossController controller, Rigidbody2D rigidbody)
        {
            game = owner; enemy = actor; boss = controller; body = rigidbody;
        }

        public bool TryBegin()
        {
            BossTechniqueProfile[] available = game.Level.Theme.bossTechniques;
            if (Active || NativeCount < nextNativeCount || available == null || available.Length != 3
                || enemy.Health.IsDead || !game.IsPlaying) return false;
            int phase = Mathf.Clamp(Phase, 1, 3);
            currentSlot = previousPhase != phase ? phase - 1 : cursor % phase;
            previousPhase = phase; cursor = currentSlot + 1;
            profile = available[currentSlot];
            if (profile == null || profile.frames == null || profile.frames.Length != 2) return false;
            nextNativeCount = NativeCount + 2;
            Active = true; CastCount++; fields.Clear();
            TargetCommitted = false;
            body.linearVelocity = Vector2.zero;
            Vector2 target = game.Player.transform.position;
            liveTarget = target;
            game.Hud.Announce(game.Level.Theme.boss + " // " + profile.name, 1.8f);
            game.Effects.BossTechnique(profile.frames[0], body.position, 3.2f, 0.48f);
            Compose(game.SelectedStage * 3 + currentSlot, target);
            float firstWarning = float.PositiveInfinity;
            foreach (BossField field in fields) firstWarning = Mathf.Min(firstWarning, field.WarningRemaining);
            TargetStrike = AimedStrike(target, firstWarning);
            float trackingDuration = Mathf.Max(0, firstWarning - 0.8f);
            foreach (BossField field in fields) field.ConfigureTargetLock(field.WarningRemaining - trackingDuration);
            return true;
        }

        public bool Step()
        {
            if (!Active) return false;
            if (enemy == null || enemy.Health.IsDead || game.Level == null || !gameObject.activeInHierarchy)
            {
                Cancel(); return false;
            }
            if (!game.IsPlaying || Time.timeScale <= 0) { body.linearVelocity = Vector2.zero; return true; }
            if (Phase != previousPhase) { Cancel(); return false; }
            body.linearVelocity = Vector2.zero;
            if (!TargetCommitted && TargetStrike != null)
            {
                TargetCommitted = TargetStrike.TargetCommitted;
                if (!TargetCommitted)
                {
                    Vector2 target = game.Player.transform.position;
                    Vector2 shift = target - liveTarget;
                    foreach (BossField field in fields)
                        if (field != null) field.FollowWarningAt((Vector2)field.transform.position + shift);
                    liveTarget = target;
                }
            }
            if (Time.time < finishAt) return true;
            Active = false;
            return false;
        }

        private void Compose(int pattern, Vector2 target)
        {
            switch (pattern)
            {
                case 0:
                    for (int index = -1; index <= 1; index++) Strike(target + Vector2.right * index * 2.7f, new Vector2(0.9f, 2.5f), 1.15f + (index + 1) * 0.2f); break;
                case 1: Sweep(target + new Vector2(-4, 2), target + new Vector2(0, 3.6f), target + new Vector2(4, 0), 1.6f, 1.2f); break;
                case 2:
                    Sweep(target + new Vector2(5, -0.6f), target + new Vector2(-4, -0.6f), target + new Vector2(5, -0.6f), 1.3f, 1.1f);
                    Sweep(target + new Vector2(-5, 2.1f), target + new Vector2(4, 2.1f), target + new Vector2(-5, 2.1f), 1, 1.65f); break;
                case 3:
                    Strike(target + new Vector2(-2.5f, 0), new Vector2(1.1f, 5.5f), 1.2f);
                    Strike(target + new Vector2(2.5f, 0), new Vector2(1.1f, 5.5f), 1.75f); break;
                case 4:
                    Strike(target, new Vector2(7, 0.6f), 1.4f, 28);
                    Strike(target, new Vector2(7, 0.6f), 1.9f, -28); break;
                case 5:
                    for (int index = -1; index <= 1; index++) Sweep(target + new Vector2(-4, index*1.5f), target + new Vector2(index, 3.5f), target + new Vector2(4, -0.5f), 1.1f, 1.2f+(index+1)*0.3f); break;
                case 6:
                    Sweep(target + new Vector2(4, 3), target + new Vector2(-1, 1.5f), target + new Vector2(4, -1), 0.85f, 1.3f); break;
                case 7:
                    for (int index = 0; index < 4; index++) Sweep(target + new Vector2((index-1.5f)*2.1f, 5), target + new Vector2((index-1.5f)*2.1f, 2), target + new Vector2((index-1.5f)*2.1f, -1.5f), 1.1f, 1.1f+index*0.27f); break;
                case 8: Ring(target, 5.4f, 1.6f, 1.4f); break;
                case 9:
                    for (int index = -1; index <= 1; index++) Strike(target + Vector2.right*index*2.8f, new Vector2(0.65f,4.5f), 1.1f+(index+1)*0.45f); break;
                case 10: Strike(target + Vector2.up*0.5f, new Vector2(8,0.65f), 1.65f); break;
                case 11: Sweep(target + new Vector2(-4,-1), target + new Vector2(-4,2), target + new Vector2(4,2), 0.9f, 1.45f); break;
                case 12:
                    Sweep(target + new Vector2(-5,1), target + new Vector2(-2,3), target, 1.2f,1.4f);
                    Sweep(target + new Vector2(5,1), target + new Vector2(2,3), target, 1.2f,1.4f); break;
                case 13: Strike(new Vector2(target.x,target.y-0.25f),new Vector2(10,0.75f),1.65f); break;
                case 14:
                    for (int index=0;index<3;index++) Sweep(target+new Vector2(index%2==0?-5:5,index%2==0?-0.6f:2.3f),target+new Vector2(0,index%2==0?-0.6f:2.3f),target+new Vector2(index%2==0?5:-5,index%2==0?-0.6f:2.3f),1.1f,1.1f+index*0.5f); break;
                case 15:
                    for (int index=-1;index<=1;index++) Sweep(target+new Vector2(index*2.5f-1,5),target+new Vector2(index*2.5f,1),target+new Vector2(index*2.5f+1,-2),0.9f,1.2f+(index+1)*0.25f); break;
                case 16: Sweep(target+new Vector2(-4,2),target+new Vector2(0,2),target+new Vector2(0,-2),1.5f,1.4f); break;
                case 17:
                    Sweep(target+new Vector2(-5,-0.8f),target+new Vector2(-3,-0.8f),target+new Vector2(-1.7f,-0.8f),2,1.5f);
                    Sweep(target+new Vector2(5,-0.8f),target+new Vector2(3,-0.8f),target+new Vector2(1.7f,-0.8f),2,1.5f); break;
                case 18: Sweep(target+new Vector2(-4,1),target+new Vector2(0,3),target+new Vector2(4,1),1.4f,1.3f); break;
                case 19:
                    Sweep(target+new Vector2(-5,-1.8f),target+new Vector2(0,-1.8f),target+new Vector2(5,-1.8f),1.2f,1.2f);
                    Sweep(target+new Vector2(5,1.8f),target+new Vector2(0,1.8f),target+new Vector2(-5,1.8f),1.2f,1.55f); break;
                case 20:
                    Sweep(target+new Vector2(0,5),target+new Vector2(0,1),target+new Vector2(0,-1),1.5f,1.2f);
                    Ring(target+Vector2.down,5,1.45f,2.35f); break;
                case 21:
                    Sweep(target+new Vector2(-4,-2),target+new Vector2(0,2),target+new Vector2(4,-2),1.2f,1.25f);
                    Sweep(target+new Vector2(4,2),target+new Vector2(0,-2),target+new Vector2(-4,2),1.2f,1.6f); break;
                case 22:
                    for(int index=-1;index<=1;index++) Strike(target+new Vector2(index*2.4f,index*0.6f),new Vector2(0.65f,4),1.1f+(index+1)*0.4f,-32); break;
                case 23:
                    foreach(Vector2 direction in new[]{Vector2.up,Vector2.down,Vector2.left,Vector2.right}) Sweep(target+direction*4.5f,target+direction*3,target+direction*1.7f,1.35f,1.5f); break;
                case 24:
                    Sweep(target+new Vector2(-5,-1.6f),target+new Vector2(-1,2.7f),target+new Vector2(4,0.2f),1.1f,1.2f);
                    Sweep(target+new Vector2(5,-1.8f),target+new Vector2(1,3.2f),target+new Vector2(-4,0.8f),1,1.6f); break;
                case 25:
                    Sweep(target+new Vector2(5,-0.4f),target,target+new Vector2(-5,-0.4f),1.1f,1.2f);
                    Strike(target+Vector2.right*1.6f,new Vector2(1.1f,4),2.1f); break;
                case 26:
                    for(int index=0;index<3;index++) Sweep(target+new Vector2(5,index-1),target+new Vector2(0,index-1),target+new Vector2(-5,index-1),1.2f+index*0.35f,1.25f+index*0.5f); break;
                case 27:
                    for(int index=-1;index<=1;index++) Strike(target+Vector2.right*index*2.8f,new Vector2(1.05f,6),1.2f+(index+1)*0.45f); break;
                case 28:
                    Sweep(target+new Vector2(4,0),target+new Vector2(0,1.3f),target+new Vector2(-4,3),1.1f,1.4f);
                    Sweep(target+new Vector2(4,0),target+new Vector2(0,-1.3f),target+new Vector2(-4,-3),1.1f,1.4f); break;
                case 29:
                    for(int index=0;index<4;index++)
                    {
                        float angle=155+index*17;
                        Vector2 direction=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
                        Sweep(target+direction*5,target+direction*3,target+direction*1.9f,1.2f,1.4f+index*0.12f);
                    }
                    break;
            }
        }

        private BossField Strike(Vector2 point, Vector2 size, float warning, float angle = 0)
        {
            var entity = new GameObject("BossTechnique_" + game.SelectedStage + "_" + currentSlot);
            entity.transform.SetParent(game.Level.transform, false); entity.transform.position = point;
            BossField field = entity.AddComponent<BossField>();
            field.InitializeAttack(game, enemy.Health, "Technique_" + game.SelectedStage + "_" + currentSlot,
                BossFieldShape.Box, BossFieldStyle.Punch, size, warning, 0.55f, 17 + currentSlot * 3, angle);
            field.ConfigureArtwork(profile.frames[1]); fields.Add(field);
            field.ConfigureTargetLock(warning);
            finishAt = Mathf.Max(finishAt, Time.time + warning + 0.55f + 0.8f);
            return field;
        }

        private BossField AimedStrike(Vector2 target, float warning)
        {
            var entity = new GameObject("TechniqueTargetStrike_" + game.SelectedStage + "_" + currentSlot);
            entity.transform.SetParent(game.Level.transform, false); entity.transform.position = target;
            BossField field = entity.AddComponent<BossField>();
            field.InitializeAttack(game, enemy.Health, "TechniqueTarget_" + game.SelectedStage + "_" + currentSlot,
                BossFieldShape.Disc, BossFieldStyle.Punch, Vector2.one * 2.5f, warning, 0.42f, 17 + currentSlot * 3);
            field.ConfigureArtwork(profile.frames[1]); fields.Add(field);
            return field;
        }

        private void Sweep(Vector2 start, Vector2 middle, Vector2 end, float width, float warning)
        {
            BossField field = Strike(start, Vector2.one * width, warning);
            field.ExtendActiveDuration(1.4f);
            field.ConfigurePath(new[] {start,middle,end}, 1.4f);
            finishAt = Mathf.Max(finishAt, Time.time + warning + 2.2f);
        }

        private void Ring(Vector2 point, float diameter, float safeRadius, float warning)
        {
            var entity = new GameObject("BossTechniqueRing_" + game.SelectedStage);
            entity.transform.SetParent(game.Level.transform, false); entity.transform.position = point;
            BossField field = entity.AddComponent<BossField>();
            field.InitializeAttack(game,enemy.Health,"Technique_"+game.SelectedStage+"_"+currentSlot,
                BossFieldShape.Ring,BossFieldStyle.Punch,Vector2.one*diameter,warning,0.65f,22,0,safeRadius);
            field.ConfigureArtwork(profile.frames[1]); fields.Add(field);
            field.ConfigureTargetLock(warning);
            finishAt = Mathf.Max(finishAt,Time.time+warning+1.45f);
        }

        private void Cancel()
        {
            foreach (BossField field in fields) if (field != null) field.Cancel();
            fields.Clear(); Active = false;
            TargetStrike = null; TargetCommitted = false;
        }

        private void OnDisable() => Cancel();
    }
}

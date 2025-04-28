using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;
using RougeChess.Runtime;
using VContainer;
using VContainer.Unity;

namespace LogueChess.Runtime
{
    // =====================================================
    // Domain Models
    // =====================================================
    public class Stats
    {
        public int ATK;
        public float gaugeSpeed;
        public int CurrentHP;
        public int MaxHP;

        public Stats(int atk, float speed, int hp)
        {
            ATK = atk;
            gaugeSpeed = speed;
            CurrentHP = hp;
            MaxHP = hp;
        }

        public float GetResistance(string type) => 0f; // Stub: always 0
    }

    public class BuffInstance
    {
        public string Id;
        public int RemainingTurns;
        public float Amount;
        public Func<float, float> Apply; // function to modify stat or damage

        public BuffInstance(string id, int turns, float amount, Func<float, float> apply)
        {
            Id = id;
            RemainingTurns = turns;
            Amount = amount;
            Apply = apply;
        }
    }

    public class Skill
    {
        public string Id;
        public string Name;
        public SkillType Type;
        public float GaugeCost;
        public float BasePower;
        public string AttributeType;
        public BuffInstance Effect; // optional buff/debuff instance

        public Skill(string id, string name, SkillType type, float cost, float power, string attr, BuffInstance effect = null)
        {
            Id = id;
            Name = name;
            Type = type;
            GaugeCost = cost;
            BasePower = power;
            AttributeType = attr;
            Effect = effect;
        }
    }
    
    public interface IPerk
    {
        float OnGaugeGain(Unit unit, float gain);
        void BeforeSkill(Unit unit, Skill skill);
        float OnModifyDamage(Unit attacker, Unit defender, Skill skill, float damage);
        void AfterSkill(Unit unit, Skill skill);
    }

    public class Unit
    {
        public string Id;
        public Stats Stats;
        public List<Skill> Skills;
        public List<IPerk> Perks;
        public List<BuffInstance> Buffs = new List<BuffInstance>();
        public ReactiveProperty<float> CurrentGauge = new ReactiveProperty<float>(0f);
        public string DisplayName => Id;
        

        public Unit(string id, Stats stats, List<Skill> skills, List<IPerk> perks)
        {
            Id = id;
            Stats = stats;
            Skills = skills;
            Perks = perks;
        }

        public bool IsReady => CurrentGauge.Value >= 100f;
        public bool IsDead => Stats.CurrentHP <= 0;

        public void GainGauge(float amount)
        {
            float gain = amount;
            foreach (var p in Perks)
                gain = p.OnGaugeGain(this, gain);
            CurrentGauge.Value = Mathf.Min(100f, CurrentGauge.Value + gain);
        }

        public void ResetGauge() => CurrentGauge.Value = 0f;

        public void TakeDamage(int dmg)
        {
            // Apply defense buffs as multiplier
            float modified = dmg;
            foreach(var buff in Buffs)
                modified = buff.Apply(modified);

            int final = Mathf.Max(1, Mathf.RoundToInt(modified));
            Stats.CurrentHP -= final;
            Debug.Log($"[Damage] {Id} takes {final}, HP={Stats.CurrentHP}");
        }

        public void ApplyBuff(BuffInstance buff)
        {
            Buffs.Add(buff);
            Debug.Log($"[Buff] {Id} gains {buff.Id} for {buff.RemainingTurns} turns");
        }

        public void TickBuffs()
        {
            for(int i=Buffs.Count-1; i>=0; i--)
            {
                Buffs[i].RemainingTurns--;
                if(Buffs[i].RemainingTurns <= 0)
                {
                    Debug.Log($"[Buff] {Id} {Buffs[i].Id} expired");
                    Buffs.RemoveAt(i);
                }
            }
        }
    }

    // =====================================================
    // Service Interfaces
    // =====================================================
    public interface IPartyService { UniTask<List<Unit>> GetPartyUnitsAsync(); }
    public interface IEnemyService { UniTask<List<Unit>> GetEnemiesAsync(); }
    public interface IUIService
    {
        UniTask<Skill> PickSkillAsync(Unit actor);
        UniTask<Unit> PickTargetAsync(Unit actor, List<Unit> candidates);
        UniTask PlaySkillAnimationAsync(Unit actor, Skill skill, Unit target);
        void UpdateGaugeDisplay(Unit unit);
        
        void InitializeUnits(IEnumerable<Unit> allUnits, IEnumerable<Unit> allies, IEnumerable<Unit> enemies);
    }
    public interface IBattleService { UniTask StartBattleAsync(List<Unit> allies, List<Unit> enemies); }

    // =====================================================
    // Service Implementations
    // =====================================================
    public class PartyService : IPartyService
    {
        public UniTask<List<Unit>> GetPartyUnitsAsync()
        {
            var stats1 = new Stats(150,20f,500);
            var stats2 = new Stats(100,25f,400);
            var u1 = new Unit("rita", stats1, CreateSkills("Physical"), new List<IPerk>());
            var u2 = new Unit("fio", stats2, CreateSkills("Magic"), new List<IPerk>());
            return UniTask.FromResult(new List<Unit>{u1,u2});
        }
        List<Skill> CreateSkills(string attr) => new List<Skill>
        {
            new Skill("basic","Basic",SkillType.Basic,100,1.0f,attr),
            new Skill("heavy","Heavy",SkillType.Heavy,100,1.5f,attr),
            // Example effect: buff self +20% ATK for 2 turns
            new Skill("ult","Ultimate",SkillType.Ultimate,150,3.0f,attr,
                new BuffInstance("buff_atk20",2,0.2f, dmg=>dmg))
        };
    }

    public class EnemyService : IEnemyService
    {
        public UniTask<List<Unit>> GetEnemiesAsync()
        {
            var statsE = new Stats(120,18f,300);
            var statsF = new Stats(130,22f,350);
            var e1 = new Unit("goblin", statsE, CreateSkills("Physical"), new List<IPerk>());
            var e2 = new Unit("imp", statsF, CreateSkills("Fire"), new List<IPerk>());
            return UniTask.FromResult(new List<Unit>{e1,e2});
        }
        List<Skill> CreateSkills(string attr) => new List<Skill>
        {
            new Skill("e_basic","Claw",SkillType.Basic,100,1.0f,attr),
            new Skill("e_heavy","Charge",SkillType.Heavy,100,1.2f,attr,
                new BuffInstance("debuff_slow",2,-0.1f, dmg=>dmg)),
            new Skill("e_ult","Rage",SkillType.Ultimate,150,2.5f,attr)
        };
    }

    public class DebugUIService : IUIService
    {
        public UniTask<Skill> PickSkillAsync(Unit actor)
        {
            Debug.Log($"[UI] {actor.Id} picks {actor.Skills[0].Name}");
            return UniTask.FromResult(actor.Skills[0]);
        }
        public UniTask<Unit> PickTargetAsync(Unit actor, List<Unit> cands)
        {
            var target = cands.FirstOrDefault(u=>!u.IsDead);
            Debug.Log($"[UI] {actor.Id} targets {target.Id}");
            return UniTask.FromResult(target);
        }
        public UniTask PlaySkillAnimationAsync(Unit actor, Skill skill, Unit target)
        {
            Debug.Log($"[Anim] {actor.Id} uses {skill.Name} on {target.Id}");
            return UniTask.DelayFrame(20);
        }
        public void UpdateGaugeDisplay(Unit unit)
        {
            if(unit.CurrentGauge.Value >= 100f)
                Debug.Log($"[Gauge] {unit.Id} is ready");
            
            // Debug.Log($"[Gauge] {unit.Id}: {unit.CurrentGauge.Value:0.0}%");
        }

        public void InitializeUnits(IEnumerable<Unit> allUnits, IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            Debug.Log($"[UI] {allUnits.Count()} units");
        }
    }

    public class BattleService : IBattleService
    {
        readonly IUIService ui;
        [Inject]
        public BattleService(IUIService uiService)=>ui=uiService;
        

        public async UniTask StartBattleAsync(List<Unit> allies, List<Unit> enemies)
        {
            var all = allies.Concat(enemies).ToList();
            
            // アクションゲージバーにユニット登録
            
            
            Debug.Log("=== Battle Start ===");
            bool over=false;
            while(!over)
            {
                float dt=Time.deltaTime;
                foreach(var u in all.Where(u=>!u.IsReady && !u.IsDead))
                {
                    u.GainGauge(u.Stats.gaugeSpeed*dt);
                    ui.UpdateGaugeDisplay(u);
                }
                foreach(var actor in all.Where(u=>u.IsReady && !u.IsDead))
                {
                    // before skill perks
                    var skill=await ui.PickSkillAsync(actor);
                    foreach(var p in actor.Perks) p.BeforeSkill(actor,skill);

                    var targetList = allies.Contains(actor)? enemies: allies;
                    var target=await ui.PickTargetAsync(actor,targetList);

                    // raw damage
                    float raw=actor.Stats.ATK*skill.BasePower;
                    // apply perk damage modifiers
                    foreach(var p in actor.Perks) raw=p.OnModifyDamage(actor,target,skill,raw);
                    // apply buff/debuff apply on raw
                    int dmg=Math.Max(1,Mathf.RoundToInt(raw*(1f-target.Stats.GetResistance(skill.AttributeType))));
                    target.TakeDamage(dmg);

                    // apply skill effect buff/debuff
                    if(skill.Effect!=null)
                        target.ApplyBuff(skill.Effect);

                    await ui.PlaySkillAnimationAsync(actor,skill,target);
                    foreach(var p in actor.Perks) p.AfterSkill(actor,skill);

                    actor.ResetGauge();
                    actor.TickBuffs();
                    target.TickBuffs();

                    if(allies.All(a=>a.IsDead)||enemies.All(e=>e.IsDead)) { over=true; break; }
                }

                await UniTask.Yield();
                // await UniTask.Delay(100); // simulate frame delay
            }
            Debug.Log("=== Battle End ===");
        }
    }

    public class BattleSampleRunner : MonoBehaviour
    {
        [Inject] IPartyService partyService;
        [Inject] IEnemyService enemyService;
        [Inject] IBattleService battleService;
        [Inject] IUIService uiService;
        
        async void Start()
        {
            var allies=await partyService.GetPartyUnitsAsync();
            var enemiesList=await enemyService.GetEnemiesAsync();
            
            // UIService にユニット情報を渡す
            uiService.InitializeUnits(allies.Concat(enemiesList), allies, enemiesList);
            await battleService.StartBattleAsync(allies,enemiesList);
        }
    }

}


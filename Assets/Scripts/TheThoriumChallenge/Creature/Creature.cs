using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Creature : MovingEntity
    {
        // Comps
        private Comp_Stats Stats { get; set; }
        private Comp_Skills Skills { get; set; }
        private Comp_Creature CreatureInfo { get; set; }

        // Simulation
        public TimeStamp NextActionTime { get; private set; }
        public bool IsPlayerControlled { get; private set; }
        public bool IsInTurn { get; set; }
        public bool IsInAction { get; set; }
        public List<TurnAction> PossibleActions { get; private set; }

        // State
        public int Level { get; private set; }
        private int CurrentHP { get; set; }
        public float HP => InHealthChange ? HealthChange_CurrentHP : CurrentHP;

        // Health change animation
        private bool InHealthChange { get; set; } // Flag if the health of this creature is currently being changed in-animation (after taking damage or healing)
        private int HealthChange_SourceTick { get; set; } // The tick when the health change animation started
        private int HealthChange_TargetTick { get; set; } // The tick when the health change animation should be completed
        private int HealthChange_SourceHP { get; set; } // Used for animation when the health changes so it's not immediate
        private int HealthChange_TargetHP { get; set; } // Used for animation when the health changes so it's not immediate
        private float HealthChange_CurrentHP { get; set; } // Used for animation when the health changes to have float values for smoother transition
        private System.Action OnHealthChangeDoneCallback { get; set; }

        // Stats
        public int MaxHP => (int)Stats.GetStatValue(StatDefOf.MaxHP);

        // UI
        public UI_CreatureLabel OverheadLabel;

        // Helpers
        private Sprite _UiSprite;
        private GameObject _RenderModel;

        protected override void OnStartInitialization()
        {
            _RenderModel = Resources.Load<GameObject>("TheThoriumChallenge/CreatureModels/" + Def.DefName + "_fbx");
            _UiSprite = Resources.Load<Sprite>("TheThoriumChallenge/CreaturePreviewImages/" + Def.DefName);
        }

        protected override void OnCompInitialized(EntityComp comp)
        {
            base.OnCompInitialized(comp);

            if (comp is Comp_Stats statComp) Stats = statComp;
            if (comp is Comp_Skills skillComp) Skills = skillComp;
            if (comp is Comp_Creature creatureComp) CreatureInfo = creatureComp;
        }

        public void InitializeCreature(int level, bool isPlayerControlled)
        {
            IsPlayerControlled = isPlayerControlled;
            Level = level;

            CurrentHP = MaxHP;
            NextActionTime = new TimeStamp();

            InitUiLabel();
        }

        private void InitUiLabel()
        {
            OverheadLabel = GameObject.Instantiate(Game.Instance.UI.CreatureLabelPrefab, Game.Instance.UI.CreatureLabelContainer.transform);
            OverheadLabel.Init(this);
        }

        protected override void OnTick()
        {
            if (InHealthChange)
            {
                if(World.CurrentTick >= HealthChange_TargetTick) // HP change complete
                {
                    CurrentHP = HealthChange_TargetHP;
                    InHealthChange = false;
                    if (CurrentHP <= 0) Die();

                    OnHealthChangeDoneCallback.Invoke();
                }
                else // HP change ongoing
                {
                    float ratio = (1f * World.CurrentTick - HealthChange_SourceTick) / (1f * HealthChange_TargetTick - HealthChange_SourceTick);
                    int deltaHP = HealthChange_TargetHP - HealthChange_SourceHP;
                    HealthChange_CurrentHP = HealthChange_SourceHP + (ratio * deltaHP);
                }
            }
        }

        public void RefreshPossibleActions()
        {
            PossibleActions = new List<TurnAction>();

            // Do Nothing
            PossibleActions.Add(new TurnAction_DoNothing(this));

            // Abilities
            foreach(Ability ability in CreatureInfo.GetAllAbilities())
            {
                foreach(BlockmapNode possibleTarget in ability.GetPossibleTargets())
                {
                    TurnAction_UseAbility abilityAction = new TurnAction_UseAbility(this, possibleTarget, ability);
                    PossibleActions.Add(abilityAction);
                }
            }

            if (PossibleActions.Count == 0) Debug.LogWarning("No possible actions");
        }

        public void PerformNextAction()
        {
            if (!IsPlayerControlled)
            {
                Game.Instance.CurrentStage.PerformTurnAction(PossibleActions.RandomElement());
            }
        }

        public void EndTurn()
        {
            IsInTurn = false;
        }

        #region Damage

        public void ApplyDamage(DamageInfo info, System.Action onHealthChangeDoneCallback)
        {
            if (info.Target != this) throw new System.Exception("This creature does not seem to be the intended target of the damage.");
            Debug.Log($"{this}: Taking {info.Amount} {info.Type.Label} damage from {info.Source}");

            TakeDamage(Mathf.RoundToInt(info.Amount), onHealthChangeDoneCallback);
        }

        private void TakeDamage(int damage, System.Action onHealthChangeDoneCallback)
        {
            int targetHP = CurrentHP - damage;
            if (targetHP < 0) targetHP = 0;
            InitiateHealthChange(targetHP, onHealthChangeDoneCallback);
        }

        private void InitiateHealthChange(int targetHP, System.Action onHealthChangeDoneCallback)
        {
            OnHealthChangeDoneCallback = onHealthChangeDoneCallback;

            HealthChange_SourceHP = CurrentHP;
            HealthChange_TargetHP = targetHP;
            HealthChange_SourceTick = World.CurrentTick;

            int deltaHP = Mathf.Abs(HealthChange_TargetHP - HealthChange_SourceHP);
            int animationTickDuration = (int)(deltaHP * 5.0f);
            HealthChange_TargetTick = World.CurrentTick + animationTickDuration;

            InHealthChange = true;

            Debug.Log($"Initiating health change on {this}: {HealthChange_SourceHP} --> {HealthChange_TargetHP}");
        }

        public override string ToString()
        {
            string faction = IsPlayerControlled ? "Friendly" : "Hostile";
            return $"{faction} {LabelCap}, Level {Level}";
        }

        private void Die()
        {
            Debug.Log($"{this} died.");

            Stage.RemoveCreatureFromStage(this);

            GameObject.Destroy(OverheadLabel.gameObject);
            OverheadLabel = null;

            World.MarkEntityToBeRemovedThisTick(this);
        }

        #endregion

        #region Entity Properties

        public override Sprite UiSprite => _UiSprite;
        protected override GameObject RenderModel => _RenderModel;
        public override float VisionRange => Stats.GetStatValue(StatDefOf.VisionRange);
        public override float MovementSpeed => 5;
        public float CreatureMovementSpeed => Stats.GetStatValue(StatDefOf.MovementSpeed);

        #endregion

        #region Creature Properties

        public Stage Stage => Game.Instance.CurrentStage;
        public List<Ability> Abilities => CreatureInfo.GetAllAbilities();
        public List<CreatureClassDef> Classes => CreatureInfo.Props.Classes;

        #endregion
    }
}

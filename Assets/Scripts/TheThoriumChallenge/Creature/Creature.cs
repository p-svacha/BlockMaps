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
        public Comp_Stats Stats { get; private set; }
        public Comp_Skills Skills { get; private set; }
        public Comp_Abilities Abilities { get; private set; }

        // Simulation
        public TimeStamp NextActionTime { get; private set; }
        public bool IsPlayerControlled { get; private set; }
        public bool IsInTurn { get; set; }
        public bool IsInAction { get; set; }
        public List<TurnAction> PossibleActions { get; private set; }

        // State
        public int Level { get; private set; }
        public int HP { get; private set; }

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
            if (comp is Comp_Abilities abilitiesComp) Abilities = abilitiesComp;
        }

        public void InitializeCreature(int level, bool isPlayerControlled)
        {
            IsPlayerControlled = isPlayerControlled;
            Level = level;

            HP = MaxHP;
            NextActionTime = new TimeStamp();

            InitUiLabel();
        }

        private void InitUiLabel()
        {
            OverheadLabel = GameObject.Instantiate(Game.Instance.UI.CreatureLabelPrefab, Game.Instance.UI.CreatureLabelContainer.transform);
            OverheadLabel.Init(this);
        }

        public void RefreshPossibleActions()
        {
            PossibleActions = new List<TurnAction>();

            // Do Nothing
            PossibleActions.Add(new TurnAction_DoNothing(this));

            // Abilities
            foreach(Ability ability in Abilities.GetAllAbilities())
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

        #region Getters

        public override Sprite UiSprite => _UiSprite;
        protected override GameObject RenderModel => _RenderModel;
        public override float VisionRange => Stats.GetStatValue(StatDefOf.VisionRange);
        public override float MovementSpeed => 5;
        public float CreatureMovementSpeed => Stats.GetStatValue(StatDefOf.MovementSpeed);

        #endregion
    }
}

using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class CtfCharacter : Entity
    {
        private const float BASE_MOVEMENT_COST_MODIFIER = 10;

        public CtfMatch Match;
        public Player Owner { get; private set; }
        public Player Opponent { get; private set; }

        // Components
        public Comp_Movement MovementComp { get; private set; }
        public Comp_CtfCharacter CtfComp { get; private set; }

        // Current stats
        public float ActionPoints { get; private set; }
        public float Stamina { get; private set; }
        public int JailTime { get; private set; }
        public bool IsInJail => JailTime > 0;
        public Dictionary<BlockmapNode, Action_Movement> PossibleMoves { get; private set; }
        public List<SpecialCharacterAction> PossibleSpecialActions { get; private set; } // Actions that can be performed via button
        public CharacterAction CurrentAction { get; private set; }

        // UI
        public UI_CharacterLabel UI_Label;

        #region Init

        protected override void OnCompInitialized(EntityComp comp)
        {
            if (comp is Comp_Movement move) MovementComp = move;
            if (comp is Comp_CtfCharacter ctf) CtfComp = ctf;
        }

        #endregion

        #region Game Loop

        public void OnMatchReady(CtfMatch game, Player player, Player opponent)
        {
            Match = game;
            ActionPoints = MaxActionPoints;
            Stamina = MaxStamina;
            Owner = player;
            Opponent = opponent;

            // Create label
            UI_Label = GameObject.Instantiate(game.UI.CharacterLabelPrefab, game.UI.CharacterLabelsContainer.transform);
            UI_Label.Init(this);
        }

        public void OnStartTurn()
        {
            // Base action point and stamina regeneration
            ActionPoints = MaxActionPoints;
            Stamina += StaminaRegeneration;
            if (Stamina > MaxStamina) Stamina = MaxStamina;

            // No movement if in jail
            if (IsInJail) JailTime--;
            if (IsInJail) ActionPoints = 0;

            UpdatePossibleActions();
        }

        public void SetCurrentAction(CharacterAction action)
        {
            CurrentAction = action;
        }

        #endregion

        #region Actions

        public void SetJailTime(int turns)
        {
            JailTime = turns;
        }

        public void UpdatePossibleActions()
        {
            PossibleMoves = GetPossibleMoves();
            PossibleSpecialActions = GetSpecialActions();
        }

        public void ReduceActionAndStamina(float amount)
        {
            ActionPoints -= amount;
            Stamina -= amount;
        }

        public void SetActionPointsToZero()
        {
            ActionPoints = 0;
        }

        #endregion

        #region Getters

        public Vector2Int WorldCoordinates => OriginNode.WorldCoordinates;
        public bool IsInAction => CurrentAction != null;
        public BlockmapNode Node => OriginNode;
        public bool IsVisible => IsVisibleBy(Match.World.ActiveVisionActor);
        public bool IsVisibleByOpponent => IsVisibleBy(Owner.Opponent.Actor);
        public bool IsInOpponentTerritory => Owner.Opponent.Territory.ContainsNode(OriginNode);

        // Stats
        public Sprite Avatar => CtfComp.Avatar;
        public float MaxActionPoints => CtfComp.MaxActionPoints;

        public override float MovementSpeed => MovementComp.IsOverrideMovementSpeedActive ? MovementComp.MovementSpeed : CtfComp.GetStat(StatDefOf.Speed) * 0.2f;

        public List<Stat> GetAllStats() => CtfComp.GetAllStats();
        public float MovementSkill => CtfComp.GetStat(StatDefOf.Speed);
        public override float VisionRange => CtfComp.GetStat(StatDefOf.Vision);
        public float MaxStamina => CtfComp.GetStat(StatDefOf.MaxStamina);
        public float StaminaRegeneration => CtfComp.GetStat(StatDefOf.StaminaRegeneration);

        public override ClimbingCategory ClimbingSkill => CtfComp.GetStat(StatDefOf.Climbing) == 0 ? ClimbingCategory.None : ClimbingCategory.Intermediate;
        public override float ClimbingAptitude => CtfComp.GetStat(StatDefOf.Climbing);
        public override bool CanSwim => CtfComp.GetStat(StatDefOf.Swimming) > 0;
        public override int MaxHopUpDistance => (int)CtfComp.GetStat(StatDefOf.Jumping);
        public override int MaxHopDownDistance => (int)CtfComp.GetStat(StatDefOf.Dropping);
        public bool CanInteractWithDoors => CtfComp.GetStat(StatDefOf.CanUseDoors) == 1;

        public override Vector3Int Dimensions => new Vector3Int(Def.Dimensions.x, (int)CtfComp.GetStat(StatDefOf.Height), Def.Dimensions.z);

        public override float GetSurfaceAptitude(SurfaceDef def)
        {
            if (def == SurfaceDefOf.Water) return CtfComp.GetStat(StatDefOf.Swimming);
            return base.GetSurfaceAptitude(def);
        }

        /// <summary>
        /// Returns a list of possible moves that this character can undertake with default movement within this turn with their remaining action points.
        /// </summary>
        private Dictionary<BlockmapNode, Action_Movement> GetPossibleMoves()
        {
            Dictionary<BlockmapNode, Action_Movement> movements = new Dictionary<BlockmapNode, Action_Movement>();

            Dictionary<BlockmapNode, float> priorityQueue = new Dictionary<BlockmapNode, float>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();
            Dictionary<BlockmapNode, float> nodeCosts = new Dictionary<BlockmapNode, float>();
            Dictionary<BlockmapNode, NavigationPath> nodePaths = new Dictionary<BlockmapNode, NavigationPath>();

            // Start with origin node
            priorityQueue.Add(OriginNode, 0f);
            nodeCosts.Add(OriginNode, 0f);
            nodePaths.Add(OriginNode, new NavigationPath(OriginNode));

            while(priorityQueue.Count > 0)
            {
                BlockmapNode currentNode = priorityQueue.OrderBy(x => x.Value).First().Key;
                priorityQueue.Remove(currentNode);

                if (visited.Contains(currentNode)) continue;
                visited.Add(currentNode);

                foreach(Transition t in currentNode.Transitions)
                {
                    BlockmapNode targetNode = t.To;
                    float transitionCost = GetActionPointCost(t);
                    float totalCost = nodeCosts[currentNode] + transitionCost;

                    if (totalCost > ActionPoints) continue; // not reachable with current action points
                    if (totalCost > Stamina) continue; // not reachable with current stamina
                    if (!t.CanPass(this)) continue; // transition not passable for this character
                    if (!t.To.IsExploredBy(Actor)) continue; // node not explored

                    // Node has not yet been visited or cost is lower than previously lowest cost => Update
                    if(!nodeCosts.ContainsKey(targetNode) || totalCost < nodeCosts[targetNode])
                    {
                        // Update cost to this node
                        nodeCosts[targetNode] = totalCost;

                        // Update path to this node.
                        NavigationPath path = new NavigationPath(nodePaths[currentNode]);
                        path.AddTransition(t);
                        nodePaths[targetNode] = path;

                        // Add target node to queue to continue search
                        if(!priorityQueue.ContainsKey(targetNode) || priorityQueue[targetNode] > totalCost) 
                            priorityQueue[targetNode] = totalCost;

                        // Check if we can stand on that tile (different check than IsPassable - a node can be passable but not eligible to stand on)
                        if (!CanStandOn(targetNode)) continue;

                        // Add target node to possible moves
                        movements[targetNode] = new Action_Movement(this, nodePaths[targetNode], nodeCosts[targetNode]);
                    }
                }
            }

            return movements;
        }

        private float GetActionPointCost(Transition t)
        {
            return t.GetMovementCost(this) * (1f / MovementSkill) * BASE_MOVEMENT_COST_MODIFIER;
        }

        /// <summary>
        /// Returns a list of all actions that can be performed via button.
        /// </summary>
        private List<SpecialCharacterAction> GetSpecialActions()
        {
            List<SpecialCharacterAction> actions = new List<SpecialCharacterAction>();

            // Go to jail
            if(!IsInJail) actions.Add(new Action_GoToJail(this));

            // Door interaction
            if (CanInteractWithDoors)
            {
                foreach (Door door in World.GetNearbyEntities(WorldPosition, maxDistance: 2f).Where(e => e is Door && e.IsExploredBy(Actor)))
                {
                    actions.Add(new Action_InteractWithDoor(this, door));
                }
            }
            
            // Ladder transition
            if(ClimbingSkill != ClimbingCategory.None)
            {
                foreach (Ladder ladder in OriginNode.SourceLadders.Values)
                {
                    Transition targetTransition = ladder.GetTransition(from: OriginNode);
                    if (targetTransition != null) actions.Add(new Action_UseLadder(this, targetTransition));
                }

                foreach (Ladder ladder in OriginNode.TargetLadders.Values)
                {
                    Transition targetTransition = ladder.GetTransition(from: OriginNode);
                    if(targetTransition != null) actions.Add(new Action_UseLadder(this, targetTransition));
                }
            }

            return actions;
        }

        /// <summary>
        /// Returns if this character can stand on / stop on the given node. This is independent from IsPassable, a node can be passable but not able to stand on.
        /// </summary>
        private bool CanStandOn(BlockmapNode targetNode)
        {
            if (targetNode.Entities.Any(x => x == Owner.Flag)) return false; // can't stand on own flag
            if (Owner.FlagZone.ContainsNode(targetNode)) return false; // Can't stand in own flag zone

            return true;
        }

        #endregion


    }
}

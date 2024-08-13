using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Character : MonoBehaviour
    {
        private const float BASE_MOVEMENT_COST_MODIFIER = 10;

        public CTFGame Game;
        public MovingEntity Entity { get; private set; }
        public Player Owner { get; private set; }
        public Player Opponent { get; private set; }

        [Header("Attributes")]
        public Sprite Avatar;
        public string Name;
        public float MaxActionPoints;
        public float MaxStamina;
        public float StaminaRegeneration;
        public float MovementSkill;

        // Current stats
        public float ActionPoints { get; private set; }
        public float Stamina { get; private set; }
        public int JailTime { get; private set; }
        public bool IsInJail => JailTime > 0;
        public Dictionary<BlockmapNode, Action_Movement> PossibleMoves { get; private set; }
        public List<SpecialAction> PossibleSpecialActions { get; private set; } // Actions that can be performed via button
        private CharacterAction CurrentAction;

        // UI
        public UI_CharacterLabel UI_Label;

        private void Awake()
        {
            Entity = GetComponent<MovingEntity>(); 
        }

        #region Game Loop

        public void OnStartGame(CTFGame game, Player player, Player opponent)
        {
            Game = game;
            ActionPoints = MaxActionPoints;
            Stamina = MaxStamina;
            Owner = player;
            Opponent = opponent;

            // Create label
            UI_Label = Instantiate(game.UI.CharacterLabelPrefab, game.UI.CharacterLabelsContainer.transform);
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

        public Vector2Int WorldCoordinates => Entity.OriginNode.WorldCoordinates;
        public bool IsInAction => CurrentAction != null;
        public BlockmapNode Node => Entity.OriginNode;
        public bool IsVisible => Entity.IsVisibleBy(Game.World.ActiveVisionActor);
        public bool IsVisibleByOpponent => Entity.IsVisibleBy(Owner.Opponent.Actor);
        public bool IsInOpponentZone => Owner.Opponent.Territory.ContainsNode(Entity.OriginNode);

        /// <summary>
        /// Returns a list of possible moves that this character can undertake with default movement within this turn with their remaining action points.
        /// </summary>
        private Dictionary<BlockmapNode, Action_Movement> GetPossibleMoves()
        {
            Dictionary<BlockmapNode, Action_Movement> movements = new Dictionary<BlockmapNode, Action_Movement>();

            Dictionary<BlockmapNode, float> priorityQueue = new Dictionary<BlockmapNode, float>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();
            Dictionary<BlockmapNode, float> nodeCosts = new Dictionary<BlockmapNode, float>();
            Dictionary<BlockmapNode, List<BlockmapNode>> nodePaths = new Dictionary<BlockmapNode, List<BlockmapNode>>();

            // Start with origin node
            priorityQueue.Add(Entity.OriginNode, 0f);
            nodeCosts.Add(Entity.OriginNode, 0f);
            nodePaths.Add(Entity.OriginNode, new List<BlockmapNode>() { Entity.OriginNode });

            while(priorityQueue.Count > 0)
            {
                BlockmapNode currentNode = priorityQueue.OrderBy(x => x.Value).First().Key;
                priorityQueue.Remove(currentNode);

                if (visited.Contains(currentNode)) continue;
                visited.Add(currentNode);

                foreach(KeyValuePair<BlockmapNode, Transition> t in currentNode.Transitions)
                {
                    BlockmapNode targetNode = t.Key;
                    float transitionCost = t.Value.GetMovementCost(Entity) * (1f / MovementSkill) * BASE_MOVEMENT_COST_MODIFIER;
                    float totalCost = nodeCosts[currentNode] + transitionCost;

                    if (totalCost > ActionPoints) continue; // not reachable with current action points
                    if (totalCost > Stamina) continue; // not reachable with current stamina
                    if (!t.Value.CanPass(Entity)) continue; // transition not passable for this character
                    if (!t.Key.IsExploredBy(Entity.Owner)) continue; // node not explored

                    // Node has not yet been visited or cost is lower than previously lowest cost => Update
                    if(!nodeCosts.ContainsKey(targetNode) || totalCost < nodeCosts[targetNode])
                    {
                        // Update cost to this node
                        nodeCosts[targetNode] = totalCost;

                        // Update path to this node.
                        List<BlockmapNode> path = new List<BlockmapNode>(nodePaths[currentNode]);
                        path.Add(targetNode);
                        nodePaths[targetNode] = path;

                        // Add target node to queue to continue search
                        if(!priorityQueue.ContainsKey(targetNode) || priorityQueue[targetNode] > totalCost) 
                            priorityQueue[targetNode] = totalCost;

                        // Check if we can stand on that tile (different check than IsPassable - a node can be passable but not eligible to stand on)
                        if (!CanStandOn(targetNode)) continue;

                        // Add target node to possible moves
                        movements[targetNode] = new Action_Movement(Game, this, nodePaths[targetNode], nodeCosts[targetNode]);
                    }
                }
            }

            return movements;
        }

        /// <summary>
        /// Returns a list of all actions that can be performed via button.
        /// </summary>
        /// <returns></returns>
        private List<SpecialAction> GetSpecialActions()
        {
            List<SpecialAction> actions = new List<SpecialAction>();

            // Go to jail
            if(!IsInJail) actions.Add(new Action_GoToJail(Game, this));

            return actions;
        }

        /// <summary>
        /// Returns if this character can stand on / stop on the given node. This is independent from IsPassable, a node can be passable but not able to stand on.
        /// </summary>
        private bool CanStandOn(BlockmapNode targetNode)
        {
            if (Game.GetCharacters(targetNode).Any(x => x.Owner == Owner)) return false; // can't stand on ally characters
            if (Game.NeutralZone.ContainsNode(targetNode) && Game.GetCharacters(targetNode).Count > 0) return false; // can't stand on any characters in neutral zone
            if (targetNode.Entities.Any(x => x == Owner.Flag)) return false; // can't stand on own flag
            if (Owner.FlagZone.ContainsNode(targetNode)) return false; // Can't stand in own flag zone

            return true;
        }


        #endregion
    }
}

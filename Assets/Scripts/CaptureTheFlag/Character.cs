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
        public Dictionary<BlockmapNode, Movement> PossibleMoves { get; private set; }
        private CharacterAction CurrentAction;

        // Event
        public event System.Action OnTargetReached;

        private void Awake()
        {
            Entity = GetComponent<MovingEntity>(); 
        }

        #region Game Loop

        public void OnStartGame(CTFGame game, Player player)
        {
            Game = game;
            ActionPoints = MaxActionPoints;
            Stamina = MaxStamina;
            Owner = player;

            Entity.OnTargetReached += OnActionDone;
        }

        public void OnStartTurn()
        {
            // Base action point and stamina regeneration
            ActionPoints = MaxActionPoints;
            Stamina += StaminaRegeneration;
            if (Stamina > MaxStamina) Stamina = MaxStamina;

            // No movement if in jail
            if (JailTime > 0) JailTime--;
            if (JailTime > 0) ActionPoints = 0;

            UpdatePossibleMoves();
        }

        private void OnActionDone()
        {
            Game.OnActionDone(this, CurrentAction);
            CurrentAction = null;
        }

        #endregion

        #region Actions

        public void PerformAction(CharacterAction action)
        {
            CurrentAction = action;
            action.Perform(this);
        }

        public void SetJailTime(int turns)
        {
            JailTime = turns;
        }

        #endregion

        public void UpdatePossibleMoves()
        {
            PossibleMoves = GetPossibleMoves();
        }

        /// <summary>
        /// Returns a list of possible moves that this character can undertake with default movement within this turn with their remaining action points.
        /// </summary>
        private Dictionary<BlockmapNode, Movement> GetPossibleMoves()
        {
            Dictionary<BlockmapNode, Movement> movements = new Dictionary<BlockmapNode, Movement>();

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
                        movements[targetNode] = new Movement(nodePaths[targetNode], nodeCosts[targetNode]);
                    }
                }
            }

            return movements;
        }

        /// <summary>
        /// Returns if this character can stand on / stop on the given node. This is independent from IsPassable, a node can be passable but not able to stand on.
        /// </summary>
        private bool CanStandOn(BlockmapNode targetNode)
        {
            if (Game.GetCharacters(targetNode).Any(x => x.Owner == Owner)) return false; // can't stand on ally characters
            if (Game.NeutralZone.ContainsNode(targetNode) && Game.GetCharacters(targetNode).Count > 0) return false; // can't stand on any characters in neutral zone
            if (targetNode.Entities.Any(x => x == Owner.Flag)) return false; // can't stand on own flag

            return true;
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

        public bool IsInAction => CurrentAction != null;
        public BlockmapNode Node => Entity.OriginNode;
    }
}

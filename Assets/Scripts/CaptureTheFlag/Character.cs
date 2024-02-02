using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Character : MonoBehaviour
    {
        public MovingEntity Entity { get; private set; }

        [Header("Attributes")]
        public Sprite Avatar;
        public float MaxActionPoints;
        public float MaxStamina;
        public float StaminaRegeneration;

        // Current stats
        public float ActionPoints { get; private set; }
        public float Stamina { get; private set; }

        private void Awake()
        {
            Entity = GetComponent<MovingEntity>(); 
        }

        public void OnStartGame()
        {
            ActionPoints = MaxActionPoints;
            Stamina = MaxStamina;
        }

        public void OnStartTurn()
        {
            ActionPoints = MaxActionPoints;
            Stamina += StaminaRegeneration;
            if (Stamina > MaxStamina) Stamina = MaxStamina;
        }

        /// <summary>
        /// Returns a set of nodes that this character can reach with default movement within this turn with their remaining action points.
        /// </summary>
        /// <returns></returns>
        public HashSet<BlockmapNode> GetReachableNodes()
        {
            HashSet<BlockmapNode> nodes = new HashSet<BlockmapNode>();

            Dictionary<BlockmapNode, float> priorityQueue = new Dictionary<BlockmapNode, float>();
            HashSet<BlockmapNode> visited = new HashSet<BlockmapNode>();
            Dictionary<BlockmapNode, float> nodeCosts = new Dictionary<BlockmapNode, float>();

            // Start with origin node
            priorityQueue.Add(Entity.OriginNode, 0f);
            nodeCosts.Add(Entity.OriginNode, 0f);

            while(priorityQueue.Count > 0)
            {
                BlockmapNode currentNode = priorityQueue.OrderBy(x => x.Value).First().Key;
                priorityQueue.Remove(currentNode);

                if (visited.Contains(currentNode)) continue;
                visited.Add(currentNode);

                foreach(KeyValuePair<BlockmapNode, Transition> kvp in currentNode.Transitions)
                {
                    BlockmapNode targetNode = kvp.Key;
                    float transitionCost = kvp.Value.GetMovementCost(Entity);
                    float totalCost = nodeCosts[currentNode] + transitionCost;

                    if (totalCost > ActionPoints) continue; // not reachable with current action points
                    if (!kvp.Value.CanPass(Entity)) continue; // transition not passable for this character

                    if(!nodeCosts.ContainsKey(targetNode) || totalCost < nodeCosts[targetNode])
                    {
                        nodeCosts[targetNode] = totalCost;

                        priorityQueue.Add(targetNode, totalCost);
                        nodes.Add(targetNode);
                    }
                }
            }

            return nodes;
        }
    }
}

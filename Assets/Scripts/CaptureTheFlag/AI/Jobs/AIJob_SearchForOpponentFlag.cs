using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIJob_SearchForOpponentFlag : AICharacterJob
    {
        private BlockmapNode TargetNode;
        private BlockmapNode ViaNode;
        private NavigationPath TargetPath;

        // AICharacterJob Base
        public override AICharacterJobId Id => AICharacterJobId.SearchOpponentFlag;
        public override string DevmodeDisplayText => "Searching Flag (going to " + TargetNode + ")" + (ViaNode != null ? (" via " + ViaNode) : "");

        public AIJob_SearchForOpponentFlag(CtfCharacter c) : base(c)
        {
            SetNewTargetNode();
        }

        /// <summary>
        /// Sets the target node and target path to random reachable unexplored node in opponet territory
        /// </summary>
        private void SetNewTargetNode()
        {
            // Find a target node
            int attempts = 0;
            int maxAttempts = 10;
            while (TargetNode == null && attempts < maxAttempts)
            {
                attempts++;
                List<BlockmapNode> candidates = Match.LocalPlayerZone.Nodes.Where(x => !x.IsExploredBy(Player.Actor) && x.IsPassable(Character)).ToList();
                BlockmapNode targetNode = candidates[Random.Range(0, candidates.Count)];
                NavigationPath targetPath = GetPath(targetNode);
                if (targetPath != null) // valid target that we can reach
                {
                    TargetNode = targetNode;
                    TargetPath = targetPath;
                }
            }
        }

        public override void OnNextActionRequested()
        {
            // Check if we are at the via node
            if (ViaNode != null && IsOnOrNear(ViaNode))
            {
                ViaNode = null;
                SetNewTargetNode();
            }

            // If we are in neutral territory and see an opponent, make a detour in the neutral territory
            if (ViaNode == null && Player.ShouldMakeNeutralDetour(Character))
            {
                ViaNode = Match.NeutralZone.Nodes.RandomElement();
            }
        }

        public override bool ShouldStopJob(out AICharacterJob forcedNewJob)
        {
            forcedNewJob = null;

            // Check if our path is still valid
            if (TargetPath == null) return true;
            if (!TargetPath.CanPass(Character)) return true;

            // If we can tag an opponent this turn, do that
            if (Player.CanTagCharacterDirectly(Character, out CtfCharacter target0))
            {
                forcedNewJob = new AIJob_ChaseToTagOpponent(Character, target0);
                return true;
            }

            // If we should flee, do that
            if(Player.ShouldFlee(Character))
            {
                forcedNewJob = new AIJob_Flee(Character);
                return true;
            }

            // If we are on or close to our target node, look for new job
            if (IsOnOrNear(TargetNode)) return true;

            return false;
        }



        public override CharacterAction GetNextAction()
        {
            if (ViaNode != null) return GetSingleNodeMovementTo(ViaNode);
            else return GetSingleNodeMovementTo(TargetNode);
        }
    }
}

using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class AIPlayer : Player
    {
        public bool TurnFinished { get; private set; }

        private Dictionary<Character, AICharacterJob> Jobs = new Dictionary<Character, AICharacterJob>();
        private Dictionary<Character, CharacterAction> Actions = new Dictionary<Character, CharacterAction>();
        private List<BlockmapNode> TargetNodes = new List<BlockmapNode>();

        private List<CharacterAction> VisibleActions = new List<CharacterAction>();
        private int VisibleActionIndex; // which of the visible actions is currently being shown

        public AIPlayer(Actor actor, Zone jailZone, Zone flagZone) : base(actor, jailZone, flagZone) { }


        public void StartTurn()
        {
            TurnFinished = false;

            // Assign a job for each character
            Jobs.Clear();
            for(int i = 0; i < Characters.Count; i++)
            {
                if (i < 2) Jobs.Add(Characters[i], AICharacterJob.DefendFlag); // 2 Defenders
                else Jobs.Add(Characters[i], AICharacterJob.AttackEnemyFlag); // 6 Attackers
            }

            // Get action for each character depending on job
            Actions.Clear();
            TargetNodes.Clear();
            foreach(Character c in Characters)
            {
                Actions.Add(c, GetCharacterAction(c));
            }

            // Sort actions into visible and hidden (for local player)
            VisibleActions.Clear();
            VisibleActionIndex = 0;
            List<CharacterAction> hiddenActions = new List<CharacterAction>();
            foreach (Character c in Characters)
            {
                if (Actions[c].IsVisibleBy(Game.LocalPlayer)) VisibleActions.Add(Actions[c]);
                else hiddenActions.Add(Actions[c]);
            }

            // Instantly perform all hidden actions
            foreach (CharacterAction action in hiddenActions) action.Perform();
        }

        /// <summary>
        /// Returns the action the given character will do this turn depending on their job and game state.
        /// </summary>
        private CharacterAction GetCharacterAction(Character c)
        {
            if (c.PossibleMoves.Count == 0) return null;

            // Move to random reachable node, with heigher weights for nodes that are further west
            Dictionary<Movement, float> movementProbabilities = new Dictionary<Movement, float>();
            int maxX = c.PossibleMoves.Max(x => x.Value.Target.WorldCoordinates.x);
            foreach (var possibleMove in c.PossibleMoves)
            {
                if (TargetNodes.Contains(possibleMove.Value.Target)) continue; // Can't go on a node that another character is going to already
                movementProbabilities.Add(possibleMove.Value, maxX - possibleMove.Value.Target.WorldCoordinates.x + 1);
            }

            Movement randomMove = HelperFunctions.GetWeightedRandomElement(movementProbabilities);
            TargetNodes.Add(randomMove.Target);

            return randomMove;
        }

        public void UpdateTurn()
        {
            // Check if AI turn is finished
            if (Characters.All(x => !x.IsInAction) && VisibleActionIndex == VisibleActions.Count) TurnFinished = true;

            // Update visible action
            if(VisibleActionIndex < VisibleActions.Count)
            {
                CharacterAction currentVisibleAction = VisibleActions[VisibleActionIndex];
                if(currentVisibleAction.State == CharacterActionState.Pending)
                {
                    currentVisibleAction.Perform();
                }
                else if(currentVisibleAction.State == CharacterActionState.Performing)
                {
                    currentVisibleAction.UpdateVisibleOpponentAction();
                }
                else if(currentVisibleAction.State == CharacterActionState.Done)
                {
                    VisibleActionIndex++;
                }
            }
        }

        private enum AICharacterJob
        {
            DefendFlag,             // Stay near own flag
            AttackEnemyFlag,        // Go straight towards enemy flag
            AttackEnemyCharacter,   // Go straight towards an enemy character in own half
            Sneak,                  // Go towards enemy flag where noone sees you
            Retreat,                // Go back to own half
            Hide                    // Go away from all enemy characters
        }
    }
}

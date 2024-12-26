using BlockmapFramework;
using CaptureTheFlag.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    /// <summary>
    /// An action for default moving from one node to another.
    /// </summary>
    public class Action_Movement : CharacterAction
    {
        /// <summary>
        /// Exact order of nodes that are traversed for this movement, including the origin and target node.
        /// </summary>
        public NavigationPath Path { get; private set; }
        public BlockmapNode Target => Path.Target;

        public Action_Movement(CtfMatch game, CtfCharacter c, NavigationPath path, float cost) : base(game, c, cost)
        {
            Path = path;
        }

        public override bool CanPerformNow()
        {
            // Check if another character is currently heading to the target node
            foreach (CtfCharacter character in Match.Characters)
            {
                if (character.IsInAction && character.CurrentAction is Action_Movement otherMove && otherMove.Target == Target) return false;
            }

            return base.CanPerformNow();
        }

        protected override void OnStartPerform()
        {
            // Subsribe to OnTargetReached so we know when character is done moving
            Character.MovementComp.OnTargetReached += OnCharacterReachedTarget;

            // Start movement of character entity
            Character.MovementComp.MoveAlong(Path);
        }

        public override void DoPause()
        {
            Character.MovementComp.PauseMovement();
        }
        public override void DoUnpause()
        {
            Character.MovementComp.UnpauseMovement();
        }

        private void OnCharacterReachedTarget()
        {
            Character.MovementComp.OnTargetReached -= OnCharacterReachedTarget;
            EndAction();
        }

        public override NetworkMessage_CharacterAction GetNetworkAction()
        {
            return new NetworkMessage_CharacterAction("CharacterAction_MoveCharacter", Character.Id, Target.Id);
        }
    }
}

using BlockmapFramework;
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
        public List<BlockmapNode> Path { get; private set; }
        public BlockmapNode Target { get; private set; }

        public Action_Movement(CTFGame game, Character c, List<BlockmapNode> path, float cost) : base(game, c, cost)
        {
            Path = path;
            Target = path.Last();
        }

        protected override void OnStartPerform()
        {
            // Subsribe to OnTargetReached so we know when character is done moving
            Character.Entity.OnTargetReached += OnCharacterReachedTarget;

            // Start movement of character entity
            Character.Entity.Move(Path);
        }

        public override void DoPause()
        {
            Character.Entity.PauseMovement();
        }
        public override void DoUnpause()
        {
            Character.Entity.UnpauseMovement();
        }

        private void OnCharacterReachedTarget()
        {
            Character.Entity.OnTargetReached -= OnCharacterReachedTarget;
            EndAction();
        }
    }
}

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
    public class Movement : CharacterAction
    {
        /// <summary>
        /// Exact order of nodes that are traversed for this movement, including the origin and target node.
        /// </summary>
        public List<BlockmapNode> Path { get; private set; }
        public BlockmapNode Target { get; private set; }

        public Movement(CTFGame game, Character c, List<BlockmapNode> path, float cost) : base(game, c)
        {
            Path = path;
            Target = path.Last();
            Cost = cost;
        }

        protected override void OnStartPerform()
        {
            // Subsribe to OnTargetReached so we know when character is done moving
            Character.Entity.OnTargetReached += OnCharacterReachedTarget;

            // Start movement of character entity
            Character.Entity.Move(Path);
        }

        public override void UpdateVisibleOpponentAction()
        {
            // Check if node that the character is currently on is visible to the player
            bool characterVisible = Character.Entity.IsVisibleBy(Game.LocalPlayer.Actor);

            // Pause movement and move camera to pan to character
            if(characterVisible && !Game.World.Camera.IsPanning && Game.World.Camera.FollowEntity != Character.Entity)
            {
                Character.Entity.PauseMovement();
                Game.World.CameraPanToFocusEntity(Character.Entity, duration: 1f, followAfterPan: true);
            }
            // Unpause and continue movement is camera is following entity
            else if(characterVisible && Game.World.Camera.FollowEntity == Character.Entity)
            {
                Character.Entity.UnpauseMovement();
            }
            // Unfollow if character goes out of vision
            else if(!characterVisible)
            {
                Game.World.Camera.Unfollow();
            }
        }

        private void OnCharacterReachedTarget()
        {
            Character.Entity.OnTargetReached -= OnCharacterReachedTarget;
            Game.World.Camera.Unfollow(); // Unfollow any entity at the end of movement (needed for opponent turns that camera follows)
            EndAction();
        }

        public override bool IsVisibleBy(Player p)
        {
            return Path.Any(x => x.IsVisibleBy(p.Actor));
        }
    }
}

using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Player
    {
        public CTFGame Game;
        public Actor Actor;
        public Entity Flag;
        public List<Character> Characters;
        public Zone Territory;
        public Zone JailZone;
        public Zone FlagZone;
        public Player Opponent;

        /// <summary>
        /// Dictionary containing the current/last action for each character.
        /// </summary>
        public Dictionary<Character, CharacterAction> Actions = new Dictionary<Character, CharacterAction>();

        public Player(Actor actor, Zone territory, Zone jailZone, Zone flagZone)
        {
            Actor = actor;

            Characters = new List<Character>();
            foreach (Entity e in actor.Entities)
            {
                if (e.Def.DefName == CTFMapGenerator.FLAG_ID) Flag = e;
                if (e is Character c) Characters.Add(c);
            }

            Territory = territory;
            JailZone = jailZone;
            FlagZone = flagZone;
        }

        public virtual void OnStartGame(CTFGame game)
        {
            Game = game;
            foreach (Character c in Characters) c.OnStartGame(Game, this, Opponent);
        }

        /// <summary>
        /// Gets called when a character of this player has completed their action.
        /// </summary>
        public virtual void OnActionDone(CharacterAction action) { }

        #region Getters

        public World World => Actor.World;

        /// <summary>
        /// Returns if a movement action can currently be performed.
        /// </summary>
        public bool CanPerformMovement(Action_Movement move)
        {
            if (move.Character.Owner != this) throw new System.Exception("Can only check actions from characters of this player");

            // Check if character is currently performing another action
            if (move.Character.IsInAction) return false;

            // Check if another character is currently heading to the target node
            foreach(CharacterAction action in Actions.Values)
            {
                if (!action.IsDone && action is Action_Movement otherMove && otherMove.Target == move.Target) return false;
            }

            return true;
        }

        #endregion
    }
}

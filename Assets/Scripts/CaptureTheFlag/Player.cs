using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Player
    {
        public string Label => Actor.Label;
        public CtfMatch Match;
        public Actor Actor;
        public Entity Flag;
        public List<CtfCharacter> Characters;
        public Zone Territory;
        public Zone JailZone;
        public Zone FlagZone;
        public Player Opponent;

        /// <summary>
        /// The order of nodes that characters will be placed on when going to jail.
        /// </summary>
        public List<BlockmapNode> JailPositions;
        private int CurrentJailPositionIndex;

        // Multiplayer
        public string ClientId;
        public bool ReadyToStartMultiplayerMatch;
        public bool TurnEnded;

        public Player(Actor actor, Zone territory, Zone jailZone, Zone flagZone, string clientId = "")
        {
            Actor = actor;
            ClientId = clientId;

            Characters = new List<CtfCharacter>();
            foreach (Entity e in actor.Entities)
            {
                if (e.Def == EntityDefOf.Flag) Flag = e;
                if (e is CtfCharacter c) Characters.Add(c);
            }

            Territory = territory;
            JailZone = jailZone;
            FlagZone = flagZone;
        }

        public virtual void OnMatchReady(CtfMatch match)
        {
            Match = match;

            // Set determinstic jail positions
            JailPositions = JailZone.Nodes.Where(x => x.IsPassable()).ToList().GetShuffledList();
            CurrentJailPositionIndex = 0;

            // Debug
            string log = "";
            foreach(BlockmapNode n in JailPositions) log += n.Id + "/";
            Debug.Log($"Jail Positions for {Label}: {log}");

            // Inform characters about match ready
            foreach (CtfCharacter c in Characters) c.OnMatchReady(Match, this, Opponent);
        }

        /// <summary>
        /// Gets called when a character of this player has completed their action.
        /// </summary>
        public virtual void OnActionDone(CharacterAction action) { }

        public virtual void OnStartTurn()
        {
            TurnEnded = false;
            foreach (CtfCharacter c in Characters) c.OnStartTurn();
        }

        public BlockmapNode GetNextJailPosition()
        {
            BlockmapNode node = JailPositions[CurrentJailPositionIndex];
            CurrentJailPositionIndex++;
            if (CurrentJailPositionIndex == JailPositions.Count) CurrentJailPositionIndex = 0;
            return node;
        }

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
            foreach(CtfCharacter character in Match.Characters)
            {
                if (character.IsInAction && character.CurrentAction is Action_Movement otherMove && otherMove.Target == move.Target) return false;
            }

            return true;
        }

        /// <summary>
        /// Gets called when dev mode gets activated or deactivated.
        /// </summary>
        public void OnSetDevMode(bool active)
        {
            if (active)
            {
                SetDevModeLabels();
            }
            else
            {
                foreach (CtfCharacter c in Characters) c.UI_Label.Init(c);
            }
        }

        /// <summary>
        /// Sets the visible label of all characters according to their role and job to easily debug what they are doing.
        /// </summary>
        protected virtual void SetDevModeLabels()
        {
            foreach (CtfCharacter c in Characters)
            {
                c.UI_Label.SetLabelText($"{c.LabelCap} ({c.Id})");
            }
        }

        #endregion
    }
}

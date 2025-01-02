using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CaptureTheFlag
{
    public class Player
    {
        private ClientInfo ClientInfo;

        public string Name => ClientInfo.DisplayName;
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
        public string ClientId => ClientInfo.ClientId;
        public bool ReadyToStartMultiplayerMatch;
        public bool TurnEnded;

        public Player(ClientInfo info)
        {
            ClientInfo = info;
            Debug.Log($"Adding player with Name {Name} and ClientId {ClientId}.");
        }
        public void OnWorldGenerationDone(Actor actor, Zone territory, Zone jailZone, Zone flagZone)
        {
            Actor = actor;

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
            foreach (BlockmapNode n in JailPositions) log += n.Id + "/";
            Debug.Log($"Jail Positions for {Name}: {log}");

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

        public virtual void OnCharacterGotSentToJail(CtfCharacter c) { }

        public virtual void OnCharacterGotReleasedFromJail(CtfCharacter c) { }

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
        /// Gets called when dev mode gets activated or deactivated.
        /// </summary>
        public void OnSetDevMode(bool active)
        {
            foreach (CtfCharacter c in Characters) c.RefreshLabelText();
        }

        #endregion
    }
}

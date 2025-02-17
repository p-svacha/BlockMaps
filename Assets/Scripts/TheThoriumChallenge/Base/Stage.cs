using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheThoriumChallenge
{
    public class Stage
    {
        public Game Game { get; private set; }
        public World World { get; private set; }
        public Vector2Int EntryPoint;
        public Dictionary<Vector2Int, Biome> ExitPoints;
        public TimeStamp GlobalSimulationTime { get; private set; }
        public Actor LocalPlayer { get; private set; }

        public List<Creature> Creatures;
        private PriorityQueue<Creature> ActionQueue;

        // Current turn
        private Creature ActiveTurnCreature;
        private Ability ActiveTargetChoosingAbility;
        private HashSet<BlockmapNode> PossibleTargetNodes;
        private HashSet<BlockmapNode> HighlightedNodes = new();
        private HashSet<BlockmapNode> CurrentImpactNodes;
        private BlockmapNode HoveredTargetNode;

        public Stage(Game game, World world, Vector2Int entryPoint, Dictionary<Vector2Int, Biome> exitPoints)
        {
            Game = game;
            World = world;
            EntryPoint = entryPoint;
            ExitPoints = exitPoints;

            LocalPlayer = World.GetActor(1);

            // Fill entity lists
            Creatures = new List<Creature>();
            foreach (Entity e in World.GetAllEntities())
            {
                if (e is Creature creature)
                {
                    Creatures.Add(creature);
                }
            }

            // Display settings
            World.DisplaySettings.ShowTextures(true);
            World.DisplaySettings.ShowTileBlending(true);
            World.DisplaySettings.ShowGrid(false);
        }

        public void Start()
        {
            // Camera
            BlockmapCamera.Instance.SetAngle(0);
            BlockmapCamera.Instance.SetZoom(10);
            World.CameraJumpToFocusNode(World.GetGroundNode(EntryPoint));

            // Vision
            World.SetActiveVisionActor(LocalPlayer);

            // Action queue
            ActionQueue = new PriorityQueue<Creature>();
            foreach (Creature e in Creatures)
            {
                ActionQueue.Enqueue(e, e.NextActionTime.ValueInSeconds);
            }

            // Time
            GlobalSimulationTime = new TimeStamp();

            // Start first turn
            StartNextTurn();
        }

        public void Tick()
        {
            // Go to next turn
            if (!ActiveTurnCreature.IsInTurn)
            {
                ActionQueue.Enqueue(ActiveTurnCreature, ActiveTurnCreature.NextActionTime.ValueInSeconds);
                StartNextTurn();
            }
        }

        

        #region Turn Loop

        private void StartNextTurn()
        {
            
            SetGlobalSimulationTime(ActionQueue.ToSortedList()[0].NextActionTime.ValueInSeconds);
            Game.UI.ActionTimeline.Refresh(ActionQueue);
            ActiveTurnCreature = ActionQueue.Dequeue();
            ActiveTurnCreature.IsInTurn = true;

            ActiveTurnCreature.RefreshPossibleActions();

            if (ActiveTurnCreature.IsPlayerControlled)
            {
                World.CameraPanToFocusEntity(ActiveTurnCreature, duration: 0.6f, followAfterPan: false);
                Game.UI.ShowCreatureInfo(ActiveTurnCreature);
                Game.UI.ShowCreatureActionSelection(ActiveTurnCreature);
                ActiveTurnCreature.PerformNextAction();
            }
            else
            {
                Game.UI.HideCreatureInfo();
                Game.UI.HidereatureActionSelection();
                if (ActiveTurnCreature.IsVisible) World.CameraPanToFocusEntity(ActiveTurnCreature, duration: 0.6f, followAfterPan: false, callback: () => ActiveTurnCreature.PerformNextAction());
                else ActiveTurnCreature.PerformNextAction();
            }
        }

        private void SetGlobalSimulationTime(int secondsAbsolute)
        {
            GlobalSimulationTime.SetTime(secondsAbsolute);
            Game.UI.RefreshTimeText();
        }


        #endregion

        #region Player Inputs

        public void HandleInputs()
        {
            if (ActiveTurnCreature.IsPlayerControlled && !ActiveTurnCreature.IsInAction)
            {
                // Quick movement with arrow keys
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 180);
                    BlockmapNode target = World.GetAdjacentGroundNode(ActiveTurnCreature.OriginNode, dir);

                    TurnAction moveAction = ActiveTurnCreature.PossibleActions.FirstOrDefault(a => a is TurnAction_UseAbility abilityAction && abilityAction.Ability.Def.DefName == "Move" && abilityAction.Target == target);
                    if (moveAction != null) PerformTurnAction(moveAction);

                }
                else if (Input.GetKey(KeyCode.LeftArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 90);
                    BlockmapNode target = World.GetAdjacentGroundNode(ActiveTurnCreature.OriginNode, dir);

                    TurnAction moveAction = ActiveTurnCreature.PossibleActions.FirstOrDefault(a => a is TurnAction_UseAbility abilityAction && abilityAction.Ability.Def.DefName == "Move" && abilityAction.Target == target);
                    if (moveAction != null) PerformTurnAction(moveAction);
                }
                else if (Input.GetKey(KeyCode.DownArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 0);
                    BlockmapNode target = World.GetAdjacentGroundNode(ActiveTurnCreature.OriginNode, dir);

                    TurnAction moveAction = ActiveTurnCreature.PossibleActions.FirstOrDefault(a => a is TurnAction_UseAbility abilityAction && abilityAction.Ability.Def.DefName == "Move" && abilityAction.Target == target);
                    if (moveAction != null) PerformTurnAction(moveAction);
                }
                else if (Input.GetKey(KeyCode.RightArrow))
                {
                    Direction dir = HelperFunctions.GetDirection4FromAngle(BlockmapCamera.Instance.CurrentAngle, offset: 270);
                    BlockmapNode target = World.GetAdjacentGroundNode(ActiveTurnCreature.OriginNode, dir);

                    TurnAction moveAction = ActiveTurnCreature.PossibleActions.FirstOrDefault(a => a is TurnAction_UseAbility abilityAction && abilityAction.Ability.Def.DefName == "Move" && abilityAction.Target == target);
                    if (moveAction != null) PerformTurnAction(moveAction);
                }

                // Hovered target
                if(ActiveTargetChoosingAbility != null)
                {
                    if (PossibleTargetNodes.Contains(World.HoveredNode))
                    {
                        SetHoveredTargetNode(World.HoveredNode);
                        if (Input.GetMouseButtonDown(0))
                        {
                            TurnAction abilityAction = ActiveTurnCreature.PossibleActions.First(a => a is TurnAction_UseAbility abilityAction && abilityAction.Ability == ActiveTargetChoosingAbility && abilityAction.Target == World.HoveredNode);
                            PerformTurnAction(abilityAction);
                        }
                    }
                    else
                    {
                        SetHoveredTargetNode(null);
                    }
                }
            }
        }

        /// <summary>
        /// Switches to the mode where the player has to choose a node as the target.
        /// </summary>
        public void GoToChooseTargetMode(Ability ability)
        {
            // Leave mode if reselecting ability we are currently in
            if (ActiveTargetChoosingAbility == ability)
            {
                LeaveChooseTargetMode();
                return;
            }

            ActiveTargetChoosingAbility = ability;
            PossibleTargetNodes = ability.GetPossibleTargets();
            HoveredTargetNode = null;

            GameUI.Instance.ActionSelection.SetSelectedAbility(ability);
            UpdateHighlightedNodes();
        }

        public void LeaveChooseTargetMode()
        {
            ActiveTargetChoosingAbility = null;
            PossibleTargetNodes = null;
            HoveredTargetNode = null;

            GameUI.Instance.ActionSelection.UnsetSelectedAbility();
            UpdateHighlightedNodes();
        }

        public void SetHoveredTargetNode(BlockmapNode target)
        {
            if (HoveredTargetNode == target) return;
            HoveredTargetNode = target;

            if(HoveredTargetNode == null)
            {
                CurrentImpactNodes = null;
                GameUI.Instance.ActionSelection.UnsetHighlightedTarget();
            }
            else
            {
                CurrentImpactNodes = ActiveTargetChoosingAbility.GetImpactedNodes(target);
                GameUI.Instance.ActionSelection.SetHighlightedTarget(target);
            }
            
            UpdateHighlightedNodes();
        }

        private void UpdateHighlightedNodes()
        {
            UnhighlightNodes();

            if (PossibleTargetNodes != null) HighlightPossibleTargetNodes();
            if (HoveredTargetNode != null)
            {
                HighlightSelectedTargetNode();
                HighlightImpactNodes();
            }
        }
        private void HighlightPossibleTargetNodes() => HighlightNodes(PossibleTargetNodes, MultiOverlayColor.Green);
        private void HighlightSelectedTargetNode() => HighlightNodes(new HashSet<BlockmapNode>() { HoveredTargetNode }, MultiOverlayColor.Yellow);
        private void HighlightImpactNodes() => HighlightNodes(CurrentImpactNodes, MultiOverlayColor.Yellow);
        private void HighlightNodes(HashSet<BlockmapNode> nodesToHighlight, MultiOverlayColor color)
        {
            HashSet<BlockmapNode> addedNodes = new HashSet<BlockmapNode>();
            foreach (BlockmapNode node in nodesToHighlight)
            {
                addedNodes.Add(node);
                node.ShowMultiOverlay(Game.TileOverlay, color);
                if (node is GroundNode surfaceNode && surfaceNode.WaterNode != null) // Also highlight waternodes on top of surface nodes
                {
                    surfaceNode.WaterNode.ShowMultiOverlay(Game.TileOverlay, color);
                    addedNodes.Add(surfaceNode.WaterNode);
                }
            }

            foreach (BlockmapNode node in addedNodes) HighlightedNodes.Add(node);
        }
        private void UnhighlightNodes()
        {
            foreach (BlockmapNode node in HighlightedNodes) node.HideMultiOverlay();
            HighlightedNodes.Clear();
        }

        public void PerformTurnAction(TurnAction action)
        {
            HoveredTargetNode = null;
            PossibleTargetNodes = null;
            ActiveTargetChoosingAbility = null;
            UpdateHighlightedNodes();

            action.StartPerform();
        }

        /// <summary>
        /// Makes the ActiveTurnCreature perform the Do Nothing action.
        /// </summary>
        public void DoNothing()
        {
            PerformTurnAction(ActiveTurnCreature.PossibleActions.First(a => a is TurnAction_DoNothing));
        }

        #endregion

    }
}

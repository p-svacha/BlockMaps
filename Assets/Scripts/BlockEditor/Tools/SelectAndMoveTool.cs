using BlockmapFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

namespace WorldEditor
{
    public class SelectAndMoveTool : EditorTool
    {
        public override EditorToolId Id => EditorToolId.SelectAndMove;
        public override string Name => "Select & Move";
        public override Sprite Icon => ResourceManager.LoadSprite(IconBasePath + "SelectAndMove");

        private MovingEntity SelectedEntity;

        private const float PATH_PREVIEW_WIDTH = 0.1f;
        private Color PATH_PREVIEW_COLOR = new Color(1f, 1f, 1f, 0.5f);
        private NavigationPath TargetPath;
        private LineRenderer PathPreview;

        [Header("Elements")]
        public TMP_InputField HoveredPathCostInput;

        public GameObject SelectedEntityPanel;
        public TextMeshProUGUI SelectedEntity_NameText;
        public TextMeshProUGUI SelectedEntity_OwnerText;
        public GameObject SelectedEntity_InventoryContainer;

        [Header("Prefabs")]
        public UI_EntityInventoryItem InventoryItemPrefab;

        // Cache
        private BlockmapNode CacheOriginNode;
        private Dictionary<BlockmapNode, NavigationPath> PathCache = new Dictionary<BlockmapNode, NavigationPath>();

        public override void Init(BlockEditor editor)
        {
            base.Init(editor);

            GameObject pathPreviewObject = new GameObject("PathPreview");
            PathPreview = pathPreviewObject.AddComponent<LineRenderer>();
            PathPreview.gameObject.SetActive(false);
        }

        public void SelectEntity(MovingEntity e)
        {
            if (SelectedEntity != null) SelectedEntity.ShowSelectionIndicator(false);
            SelectedEntity = e;
            if(SelectedEntity != null) SelectedEntity.ShowSelectionIndicator(true);

            World.SetNavmeshEntity(SelectedEntity);

            // UI
            RefreshSelectedEntityPanel();
        }

        public void RefreshSelectedEntityPanel()
        {
            SelectedEntityPanel.SetActive(SelectedEntity != null);
            if (SelectedEntity != null)
            {
                // General info
                SelectedEntity_NameText.text = SelectedEntity.LabelCap;
                SelectedEntity_OwnerText.text = SelectedEntity.Actor.Label;

                // Inventory
                HelperFunctions.DestroyAllChildredImmediately(SelectedEntity_InventoryContainer, skipElements: 1);
                foreach (Entity inventoryItem in SelectedEntity.Inventory)
                {
                    UI_EntityInventoryItem display = GameObject.Instantiate(InventoryItemPrefab, SelectedEntity_InventoryContainer.transform);
                    display.Init(this, inventoryItem);
                }
            }
        }

        public override void UpdateTool()
        {
            UpdatePathPreview();
        }

        public override void HandleKeyboardInputs()
        {
            // R - Update vision with debug rays in editor
            if (Input.GetKeyDown(KeyCode.R))
            {
                if (SelectedEntity != null) SelectedEntity.UpdateVision(debugVisionRays: true);
            }

            // P - Pick up all pickup-able entities on same node
            if (Input.GetKeyDown(KeyCode.P))
            {
                if (SelectedEntity != null)
                {
                    List<Entity> entitesToPickUp = SelectedEntity.OriginNode.Entities.Where(e => e.CanBeHeldByOtherEntities).ToList();
                    foreach (Entity entityToPickUp in entitesToPickUp) World.AddToInventory(entityToPickUp, SelectedEntity, updateWorld: true);
                    RefreshSelectedEntityPanel();
                }
            }
        }

        private void UpdatePathPreview()
        {
            TargetPath = null;
            PathPreview.gameObject.SetActive(false);
            HoveredPathCostInput.text = "";

            if (SelectedEntity == null) return;
            if (World.HoveredNode == null) return;

            if (SelectedEntity.OriginNode != CacheOriginNode) PathCache.Clear(); // Clear cache when changing node
            CacheOriginNode = SelectedEntity.OriginNode;

            if(PathCache.TryGetValue(World.HoveredNode, out NavigationPath cachedPath)) // A path to the hovered node is cached 
            {
                TargetPath = cachedPath == null ? null : new NavigationPath(cachedPath);
            }
            else // A path to the hovered node is not yet cached
            {
                TargetPath = Pathfinder.GetPath(SelectedEntity, SelectedEntity.OriginNode, World.HoveredNode);
                PathCache.Add(World.HoveredNode, TargetPath == null ? null : new NavigationPath(TargetPath)); // add to cache
            }

            if (TargetPath == null) return;

            PathPreview.gameObject.SetActive(true);
            Pathfinder.ShowPathPreview(PathPreview, TargetPath, PATH_PREVIEW_WIDTH, PATH_PREVIEW_COLOR);
            HoveredPathCostInput.text = TargetPath.GetCost(SelectedEntity).ToString("0.##");
        }

        public override void HandleLeftClick()
        {
            // De-select entity
            SelectEntity(null);

            // Select entity
            if (World.HoveredEntity != null)
            {
                if (World.HoveredEntity is MovingEntity movingEntity)
                {
                    SelectEntity(movingEntity);
                }
            }
        }

        // Rightclick: Move
        public override void HandleRightClick()
        {
            if (SelectedEntity != null && TargetPath != null) SelectedEntity.GetComponent<Comp_Movement>().MoveTo(World.HoveredNode);
        }

        // Middleclick: Teleport
        public override void HandleMiddleClick()
        {
            if (SelectedEntity == null) return;
            if (World.HoveredNode == null) return;
            if (!World.HoveredNode.IsPassable(SelectedEntity)) return;

            SelectedEntity.Teleport(World.HoveredNode);
        }

        public override void OnSelect()
        {
            PathCache.Clear();
            SelectEntity(null);
        }
        public override void OnDeselect()
        {
            SelectEntity(null);
            PathPreview.gameObject.SetActive(false);
        }
    }
}

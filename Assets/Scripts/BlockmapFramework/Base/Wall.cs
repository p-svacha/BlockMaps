using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// <br/>An instance represents one wall on a single world cell covering a specific direction (side, corner or full cell).
    /// <br/>Each wall consist of a combination of a WallShape and a WallMaterial, which together define how the wall looks and acts.
    /// <br/>Walls exist on their own seperate 3d grid and are not bound to nodes.
    /// </summary>
    public class Wall : WorldDatabaseObject, IClimbable, IVisionTarget, ISaveAndLoadable
    {
        private World World;

        private int id;
        public override int Id => id;

        /// <summary>
        /// The world cell coordiantes of the wall.
        /// </summary>
        public Vector3Int GlobalCellCoordinates;
        public int MinAltitude => GlobalCellCoordinates.y;
        public int MaxAltitude => MinAltitude;
        public Vector3Int LocalCellCoordinates => World.GetLocalCellCoordinates(GlobalCellCoordinates);
        public Vector2Int WorldCoordinates { get; private set; }
        public Chunk Chunk => World.GetChunk(WorldCoordinates);
        public Vector3 CellCenterWorldPosition => new Vector3(GlobalCellCoordinates.x + 0.5f, (GlobalCellCoordinates.y * World.NodeHeight) + (World.NodeHeight / 2), GlobalCellCoordinates.z + 0.5f);

        /// <summary>
        /// The side within the cell this wall covers.
        /// </summary>
        public Direction Side;

        /// <summary>
        /// The shape of this wall (i.e. solid, window).
        /// </summary>
        public WallShapeDef Shape;

        /// <summary>
        /// The material this wall is made of.
        /// </summary>
        public WallMaterialDef Material;

        /// <summary>
        /// Flag if this is the mirrored version of the wall piece.
        /// </summary>
        public bool IsMirrored;

        /// <summary>
        /// Zones that this wall is inside of
        /// </summary>
        public List<Zone> Zones = new List<Zone>();

        public float Width => Shape.Width;

        // GameObject
        public WallMesh Mesh { get; private set; }
        public GameObject VisionColliderObject;


        // IClimbable
        public ClimbingCategory ClimbSkillRequirement => Material.ClimbSkillRequirement;
        public float ClimbCostUp => Material.ClimbCostUp;
        public float ClimbCostDown => Material.ClimbCostDown;
        public float ClimbTransformOffset => Shape.Width;
        public Direction ClimbSide => Side;
        public bool IsClimbable => Shape.IsClimbable && ClimbSkillRequirement != ClimbingCategory.Unclimbable;

        #region Init

        public Wall() { }
        public Wall(World world, int id, Vector3Int globalCellCoordinates, Direction side, WallShapeDef shape, WallMaterialDef material, bool mirrored)
        {
            World = world;
            this.id = id;
            GlobalCellCoordinates = globalCellCoordinates;
            WorldCoordinates = new Vector2Int(GlobalCellCoordinates.x, GlobalCellCoordinates.z);
            Side = side;
            Shape = shape;
            Material = material;
            IsMirrored = mirrored;

            if ((Shape.IsCornerShape && HelperFunctions.IsSide(Side)) || !Shape.IsCornerShape && HelperFunctions.IsCorner(Side)) throw new System.Exception("Invalid wall side error. " + Shape.Label + " is not allowed to build in the direction " + Side.ToString());

            Init();
        }

        public override void PostLoad()
        {
            WorldCoordinates = new Vector2Int(GlobalCellCoordinates.x, GlobalCellCoordinates.z);
            World.RegisterWall(this, registerInWorld: false);

            Init();
        }

        /// <summary>
        /// Gets called after this Wall got instantiated, either through being spawned or when being loaded.
        /// </summary>
        private void Init()
        {
            CreateVisionCollider();
        }

        private void CreateVisionCollider()
        {
            VisionColliderObject = new GameObject("visionCollider_" + Shape.Label + "_" + Id);
            VisionColliderObject.layer = World.Layer_WallVisionCollider;
            VisionColliderObject.transform.SetParent(Chunk.ChunkObject.transform);

            BoxCollider collider = VisionColliderObject.AddComponent<BoxCollider>();
            collider.size = GetVisionColliderSize();
            collider.center = GetVisionColliderCenter();

            WorldObjectCollider evc = VisionColliderObject.AddComponent<WorldObjectCollider>();
            evc.Object = this;
        }

        private Vector3 GetVisionColliderSize()
        {
            if(Side == Direction.E || Side == Direction.W) return new Vector3(Shape.Width, World.NodeHeight, 1f);
            else if(Side == Direction.S || Side == Direction.N) return new Vector3(1f, World.NodeHeight, Shape.Width);
            else return new Vector3(Shape.Width, World.NodeHeight, Shape.Width);
        }

        private Vector3 GetVisionColliderCenter()
        {
            Vector3 centerPos = new Vector3(LocalCellCoordinates.x + 0.5f, (LocalCellCoordinates.y * World.NodeHeight) + (World.NodeHeight * 0.5f), LocalCellCoordinates.z + 0.5f);
            centerPos += Chunk.ChunkObject.transform.position;

            if (Side == Direction.W) return centerPos + new Vector3(-0.5f + (Shape.Width * 0.5f), 0f, 0f);
            if (Side == Direction.E) return centerPos + new Vector3(0.5f - (Shape.Width * 0.5f), 0f, 0f);
            if (Side == Direction.S) return centerPos + new Vector3(0f, 0f, -0.5f + (Shape.Width * 0.5f));
            if (Side == Direction.N) return centerPos + new Vector3(0f, 0f, 0.5f - (Shape.Width * 0.5f));

            if (Side == Direction.SE) return centerPos + new Vector3(0.5f - (Shape.Width * 0.5f), 0f, -0.5f + (Shape.Width * 0.5f));
            if (Side == Direction.SW) return centerPos + new Vector3(-0.5f + (Shape.Width * 0.5f), 0f, -0.5f + (Shape.Width * 0.5f));
            if (Side == Direction.NE) return centerPos + new Vector3(0.5f - (Shape.Width * 0.5f), 0f, 0.5f - (Shape.Width * 0.5f));
            if (Side == Direction.NW) return centerPos + new Vector3(-0.5f + (Shape.Width * 0.5f), 0f, 0.5f - (Shape.Width * 0.5f));

            return centerPos;
        }

        #endregion

        #region Actions

        public void SetMesh(WallMesh mesh)
        {
            Mesh = mesh;
        }

        public void AddZone(Zone z)
        {
            Zones.Add(z);

            // Explore walls for zone owner if zone provides vision
            if (z.ProvidesVision) AddExploredBy(z.Actor);
        }
        public void RemoveZone(Zone z)
        {
            Zones.Remove(z);
        }

        #endregion

        #region Vision Target

        private HashSet<Actor> ExploredBy = new HashSet<Actor>();
        private HashSet<Entity> SeenBy = new HashSet<Entity>();

        public void AddVisionBy(Entity e)
        {
            ExploredBy.Add(e.Actor);
            SeenBy.Add(e);
        }
        public void RemoveVisionBy(Entity e)
        {
            SeenBy.Remove(e);
        }
        public void AddExploredBy(Actor p)
        {
            ExploredBy.Add(p);
        }
        public void RemoveExploredBy(Actor p)
        {
            ExploredBy.Remove(p);
        }

        public bool IsVisibleBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            if (Zones.Any(x => x.ProvidesVision && x.Actor == actor)) return true; // Node is in a zone of actor that provides vision
            if (SeenBy.FirstOrDefault(x => x.Actor == actor) != null) return true; // Wall is seen by an entity of given actor

            return false;
        }
        public bool IsExploredBy(Actor actor)
        {
            if (actor == null) return true; // Everything is visible
            return ExploredBy.Contains(actor);
        }

        #endregion

        #region Getters
        public virtual string Label => Material.LabelCap + " " + Shape.LabelCap;
        public virtual string Description => Shape.Description;

        public Vector3 GetCenterWorldPosition()
        {
            Vector3 cellCenter = CellCenterWorldPosition;

            Vector3 directionOffset = Side switch
            {
                Direction.N => new Vector3(0f, 0f, 0.5f - (Width/2)),
                Direction.E => new Vector3(0.5f - (Width / 2), 0f, 0f),
                Direction.S => new Vector3(0f, 0f, -(0.5f - (Width / 2))),
                Direction.W => new Vector3(-(0.5f - (Width / 2)), 0f, 0f),
                Direction.NW => new Vector3(-(0.5f - (Width / 2)), 0f, 0.5f - (Width / 2)),
                Direction.NE => new Vector3(0.5f - (Width / 2), 0f, 0.5f - (Width / 2)),
                Direction.SW => new Vector3(-(0.5f - (Width / 2)), 0f, -(0.5f - (Width / 2))),
                Direction.SE => new Vector3(0.5f - (Width / 2), 0f, -(0.5f - (Width / 2))),
                _ => throw new System.Exception("Direction not handled")
            };

            return cellCenter + directionOffset;
        }

        public override string ToString()
        {
            return GlobalCellCoordinates.ToString() + " " + Side.ToString() + " " + Shape.Label + " " + Material.Label;
        }

        public bool BlocksVision => Shape.BlocksVision;

        #endregion

        #region Save / Load

        public virtual void ExposeDataForSaveAndLoad()
        {
            if (SaveLoadManager.IsLoading) World = SaveLoadManager.LoadingWorld;

            SaveLoadManager.SaveOrLoadPrimitive(ref id, "id");
            SaveLoadManager.SaveOrLoadDef(ref Material, "material");
            SaveLoadManager.SaveOrLoadDef(ref Shape, "shape");
            SaveLoadManager.SaveOrLoadVector3Int(ref GlobalCellCoordinates, "cell");
            SaveLoadManager.SaveOrLoadPrimitive(ref Side, "side");
            SaveLoadManager.SaveOrLoadPrimitive(ref IsMirrored, "isMirrored");
        }

        #endregion
    }
}

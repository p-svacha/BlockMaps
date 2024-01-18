using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    public class NavmeshVisualizer : MonoBehaviour
    {
        public GameObject ArrowPrefab;

        private const float NODE_SCALE = 0.3f;
        private const float ARROW_SCALE = 0.05f;
        private const float ARROW_HEIGHT = 0.2f;


        /// <summary>
        /// Shows the navmesh of the world.
        /// <br/> If an entity is provided the navmesh will be visualized for that entity.
        /// </summary>
        public void Visualize(World world, MovingEntity entity = null)
        {
            ClearVisualization();

            foreach (Chunk chunk in world.Chunks.Values)
            {
                foreach (BlockmapNode node in chunk.GetAllNodes())
                {
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.transform.SetParent(transform);
                    sphere.transform.localPosition = node.GetCenterWorldPosition();
                    sphere.transform.localScale = new Vector3(NODE_SCALE, NODE_SCALE, NODE_SCALE);
                    if (node.Type == NodeType.Surface) sphere.GetComponent<MeshRenderer>().material.color = Color.green;
                    if (node.Type == NodeType.AirPath) sphere.GetComponent<MeshRenderer>().material.color = Color.blue;
                    if (node.Type == NodeType.Water) sphere.GetComponent<MeshRenderer>().material.color = Color.cyan;

                    foreach (Transition t in node.Transitions.Values)
                    {
                        if (entity != null && !t.CanPass(entity)) continue;

                        GameObject arrow = Instantiate(ArrowPrefab, transform);
                        arrow.transform.localPosition = node.GetCenterWorldPosition();
                        arrow.transform.localScale = new Vector3(ARROW_SCALE, ARROW_SCALE, 0.3f * Vector3.Distance(node.GetCenterWorldPosition(), t.To.GetCenterWorldPosition()));
                        arrow.transform.LookAt(t.To.GetCenterWorldPosition());
                        arrow.transform.rotation = Quaternion.Euler(-arrow.transform.rotation.eulerAngles.x, 180 + arrow.transform.rotation.eulerAngles.y, arrow.transform.rotation.eulerAngles.z);
                        arrow.transform.position += new Vector3(0f, ARROW_HEIGHT, 0f);
                    }
                }
            }
        }

        public void ClearVisualization()
        {
            foreach (Transform t in transform) Destroy(t.gameObject);
        }

        public static NavmeshVisualizer Singleton { get { return GameObject.Find("NavmeshVisualizer").GetComponent<NavmeshVisualizer>(); } }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Game
{
    [Serializable]
    public struct DataCubeEntry
    {
        public int Index;
        public string Value;
    }

    public class Carpet : MonoBehaviour, IResettable
    {
        [Header("References")]
        public Transform Player;
        public GameObject Frame;
        public Material FrameMaterial;
        public Transform CellPivot;
        public GameObject CellPrefab;
        public Transform DataCubes;

        [Header("Carpet")]
        public float CellSize = 2f;
        public float EdgeExtra = .25f;

        [SerializeField, HideInInspector] private List<DataCube> m_DataCubes = new();
        [SerializeField, HideInInspector] private List<GameObject> m_Cells = new();

        [SerializeField, HideInInspector] private int m_Row;
        [SerializeField, HideInInspector] private int m_Col;

        public int Row => m_Row;
        public int Col => m_Col;
        public int Size => m_Cells.Count;

        /// <summary>
        /// 离玩家最近的DataCube，用于在Player脚下时调整渲染层级，避免被Carpet遮挡
        /// </summary>
        private DataCube m_PlayerSnapCube;

        /// <summary>
        /// 游戏开始时DataCubes的快照，用于Reset时恢复
        /// </summary>
        private List<DataCubeEntry> m_Snapshots;

        public void InitializeCarpet(int row, int col)
        {
            for (int i = CellPivot.childCount - 1; i >= 0; i--)
            {
                GameObject cell = CellPivot.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(cell);
                else DestroyImmediate(cell);
            }
            m_Cells.Clear();

            for (int i = m_DataCubes.Count - 1; i >= 0; i--)
            {
                if (m_DataCubes[i] != null)
                {
                    if (Application.isPlaying) Destroy(m_DataCubes[i].gameObject);
                    else DestroyImmediate(m_DataCubes[i].gameObject);
                }
            }
            m_DataCubes.Clear();
            m_Snapshots?.Clear();

            m_Row = row;
            m_Col = col;

            if (m_Row <= 0 || m_Col <= 0)
            {
                Frame.GetComponent<MeshFilter>().mesh = null;
                Frame.SetActive(false);
                return;
            }

            Frame.SetActive(true);

            // Mesh
            Mesh mesh = GenerateRectMesh(m_Row, m_Col);
            MeshFilter meshFilter = Frame.GetComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            // BoxCollider
            BoxCollider box = Frame.GetComponent<BoxCollider>();
            box.center = meshFilter.sharedMesh.bounds.center;
            box.size = meshFilter.sharedMesh.bounds.size;

            // Material
            MeshRenderer renderer = Frame.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = FrameMaterial;
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetFloat("_Seed", UnityEngine.Random.Range(0.0f, 1000f));
            renderer.SetPropertyBlock(mpb);

            // Cells
            float offsetX = (-col + 1f) * CellSize / 2f;
            float offsetY = (row - 1f) * CellSize / 2f;

            for (int i = 0; i < row; i++)
            {
                for (int j = 0; j < col; j++)
                {
                    int idx = i * col + j;
                    GameObject cell = Instantiate(CellPrefab, CellPivot);
                    m_Cells.Add(cell);

                    cell.name = $"Cell_{idx}";
                    cell.transform.localPosition = new Vector3(j * CellSize + offsetX, i * -CellSize + offsetY, 0f);
                    cell.transform.localScale = Vector3.one * CellSize / 2f;

                    cell.transform.Find("Index").GetComponent<TextMeshPro>().text = idx.ToString();

                    GameObject quad = cell.transform.Find("Quad").gameObject;
                    quad.SetActive((i + j) % 2 == 0);
                    quad.transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-3f, 3f));
                }
            }

            m_DataCubes = Enumerable.Repeat<DataCube>(null, row * col).ToList();
        }

        public void SpawnDataCube(int idx, string value)
        {
            if (idx < 0 || idx >= m_DataCubes.Count) 
            {
                Debug.LogError("[Carpet] 无效索引！");
                return;
            }

            DataCube cube = Instantiate(GameManager.Ins.DataCubePrefab, DataCubes);
            cube.SetValue(value);
            Append(cube, idx);
        }

        private void Awake()
        {
            GameManager.RegisterResettable(this);
        }

        public void CacheCurrDataCubes()
        {
            m_Snapshots = new List<DataCubeEntry>();
            if (m_DataCubes.Count == 0) return;

            for (int i = 0; i < m_DataCubes.Count; i++)
                if (m_DataCubes[i] != null)
                    m_Snapshots.Add(new DataCubeEntry { Index = i, Value = m_DataCubes[i].Value });
        }

        public void OnReset()
        {
            if (m_DataCubes.Count == 0) return;

            for (int i = m_DataCubes.Count - 1; i >= 0; i--) RemoveAndDestory(i);

            foreach (var snap in m_Snapshots) SpawnDataCube(snap.Index, snap.Value);
        }

        /// <summary>
        /// Player脚下的Cube图层应该在6
        /// </summary>
        private void LateUpdate()
        {
            if (m_DataCubes.Count == 0 || !Frame.activeSelf) return;

            int idx = SnapToCellIndex(Player.position);
            if (idx < 0 || idx >= m_DataCubes.Count) return;

            DataCube cube = GetDataCube(idx);

            if (m_PlayerSnapCube != null && m_PlayerSnapCube != cube && m_DataCubes.IndexOf(m_PlayerSnapCube) != -1) m_PlayerSnapCube.SetRendererSortingOrder(0);

            m_PlayerSnapCube = cube;
            if (cube == null) return;

            if (cube.CurrRenderOreder < 6) cube.SetRendererSortingOrder(6);
        }

        public void Append(DataCube cube, int index)
        {
            if (cube == null)
            {
                Debug.Log("[Carpet] 方块呢？！");
                return;
            }
            if (index < 0 || index >= m_DataCubes.Count) return;

            RemoveAndDestory(index);

            m_DataCubes[index] = cube;
            cube.transform.SetParent(DataCubes);
            cube.transform.position = GetCellCenter(index);
            cube.SetRendererSortingOrder(0);
        }

        public void Remove(int index)
        {
            if (index < 0 || index >= m_DataCubes.Count) return;
            m_DataCubes[index] = null;
        }

        public void RemoveAndDestory(int index)
        {
            if (index < 0 || index >= m_DataCubes.Count) return;
            if (m_DataCubes[index] != null)
            {
                if (Application.isPlaying) Destroy(m_DataCubes[index].gameObject);
                else DestroyImmediate(m_DataCubes[index].gameObject);
            }
            Remove(index);
        }

        public DataCube GetDataCube(int index)
        {
            if (index < 0 || index >= m_DataCubes.Count) return null;
            return m_DataCubes[index];
        }

        /// <summary>
        /// Player在CopyTo或CopyFrom时的Position
        /// </summary>
        public Vector3 GetPlayerDestination(int index)
        {
            if (index < 0 || index >= m_Cells.Count) return Vector3.zero;
            return m_Cells[index].transform.position + Vector3.up * .2f;
        }

        public Vector3 GetCellCenter(int index)
        {
            if (index < 0 || index >= m_Cells.Count) return Vector3.zero;
            return m_Cells[index].transform.position;
        }

        public int SnapToCellIndex(Vector3 pos)
        {
            if (m_Col <= 0 || m_Row <= 0) return -1;

            Vector3 localPos = pos - transform.position;

            float offsetX = (-m_Col + 1f) * CellSize / 2f;
            float offsetY = (m_Row - 1f) * CellSize / 2f;

            int colIndex = Mathf.RoundToInt((localPos.x - offsetX) / CellSize);
            int rowIndex = Mathf.RoundToInt((offsetY - localPos.y) / CellSize);

            colIndex = Mathf.Clamp(colIndex, 0, m_Col - 1);
            rowIndex = Mathf.Clamp(rowIndex, 0, m_Row - 1);

            return rowIndex * m_Col + colIndex;
        }

        public string[] ExportValues()
        {
            string[] values=new string[m_DataCubes.Count];
            for (int i = 0; i < m_DataCubes.Count; i++)
                values[i] = m_DataCubes[i]?.Value;
            return values;
        }

        private Mesh GenerateRectMesh(int row, int col)
        {
            Mesh mesh = new Mesh();
            mesh.name = $"Runtime_Rect_{row}x{col}";

            float totalWidth = CellSize * col + EdgeExtra;
            float totalHeight = CellSize * row + EdgeExtra;

            float centerOffsetX = totalWidth / 2f;
            float centerOffsetY = totalHeight / 2f;

            int vertexRowCount = row * 2 + 1;
            int vertexColCount = col * 2 + 1;
            Vector3[] vertices = new Vector3[vertexRowCount * vertexColCount];
            Vector2[] uv = new Vector2[vertexRowCount * vertexColCount];
            int vertexIndex = 0;

            for (int i = 0; i < vertexRowCount; i++)
            {
                for (int j = 0; j < vertexColCount; j++)
                {
                    float u = (float)j / (vertexColCount - 1);
                    float v = (float)i / (vertexRowCount - 1);

                    float x = u * totalWidth - centerOffsetX;
                    float y = v * totalHeight - centerOffsetY;

                    vertices[vertexIndex] = new Vector3(x, y, 0);
                    uv[vertexIndex] = new Vector2(u, v);
                    vertexIndex++;
                }
            }

            int triangleCount = (vertexRowCount - 1) * (vertexColCount - 1) * 2;
            int[] triangles = new int[triangleCount * 3];
            int triangleIndex = 0;

            for (int i = 0; i < vertexRowCount - 1; i++)
            {
                for (int j = 0; j < vertexColCount - 1; j++)
                {
                    int topLeft = i * vertexColCount + j;
                    int topRight = topLeft + 1;
                    int bottomLeft = (i + 1) * vertexColCount + j;
                    int bottomRight = bottomLeft + 1;

                    triangles[triangleIndex++] = topLeft;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = topRight;

                    triangles[triangleIndex++] = topRight;
                    triangles[triangleIndex++] = bottomLeft;
                    triangles[triangleIndex++] = bottomRight;
                }
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
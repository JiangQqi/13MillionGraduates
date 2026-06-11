using System;
using UnityEngine;
using System.Collections.Generic;

namespace Game
{
    [Serializable]
    public class Area
    {
        public int id { get; private set; }

        public int x1 { get; private set; }
        public int x2 { get; private set; }
        public int y1 { get; private set; }
        public int y2 { get; private set; }

        public Area(int id, int x1, int x2, int y1, int y2)
        {
            this.id = id;
            this.x1 = Mathf.Min(x1, x2);
            this.x2 = Mathf.Max(x1, x2);
            this.y1 = Mathf.Min(y1, y2);
            this.y2 = Mathf.Max(y1, y2);
        }
    }

    public class AreaManager : MonoBehaviour
    {
        [Header("References")]
        public AreaSelectionManager SelectionManager;

        [SerializeField, HideInInspector] private int m_Row = 10;
        [SerializeField, HideInInspector] private int m_Col = 10;

        [SerializeField, HideInInspector] private int[] m_AreaMap;
        [SerializeField, HideInInspector] private List<Area> m_Areas = new List<Area>();//Index by id
        [SerializeField, HideInInspector] private int m_NextAvailableId = 1;

        public int Row => m_Row;
        public int Col => m_Col;
        public int NextAvailableId => m_NextAvailableId;

        public void InitializeCarpetManager(int row, int col)
        {
            m_Row = row;
            m_Col = col;

            m_NextAvailableId = 0;

            //AreaMap
            m_AreaMap = new int[row * col];
            Array.Fill(m_AreaMap, -1);

            //Areas
            m_Areas.Clear();

            //UI
            SelectionManager.Init(row, col);
        }

        public bool CreateArea(Vector3Int p1, Vector3Int p2)
        {
            return CreateArea(p1.x, p2.x, p1.y, p2.y);
        }

        /// <summary>
        /// 新建区域，返回值为是否创建成功
        /// </summary>
        public bool CreateArea(int x1, int x2, int y1, int y2)
        {
            ClampBounds(ref x1, ref x2, ref y1, ref y2);
            if (x1<0 || x2 >= m_Row || y1 < 0 || y2 >= m_Col)
            {
                Debug.LogError("[CarpetManager] 无效索引！");
                return false;
            }
            if (IsAreaOccupied(x1, x2, y1, y2)) 
            {
                Debug.LogError("[CarpetManager] 区域重叠！");
                return false;
            }

            int id = m_NextAvailableId++;

            //AreaMap
            FillAreaMap(x1, x2, y1, y2, id);

            //Areas
            Area area = new Area(id, x1, x2, y1, y2);
            m_Areas.Add(area);

            //UI
            SelectionManager.RegisterArea(area);

            return true;
        }

        public Area RemoveArea(int id)
        {
            if (m_Areas.Count <= id || m_Areas[id] == null) 
            {
                Debug.LogError($"[CarpetManager] 获取索引为 {id} 的Area出现问题！");
                return null;
            }

            Area area = m_Areas[id];
            m_Areas[id] = null;
            FillAreaMap(area.x1, area.x2, area.y1, area.y2, -1);

            return area;
        }

        public bool IsAreaOccupied(Vector3Int p1, Vector3Int p2)
        {
            var (x1, x2, y1, y2) = (p1.x, p2.x, p1.y, p2.y);
            ClampBounds(ref x1, ref x2, ref y1, ref y2);
            return IsAreaOccupied(x1, x2, y1, y2);
        }

        public bool IsAreaOccupied(int x1, int x2, int y1, int y2)
        {
            ClampBounds(ref x1, ref x2, ref y1, ref y2);

            for (int x = x1; x <= x2; x++)
                for (int y = y1; y <= y2; y++)
                    if (m_AreaMap[x * m_Col + y] != -1)
                        return true;
            return false;
        }

        public int GetAreaIdByPos(int x, int y)
        {
            if (!IsPosValid(x, y)) return -1;
            return m_AreaMap[x * m_Col + y];
        }
        public int GetAreaIdByPos(Vector3Int pos) => GetAreaIdByPos(pos.x, pos.y);

        public bool IsPosValid(int x, int y) => x >= 0 && x < m_Row && y >= 0 && y < m_Col;
        public bool IsPosValid(Vector3Int pos) => IsPosValid(pos.x, pos.y);

        private void FillAreaMap(int x1, int x2, int y1, int y2, int id)
        {
            for (int x = x1; x <= x2; x++)
                for (int y = y1; y <= y2; y++)
                    m_AreaMap[x * m_Col + y] = id;
        }

        private void ClampBounds(ref int x1, ref int x2, ref int y1, ref int y2)
        {
            if (x1 > x2) (x1, x2) = (x2, x1);
            if (y1 > y2) (y1, y2) = (y2, y1);
        }
    }
}
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
    public class AreaSelectionManager : MonoBehaviour
    {
        private enum SelectionState
        {
            Idle,
            Creating,
            Editing,
            Coloring,
        }

        [Header("References")]
        public AreaManager AreaManager;
        public Transform Grid;
        public Tilemap Tilemap;

        [Header("Resources")]
        public Tile DotLine;
        public List<Tile> Frames;

        [Header("HSV Color")]
        public float Saturation = .4f;
        public float Value = 1f;
        public Color OccupiedColor = Color.red;

        [Header("Debug")]
        public TextMeshProUGUI text;

        [SerializeField,] private List<Color> m_Colors = new List<Color>();//Index by id

        private SelectionState m_State;
        private Vector3Int m_MousePos;
        private Vector3Int m_CreateStartPos;
        private Vector3Int m_CreateEndPos;
        private int m_ColoringAreaId;

        public void Init(int row, int col)
        {
            Grid.position = new Vector3(-7.5f, -7.5f, 0);
            Tilemap.ClearAllTiles();
            ResetAreaToDefault(0, row-1, 0, col-1);
        }

        private void Start()
        {
            m_State = SelectionState.Idle;

            m_MousePos = Vector3Int.zero;
            m_CreateStartPos = Vector3Int.zero;
            m_CreateEndPos = Vector3Int.zero;
            m_ColoringAreaId = 0;

            _camera = Camera.main;
        }

        private Camera _camera;
        private void Update()
        {
            Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 20f));
            m_MousePos = Tilemap.WorldToCell(mouseWorldPos);
            text.text = m_MousePos.ToString();

            switch(m_State)
            {
                case SelectionState.Idle:
                    if (!AreaManager.IsPosValid(m_MousePos)) break;

                    int id = AreaManager.GetAreaIdByPos(m_MousePos);
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (id == -1)
                        {
                            m_State = SelectionState.Creating;
                            m_CreateStartPos = m_MousePos;
                            m_CreateEndPos = m_MousePos;
                            DrawArea(m_CreateStartPos, m_CreateEndPos, GetDefaultColorById(AreaManager.NextAvailableId));
                        }
                        else
                        {
                            m_State = SelectionState.Coloring;
                            m_ColoringAreaId = id;
                        }
                    }
                    else if (Input.GetMouseButtonDown(1) && id != -1) 
                    {
                        Area area = AreaManager.RemoveArea(id);
                        ResetAreaToDefault(area.x1, area.x2, area.y1, area.y2);
                    }

                    break;

                case SelectionState.Creating:
                    if (!AreaManager.IsPosValid(m_MousePos)) break;

                    if (m_MousePos != m_CreateEndPos)
                    {
                        RefreshArea(m_CreateStartPos, m_CreateEndPos);
                        m_CreateEndPos = m_MousePos;

                        Color color =
                            AreaManager.IsAreaOccupied(m_CreateStartPos, m_CreateEndPos) ?
                            OccupiedColor :
                            GetDefaultColorById(AreaManager.NextAvailableId);
                        DrawArea(m_CreateStartPos, m_CreateEndPos, color);
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (AreaManager.CreateArea(m_CreateStartPos, m_CreateEndPos)) 
                        {
                            m_State = SelectionState.Idle;
                        }
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        RefreshArea(m_CreateStartPos, m_CreateEndPos);
                        m_State = SelectionState.Idle;
                    }
                    break;

                case SelectionState.Editing:
                    if (Input.GetMouseButtonDown(1))
                    {
                        m_State = SelectionState.Idle;
                    }

                    break;

                case SelectionState.Coloring:
                    if (Input.GetMouseButtonDown(1))
                    {
                        m_State = SelectionState.Idle;
                    }

                    break;
            }
        }

        /// <summary>
        /// 新建Area需先按通过该方法生成对应m_Colors值后才可使用RefreshArea
        /// </summary>
        public void RegisterArea(Area area)
        {
            Color color = GetDefaultColorById(area.id);
            m_Colors.Add(color);
            DrawArea(area.x1, area.x2, area.y1, area.y2, color);
        }

        private void DrawArea(Vector3Int p1, Vector3Int p2, Color color)
        {
            var (x1, x2, y1, y2) = (p1.x, p2.x, p1.y, p2.y);
            ClampBounds(ref x1, ref x2, ref y1, ref y2);
            DrawArea(x1, x2, y1, y2, color);
        }

        /// <summary>
        /// 直接绘制Area
        /// </summary>
        private void DrawArea(int x1, int x2, int y1, int y2, Color color)
        {
            //Signle
            if (x1 == x2 && y1 == y2)
            {
                SetTile(x1, y1, Frames[4]);
            }
            //Vertical
            else if (x1 == x2)
            {
                SetTileAndRotation(x1, y1, Frames[3], 180f);
                SetTileAndRotation(x1, y2, Frames[3], 0f);

                for (int i = y1 + 1; i < y2; i++)
                {
                    SetTileAndRotation(x1, i, Frames[2], 0f);
                }
            }
            //Horizontal
            else if (y1 == y2)
            {
                SetTileAndRotation(x1, y1, Frames[3], 90f);
                SetTileAndRotation(x2, y1, Frames[3], 270f);

                for (int i = x1 + 1; i < x2; i++)
                {
                    SetTileAndRotation(i, y1, Frames[2], 90f);
                }
            }
            //Rectangle
            else
            {
                SetTileAndRotation(x1, y2, Frames[1], 0f);
                SetTileAndRotation(x1, y1, Frames[1], 90f);
                SetTileAndRotation(x2, y1, Frames[1], 180f);
                SetTileAndRotation(x2, y2, Frames[1], 270f);

                for (int i = x1 + 1; i < x2; i++)
                {
                    SetTileAndRotation(i, y1, Frames[0], 180f);
                    SetTileAndRotation(i, y2, Frames[0], 0f);
                }

                for (int i = y1 + 1; i < y2; i++)
                {
                    SetTileAndRotation(x1, i, Frames[0], 90f);
                    SetTileAndRotation(x2, i, Frames[0], 270f);
                }

                for (int i = x1 + 1; i < x2; i++)
                {
                    for (int j = y1 + 1; j < y2; j++)
                    {
                        SetTile(i, j, null);
                    }
                }
            }

            //Color
            for (int i = x1; i <= x2; i++) 
            {
                for (int j = y1; j <= y2; j++) 
                {
                    SetColor(i, j, color);
                }
            }
        }

        private void RefreshArea(Vector3Int p1, Vector3Int p2)
        {
            var (x1, x2, y1, y2) = (p1.x, p2.x, p1.y, p2.y);
            ClampBounds(ref x1, ref x2, ref y1, ref y2);
            RefreshArea(x1, x2, y1, y2);
        }

        /// <summary>
        /// 依据AreaMap重绘
        /// </summary>
        private void RefreshArea(int x1, int x2, int y1, int y2)
        {
            for (int i = x1; i <= x2; i++) 
            {
                for (int j = y1; j <= y2; j++) 
                {
                    int id = AreaManager.GetAreaIdByPos(i, j);

                    if (id == -1)
                    {
                        SetTile(i, j, DotLine);
                        SetColor(i, j, Color.white);
                    }
                    else
                    {
                        bool up = id == AreaManager.GetAreaIdByPos(i, j+1);
                        bool down = id == AreaManager.GetAreaIdByPos(i, j - 1);
                        bool left = id == AreaManager.GetAreaIdByPos(i - 1, j);
                        bool right = id == AreaManager.GetAreaIdByPos(i + 1, j);

                        var (tile, rotZ) = DetermineTileAndRotation(up, down, left, right);
                        SetTileAndRotation(i, j, tile, rotZ);
                        SetColor(i, j, m_Colors[id]);
                    }
                }
            }
        }

        private (Tile tile, float rotZ) DetermineTileAndRotation(bool up, bool down, bool left, bool right)
        {
            int mask = (up ? 1 << 3 : 0) |
                  (down ? 1 << 2 : 0) |
                  (left ? 1 << 1 : 0) |
                  (right ? 1 << 0 : 0);

            switch (mask)
            {
                //Signal
                case 0b0000: return (Frames[4], 0f);
                //Vertical
                case 0b1100: return (Frames[2], 0f);
                //Horizontal
                case 0b0011: return (Frames[2], 90f);
                //EndPoint
                case 0b0100: return (Frames[3], 0f);
                case 0b0001: return (Frames[3], 90f);
                case 0b1000: return (Frames[3], 180f);
                case 0b0010: return (Frames[3], 270f);
                //Corner
                case 0b0101: return (Frames[1], 0f);
                case 0b1001: return (Frames[1], 90f);
                case 0b1010: return (Frames[1], 180f);
                case 0b0110: return (Frames[1], 270f);
                //Edge
                case 0b0111: return (Frames[0], 0f);
                case 0b1101: return (Frames[0], 90f);
                case 0b1011: return (Frames[0], 180f);
                case 0b1110: return (Frames[0], 270f);
                //Inside
                case 0b1111: return (null, 0f);
            }
            return (null, 0f);
        }

        /// <summary>
        /// 还原为白色DotLine
        /// </summary>
        private void ResetAreaToDefault(int x1, int x2, int y1, int y2)
        {
            for (int i = x1; i <= x2; i++)
            {
                for (int j = y1; j <= y2; j++)
                {
                    SetTile(i, j, DotLine);
                    SetColor(i, j, Color.white);
                }
            }
        }

        private void SetTileAndRotation(int x, int y, Tile tile, float rotZ)
        {
            SetTile(x, y, tile);
            SetRotation(x, y, rotZ);
        }

        private void SetTile(int x, int y, Tile tile)
        {
            _tilePos.x = x;
            _tilePos.y = y;
            Tilemap.SetTile(_tilePos, tile);
        }

        private void SetRotation(int x, int y, float rotZ)
        {
            if (_rotationMatrices.TryGetValue(rotZ, out var matrix)) 
            {
                _tilePos.x = x;
                _tilePos.y = y;
                Tilemap.SetTransformMatrix(_tilePos, matrix);
            }
        }

        private void SetColor(int x, int y, Color color)
        {
            _tilePos.x = x;
            _tilePos.y = y;
            Tilemap.SetColor(_tilePos, color);
        }

        private Vector3Int _tilePos = Vector3Int.zero;
        private static readonly Dictionary<float, Matrix4x4> _rotationMatrices = new()
        {
            { 0f, Matrix4x4.identity },
            { 90f, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 90f), Vector3.one) },
            { 180f, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 180f), Vector3.one) },
            { 270f, Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, 270f), Vector3.one) },
        };

        private Color GetDefaultColorById(int id)
        {
            return Color.HSVToRGB((id * 0.618f) % 1f, Saturation, Value);
        }

        private void ClampBounds(ref int x1, ref int x2, ref int y1, ref int y2)
        {
            if (x1 > x2) (x1, x2) = (x2, x1);
            if (y1 > y2) (y1, y2) = (y2, y1);
        }
    }
}
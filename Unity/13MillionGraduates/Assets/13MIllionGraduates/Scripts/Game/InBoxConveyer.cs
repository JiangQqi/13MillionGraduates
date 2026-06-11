using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public class InBoxConveyer : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Player拿取Cube时的Transform")]
        public Transform InBoxTransform;
        public Material InBox_ConveyerBelt;

        [Header("Conveyer")]
        public float ConveyerSpeed = 5f;
        public float ConveyerBeltSpeedFactor = 1f;

        //[Header("Audio")]
        //public AudioSource Source;

        public bool IsDataCubeAvailable => m_DataCubeList.Count > 0 && m_DataCubeList[0].transform.localPosition == Vector3.zero;
        public DataCube GetFirstDataCube() => m_DataCubeList.Count > 0 ? m_DataCubeList[0] : null;

        private List<DataCube> m_DataCubeList;

        private bool m_IsConveying;
        //private bool m_WasConveying;
        private float m_CurrentSpeed;
        private float m_ConveyerBlet;//传送带材质偏移值
        private bool m_IsPlayerIsGrabingDataCube;//player正在拿取cube时第一个方块不再向上传送

        private void Awake()
        {
            GameManager.Ins.OnGameInitialized += () => Init(LevelManager.Ins.CurrVisualInputCubes);
        }

        public void Init(List<string> values)
        {
            if(m_DataCubeList != null)
                for (int i = m_DataCubeList.Count - 1; i >= 0; i--) Destroy(m_DataCubeList[i].gameObject);

            m_DataCubeList = new List<DataCube>();
            if (values == null) return;
            for (int i = 0; i < values.Count; i++)
            {
                DataCube cube = Instantiate(GameManager.Ins.DataCubePrefab, transform);
                cube.transform.localPosition = Vector3.down * (1.25f * i + 8.5f);
                cube.SetValue(values[i]);

                m_DataCubeList.Add(cube);
            }

            m_ConveyerBlet = 0;
            m_CurrentSpeed = 0;
            m_IsPlayerIsGrabingDataCube = false;
        }

        private void Update()
        {
            float minDist = float.MaxValue;
            m_IsConveying = false;

            for (int i = 0; i < m_DataCubeList?.Count; i++) 
            {
                if (i == 0 && m_IsPlayerIsGrabingDataCube) continue;

                float dist = Vector3.Distance(m_DataCubeList[i].transform.localPosition, Vector3.down * 1.25f * i);
                if (dist < 0.001f) continue;
                m_IsConveying = true;
                if (dist < minDist) minDist = dist;
            }

            float accel = ConveyerSpeed * 4f;
            float targetSpeed = m_IsConveying ? Mathf.Min(ConveyerSpeed, Mathf.Sqrt(2f * accel * minDist + 0.001f)) : 0f;
            m_CurrentSpeed = Mathf.MoveTowards(m_CurrentSpeed, targetSpeed, accel * Time.deltaTime);

            for (int i = 0; i < m_DataCubeList?.Count; i++) 
            {
                if (i == 0 && m_IsPlayerIsGrabingDataCube) continue;

                Vector3 currPosition = m_DataCubeList[i].transform.localPosition;
                Vector3 destination = Vector3.down * 1.25f * i;

                float distance = Vector3.Distance(destination, currPosition);
                if (distance < 0.001f)
                {
                    m_DataCubeList[i].transform.localPosition = destination;
                    continue;
                }

                Vector3 way = (destination - currPosition).normalized;
                float move = m_CurrentSpeed * Time.deltaTime;

                m_DataCubeList[i].transform.localPosition += way * Mathf.Min(move, distance);
            }

            m_ConveyerBlet -= m_CurrentSpeed * ConveyerBeltSpeedFactor * Time.deltaTime;
            m_ConveyerBlet %= 1f;
            InBox_ConveyerBelt.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, m_ConveyerBlet));

            //if (m_IsConveying && !m_WasConveying) Source.Play();
            //else if (!m_IsConveying && m_WasConveying) Source.Stop();

            //m_WasConveying = m_IsConveying;
        }

        public void StartGrabing()
        {
            m_IsPlayerIsGrabingDataCube = true;
        }

        public void ExitGrabing()
        {
            if (m_DataCubeList.Count > 0) m_DataCubeList.RemoveAt(0);

            m_IsPlayerIsGrabingDataCube = false;
        }
    }
}
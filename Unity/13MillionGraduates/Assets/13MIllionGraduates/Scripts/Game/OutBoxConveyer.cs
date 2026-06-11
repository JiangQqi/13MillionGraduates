using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace Game
{
    public class OutBoxConveyer : MonoBehaviour, IResettable
    {
        [Header("References")]
        [Tooltip("Player提交Cube时的Transform")]
        public Transform OutBoxTransform;
        public Material OutBox_ConveyerBelt;
        public VisualEffect VFX_OutBox;

        [Header("Conveyer")]
        public float ConveyerSpeed = 5f;
        public float ConveyerBeltSpeedFactor = 1f;

        [Header("Debug")]
        public bool IgnoreOutBoxValueCheck;

        private List<DataCube> m_DataCubeList;
        private int m_PendingSlots;

        private int m_OutputCount;

        private bool m_IsConveying;
        private float m_CurrentSpeed;
        private float m_ConveyerBlet;
        private bool m_IsPassedConveying = false;

        public int OutputCount => m_OutputCount;

        private void Awake()
        {
            m_DataCubeList = new List<DataCube>();
            m_ConveyerBlet = 0;

            GameManager.RegisterResettable(this);

            CodeExecutor.Ins.OnStateChanged += () =>
            {
                if (CodeExecutor.Ins.IsPassed)
                {
                    m_IsPassedConveying = true;
                    DriveConveyer(8);
                }
            };
        }

        public void OnReset()
        {
            for (int i = m_DataCubeList.Count - 1; i >= 0; i--) Destroy(m_DataCubeList[i].gameObject);
            m_DataCubeList.Clear();
            m_PendingSlots = 0;

            m_OutputCount = 0;

            m_ConveyerBlet = 0;
            m_CurrentSpeed = 0;
        }

        private void Update()
        {
            float minDist = float.MaxValue;
            m_IsConveying = false;

            int totalSlots = m_PendingSlots + m_DataCubeList.Count;
            for (int i = 0; i < m_DataCubeList.Count; i++)
            {
                float dist = Vector3.Distance(m_DataCubeList[i].transform.localPosition, Vector3.down * 1.25f * (totalSlots - i - 1));
                if (dist < 0.001f) continue;
                m_IsConveying = true;
                if (dist < minDist) minDist = dist;
            }

            float accel = ConveyerSpeed * 4f;
            float targetSpeed = m_IsConveying ? Mathf.Min(ConveyerSpeed, Mathf.Sqrt(2f * accel * minDist + 0.001f)) : 0f;
            m_CurrentSpeed = Mathf.MoveTowards(m_CurrentSpeed, targetSpeed, accel * Time.deltaTime);

            for (int i = 0; i < m_DataCubeList.Count; i++)
            {
                Vector3 currPosition = m_DataCubeList[i].transform.localPosition;
                Vector3 destination = Vector3.down * 1.25f * (totalSlots - i - 1);

                float distance = Vector3.Distance(destination, currPosition);
                if (distance < 0.001f) continue;

                Vector3 way = (destination - currPosition).normalized;
                float move = m_CurrentSpeed * Time.deltaTime;

                m_DataCubeList[i].transform.localPosition += way * Mathf.Min(move, distance);
            }

            m_ConveyerBlet += m_CurrentSpeed * ConveyerBeltSpeedFactor * Time.deltaTime;
            m_ConveyerBlet %= 1f;
            OutBox_ConveyerBelt.SetVector("_BaseMap_ST", new Vector4(1, 1, 0, m_ConveyerBlet));

            if (m_IsPassedConveying && !m_IsConveying)
            {
                m_IsPassedConveying = false;
                GameManager.Ins.InvokeOnGamePassed();
            }
        }

        /// <summary>
        /// 驱动传送带预留一个空位，方块到达时由Enqueue填入
        /// </summary>
        public void DriveConveyer(int num)
        {
            m_PendingSlots += num;
        }

        public void Enqueue(DataCube dataCube)
        {
            if (dataCube == null) return;

            dataCube.transform.parent = transform;
            dataCube.transform.localPosition = Vector3.zero;
            dataCube.RandomRotate();

            m_DataCubeList.Add(dataCube);
            if (m_PendingSlots > 0) m_PendingSlots--;

            VFX_OutBox.Play();

            if (IgnoreOutBoxValueCheck) return;

            if (m_OutputCount >= LevelManager.Ins.CurrExpectedOutputCount)
            {
                CodeExecutor.Ins.HandleExecutionError($"你的OutBox(输出栏)里东西太多了！");
            }
            else if (!dataCube.Value.Equals(LevelManager.Ins.CurrExpectedOutputCube(m_OutputCount)))
            {
                CodeExecutor.Ins.HandleExecutionError($"OUTBOX(输出栏)内容错误！\n公司高层想要的是 {LevelManager.Ins.CurrExpectedOutputCube(m_OutputCount)}，\n但你却送进去了{dataCube.Value}。");
            }
            m_OutputCount++;
        }
    }
}
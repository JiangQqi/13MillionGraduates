using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.VFX;

namespace Game
{
    /// <summary>
    /// 图层顺序一般为
    /// 0-DataCube->floor
    /// 1-DataCube_Text, Player_Shadow
    /// 2-Leg, Hand
    /// 3-Body
    /// 4-Tile
    /// 5-Head
    /// 6-DataCube->copy
    /// 7-DataCube_Text
    /// 8-DataCube->hand
    /// 9-DataCube_Text
    /// </summary>
    public class PlayerController : MonoBehaviour, IResettable
    {
        [Header("Renference")]
        public Transform DataCubeSocket;
        public Transform CopyDataCubeSocket;
        public Transform CellDataCubeSocket;
        public SpriteRenderer ArithmeticIcon;

        private Carpet m_Carpet;
        private AreaManager m_AreaManager;
        private InBoxConveyer m_InBoxConveyer;
        private OutBoxConveyer m_OutBoxConveyer;
        private Animator m_Animator;

        [Header("VFX")]
        public VisualEffect VFX_Walk;
        public List<Sprite> ArithmeticSprites = new List<Sprite>();
        public List<Color> ArithmeticIconColors = new List<Color>();

        [Header("Move")]
        public float MoveSpeed = 10f;

        private Vector3 m_Destination;
        private bool m_IsFacingLeft;

        public float AnimatorSpeed
        {
            get => m_Animator.speed;
            set => m_Animator.speed = value;
        }

        /// <summary>
        /// Player使用 StateMachine 而非 StateQueue，是为了能够在任意时刻自由选择操作（类似沙盒模式），
        /// 从而探索正确的解法，而非只能在游戏开始前预先设定好程序。
        /// </summary>
        public StateMachine StateMachine => m_StateMachine;
        private StateMachine m_StateMachine;

        public DataCube HoldingDataCube => m_HoldingDataCube;
        private DataCube m_HoldingDataCube;
        private DataCube m_CopyDataCube;
        private DataCube m_CellDataCube;

        private int m_AreaId;
        private int m_Index;
        private ArithmeticType m_ArithmeticType;

        public event UnityAction OnInBoxCompleted;
        public event UnityAction OnDropDataCubeCompleted;
        public event UnityAction OnOutBoxCompleted;
        public event UnityAction OnCopyToCompleted;
        public event UnityAction OnCopyFromCompleted;
        public event UnityAction OnArithmeticCompleted;

        private readonly Vector3 m_VFX_Arithmetic_Pos = new Vector3(0, 2.35f, -1f);
        private readonly Vector3 m_VFX_Arithmetic_Bump_Pos = new Vector3(0, -.2f, -1f);
        private readonly Vector3 m_VFX_DropDataCube_Pos = new Vector3(-3.51f, .17f, -1f);

        private void Awake()
        {
            m_Carpet = GameManager.Ins.Carpet;
            m_AreaManager = GameManager.Ins.AreaManager;
            m_InBoxConveyer = GameManager.Ins.InBoxConveyer;
            m_OutBoxConveyer = GameManager.Ins.OutBoxConveyer;

            m_StateMachine = new StateMachine();
            m_StateMachine.AddState(StateType.Idle, new IdleState(this));
            m_StateMachine.AddState(StateType.InBox, new InBoxState(this, m_InBoxConveyer));
            m_StateMachine.AddState(StateType.OutBox, new OutBoxState(this, m_OutBoxConveyer));
            m_StateMachine.AddState(StateType.DropDataCube, new DropDataCubeState(this));

            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_StateMachine.AddState(StateType.CopyTo, new CopyToState(this, m_Carpet));
                    m_StateMachine.AddState(StateType.CopyFrom, new CopyFromState(this, m_Carpet));
                    m_StateMachine.AddState(StateType.Arithmetic, new ArithmeticState(this, m_Carpet));
                    break;

                case GameMode.Advanced:
                    m_StateMachine.AddState(StateType.CopyTo, new CopyToState(this, m_AreaManager));
                    m_StateMachine.AddState(StateType.CopyFrom, new CopyFromState(this, m_AreaManager));
                    m_StateMachine.AddState(StateType.Arithmetic, new ArithmeticState(this, m_AreaManager));
                    break;
            }

            m_Animator = GetComponentInChildren<Animator>();

            m_Destination = transform.position;
            m_StateMachine.ChangeState(StateType.Idle);

            GameManager.RegisterResettable(this);
        }

        public void OnReset()
        {
            transform.position = new Vector3(-6f, 2.5f, 0);
            m_Destination = transform.position;
            SetFacing(false);

            m_StateMachine.CurrState.OnCompleted -= CodeExecutor.Ins.Advance;
            m_StateMachine.ChangeState(StateType.Idle);

            if (m_HoldingDataCube != null) Destroy(m_HoldingDataCube.gameObject);
            if (m_CopyDataCube != null) Destroy(m_CopyDataCube.gameObject);
            if (m_CellDataCube != null) Destroy(m_CellDataCube.gameObject);
            m_HoldingDataCube = null;
            m_CopyDataCube = null;
            m_CellDataCube = null;

            m_Animator.Rebind();
            m_Animator.Play("Player_Idle", 0, 0);
        }

        private void Update()
        {
            UpdateMove();

            m_StateMachine.Update();
        }

        public void SetDestination(Vector3 target)
        {
            m_Destination = target;
            if (transform.position != target) 
            {
                m_Animator.SetBool("IsWalking", true);
                VFX_Walk.Play();

                SetFacing(transform.position.x > target.x);
            }
        }

        public void SetFacing(bool isFacingLeft)
        {
            if (m_IsFacingLeft != isFacingLeft)
            {
                transform.rotation = Quaternion.Euler(0f, isFacingLeft ? 180f : 0f, 0f);
                m_IsFacingLeft = isFacingLeft;

                if (m_HoldingDataCube != null)//手上的Cube不跟着旋转
                {
                    m_HoldingDataCube.transform.rotation = Quaternion.identity;
                }
            }
        }

        public bool HasReachedDestination() => transform.position == m_Destination;

        private void UpdateMove()
        {
            if (m_Destination == null) return;

            if (HasReachedDestination()) 
            {
                m_Animator.SetBool("IsWalking", false);
                VFX_Walk.Stop();
            }
            else
            {
                Vector3 way = m_Destination - transform.position;
                float moveDis = Mathf.Min(way.magnitude, MoveSpeed * Time.deltaTime);
                transform.position += way.normalized * moveDis;
            }
        }

        /// <summary>
        /// 以下为各种State以及其AniEvent
        /// </summary>
        #region InBox
        public void InBox_GrabDataCube()
        {
            SetFacing(true);

            m_InBoxConveyer.StartGrabing();

            m_Animator.SetTrigger("InBox_GrabDataCube");
        }

        public void InBox_GrabDataCube_AniEvent01()
        {
            m_HoldingDataCube = m_InBoxConveyer.GetFirstDataCube();
            m_HoldingDataCube.transform.SetParent(DataCubeSocket);
            m_HoldingDataCube.transform.localPosition = Vector3.zero;
            m_HoldingDataCube.SetRendererSortingOrder(8);
        }

        public void InBox_GrabDataCube_AniEvent02()
        {
            m_InBoxConveyer.ExitGrabing();

            OnInBoxCompleted?.Invoke();
        }
        #endregion

        #region DropDataCube
        public void DropDataCube()
        {
            m_Animator.SetTrigger("DropDataCube");
        }

        public void DropDataCube_AniEvent01()
        {
            Destroy(m_HoldingDataCube.gameObject);
            m_HoldingDataCube = null;

            VFXManager.Ins.Play(VFXType.DropDataCube, transform.TransformPoint(m_VFX_DropDataCube_Pos));

            OnDropDataCubeCompleted?.Invoke();
        }
        #endregion

        #region OutBox
        public void OutBox_DropDataCube()
        {
            SetFacing(false);

            m_Animator.SetTrigger("OutBox_DropDataCube");
        }

        public void OutBox_DropDateCube_AniEvent01()
        {
            m_OutBoxConveyer.Enqueue(m_HoldingDataCube);

            m_HoldingDataCube = null;

            OnOutBoxCompleted?.Invoke();
        }
        #endregion

        #region CopyTo
        public void CopyTo_CopyTo(int areaId, int index)
        {
            m_AreaId = areaId;
            m_Index = index;

            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_CellDataCube = m_Carpet.GetDataCube(m_Index);
                    m_CellDataCube?.transform.SetParent(CellDataCubeSocket);
                    break;

                case GameMode.Advanced:
                    break;
            }

            m_Animator.SetTrigger("CopyTo");
        }

        /// <summary>
        /// 此处是真正的动画开始播放的第一帧
        /// 复制DataCube以及设置图层
        /// </summary>
        public void CopyTo_CopyTo_AniEvent01()
        {
            DataCube copyCube = Instantiate(GameManager.Ins.DataCubePrefab, CopyDataCubeSocket);
            copyCube.transform.rotation = Quaternion.identity;
            copyCube.SetValue(m_HoldingDataCube.Value);
            copyCube.SetRendererSortingOrder(6);

            m_CopyDataCube = copyCube;
        }

        public void CopyTo_CopyTo_AniEvent02()
        {
            //设置DataCube图层
            m_CopyDataCube.SetRendererSortingOrder(8);

            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_Carpet.Append(m_CopyDataCube, m_Index);
                    break;

                case GameMode.Advanced:
                    break;
            }

            m_CopyDataCube = null;
            m_CellDataCube = null;

            OnCopyToCompleted?.Invoke();
        }
        #endregion

        #region CopyFrom
        /// <summary>
        /// 先拿走原有的DataCube,再长出来一个新的DataCube
        /// </summary>
        public void CopyFrom_CopyFrom(int areaId, int index)
        {
            m_AreaId = areaId;
            m_Index = index;

            m_Animator.SetTrigger(m_HoldingDataCube == null ?
                "CopyFrom" :
                "CopyFrom_WithCube");
        }

        public void CopyFrom_CopyFrom_AniEvent01()
        {
            if (m_HoldingDataCube != null) m_HoldingDataCube.SetRendererSortingOrder(6);

            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_CopyDataCube = m_Carpet.GetDataCube(m_Index);
                    m_Carpet.Remove(m_Index);
                    break;

                case GameMode.Advanced:
                    break;
            }
            m_CopyDataCube.transform.SetParent(CopyDataCubeSocket);
            m_CopyDataCube.SetRendererSortingOrder(8);

            DataCube cellCube = Instantiate(GameManager.Ins.DataCubePrefab, CellDataCubeSocket);
            cellCube.transform.rotation = Quaternion.identity;
            cellCube.SetValue(m_CopyDataCube.Value);
            cellCube.SetRendererSortingOrder(6);

            m_CellDataCube = cellCube;
        }

        public void CopyFrom_CopyFrom_AniEvent02()
        {
            if (m_HoldingDataCube != null) Destroy(m_HoldingDataCube.gameObject);

            m_HoldingDataCube = m_CopyDataCube;
            m_HoldingDataCube.transform.SetParent(DataCubeSocket);
            m_HoldingDataCube.transform.localPosition = Vector3.zero;
            m_HoldingDataCube.transform.localScale = Vector3.one;

            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_Carpet.Append(m_CellDataCube, m_Index);
                    break;

                case GameMode.Advanced:
                    break;
            }

            m_CopyDataCube = null;
            m_CellDataCube = null;

            OnCopyFromCompleted?.Invoke();
        }
        #endregion

        #region Arithmetic
        public void Arithmetic_Arithmetic(int areaId, int index, ArithmeticType type)
        {
            m_AreaId = areaId;
            m_Index = index;
            m_ArithmeticType = type;

            if (!Arithmetic.IsUnaryType(type)) SetFacing(false);

            if (Arithmetic.IsUnaryType(type)) 
                m_Animator.SetTrigger(m_HoldingDataCube == null ? 
                    "Arithmetic_Bump" : 
                    "Arithmetic_Bump_WithCube");
            else m_Animator.SetTrigger("Arithmetic");
        }

        public void Arithmetic_Arithmetic_AniEvent01()
        {
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_CopyDataCube = m_Carpet.GetDataCube(m_Index);
                    m_CopyDataCube.transform.SetParent(CopyDataCubeSocket);
                    m_Carpet.Remove(m_Index);
                    break;

                case GameMode.Advanced:
                    break;
            }

            DataCube cellCube = Instantiate(GameManager.Ins.DataCubePrefab, CellDataCubeSocket);
            cellCube.transform.rotation = Quaternion.identity;
            cellCube.SetValue(m_CopyDataCube.Value);
            cellCube.SetRendererSortingOrder(6);
            m_CellDataCube = cellCube;

            ArithmeticIcon.sprite = ArithmeticSprites[(int)m_ArithmeticType];
            ValueType type = Arithmetic.GetResultType(m_HoldingDataCube.Value, m_CopyDataCube.Value, m_ArithmeticType);
            ArithmeticIcon.color = ArithmeticIconColors[(int)type];
        }

        public void Arithmetic_Arithmetic_AniEvent02()
        {
            string result = Arithmetic.Compute(m_HoldingDataCube.Value, m_CopyDataCube.Value, m_ArithmeticType);
            m_HoldingDataCube.SetValue(result);

            Destroy(m_CopyDataCube?.gameObject);

            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_Carpet.Append(m_CellDataCube, m_Index);
                    break;

                case GameMode.Advanced:
                    break;
            }

            VFXManager.Ins.Play(VFXType.Arithmetic, transform.TransformPoint(m_VFX_Arithmetic_Pos));

            m_CopyDataCube = null;
            m_CellDataCube = null;
        }

        public void Arithmetic_Arithmetic_AniEvent03()
        {
            OnArithmeticCompleted?.Invoke();
        }

        public void Arithmetic_Bump_AniEvent01()
        {
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_HoldingDataCube = m_Carpet.GetDataCube(m_Index);
                    m_HoldingDataCube.transform.SetParent(DataCubeSocket);
                    m_Carpet.Remove(m_Index);
                    break;

                case GameMode.Advanced:
                    break;
            }
        }

        public void Arithmetic_Bump_AniEvent02()
        {
            string result = Arithmetic.Compute("0", m_HoldingDataCube.Value, m_ArithmeticType);
            m_HoldingDataCube.SetValue(result);

            DataCube cellCube = Instantiate(GameManager.Ins.DataCubePrefab, CellDataCubeSocket);
            cellCube.transform.rotation = Quaternion.identity;
            cellCube.SetValue(m_HoldingDataCube.Value);
            cellCube.SetRendererSortingOrder(6);
            m_CellDataCube = cellCube;

            VisualEffect vfx = VFXManager.Ins.Play(VFXType.Arithmetic_Bump, transform.TransformPoint(m_VFX_Arithmetic_Bump_Pos));
            vfx.SetBool("IsInc", m_ArithmeticType == ArithmeticType.Inc);
        }

        public void Arithmetic_Bump_AniEvent03()
        {
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_Carpet.Append(m_CellDataCube, m_Index);
                    m_CellDataCube = null;
                    break;

                case GameMode.Advanced:
                    break;
            }

            OnArithmeticCompleted?.Invoke();
        }

        public void Arithmetic_Bump_WithCube_AniEvent01()
        {
            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_CopyDataCube = m_Carpet.GetDataCube(m_Index);
                    m_CopyDataCube.transform.SetParent(CopyDataCubeSocket);
                    m_CopyDataCube.SetRendererSortingOrder(8);
                    m_Carpet.Remove(m_Index);
                    break;

                case GameMode.Advanced:
                    break;
            }

            m_HoldingDataCube.SetRendererSortingOrder(6);
        }

        public void Arithmetic_Bump_WithCube_AniEvent02()
        {
            string result = Arithmetic.Compute("0", m_CopyDataCube.Value, m_ArithmeticType);
            m_CopyDataCube.SetValue(result);

            DataCube cellCube = Instantiate(GameManager.Ins.DataCubePrefab, CellDataCubeSocket);
            cellCube.transform.rotation = Quaternion.identity;
            cellCube.SetValue(m_CopyDataCube.Value);
            cellCube.SetRendererSortingOrder(6);
            m_CellDataCube = cellCube;

            VisualEffect vfx = VFXManager.Ins.Play(VFXType.Arithmetic_Bump, transform.TransformPoint(m_VFX_Arithmetic_Bump_Pos));
            vfx.SetBool("IsInc", m_ArithmeticType == ArithmeticType.Inc);
        }

        public void Arithmetic_Bump_WithCube_AniEvent03()
        {
            Destroy(m_HoldingDataCube?.gameObject);
        }

        public void Arithmetic_Bump_WithCube_AniEvent04()
        {
            m_HoldingDataCube = m_CopyDataCube;
            m_HoldingDataCube.transform.SetParent(DataCubeSocket);
            m_HoldingDataCube.transform.localPosition = Vector3.zero;
            m_HoldingDataCube.transform.localScale = Vector3.one;

            switch (LevelManager.Ins.GameMode)
            {
                case GameMode.Classic:
                    m_Carpet.Append(m_CellDataCube, m_Index);
                    break;
                case GameMode.Advanced:
                    break;
            }

            m_CopyDataCube = null;
            m_CellDataCube = null;

            OnArithmeticCompleted?.Invoke();
        }
        #endregion
    }
}
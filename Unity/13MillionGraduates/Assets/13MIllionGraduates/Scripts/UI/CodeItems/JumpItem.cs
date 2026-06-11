using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class JumpItem : CodeItem
    {
        [Header("Jump")]
        public TextMeshProUGUI TypeText;
        public LineRenderer Line;
        public LineRenderer ShadowLine;
        public Image Arrow;
        public Image ShadowArrow;
        [Range(2, 100)]
        public int LineSegmentCount = 30;
        public Color FadeOutColor;
        private Canvas m_ArrowCanvas;
        private Canvas m_ShadowArrowCanvas;

        private JumpOperation m_Operation = new JumpOperation();

        public override IOperation Operation => m_Operation;
        public override OperationType OperationType => m_Operation.OperationType;
        public override bool HasParams => true;
        public override bool ShowLineNum => true;
        public override bool Addressable => false;

        private LabelItem m_Label;
        private bool m_JumpTypeSetExternally;

        private Color m_LineColor;
        private Color m_LineTargetColor;
        private float m_ShadowLineTargetAlpha = .5f;
        private float m_ShadowLineCurrAlpha = .5f;

        public JumpType JumpType => m_Operation.Param2;
        public LabelItem Label
        {
            get => m_Label;
            set 
            {
                m_Label = value;
                m_Operation.Param1 = m_Label.Operation as LabelOperation;
            }
        }

        private void Start()
        {
            if (!m_JumpTypeSetExternally)
            {
                OperationConfig cfg = LevelManager.Ins.GetOperationConfig(OperationType.Jump);
                if (cfg.Jump_Always) SetJumpType(JumpType.Any);
                else if (cfg.Jump_IfZero) SetJumpType(JumpType.IfZero);
                else if (cfg.Jump_IfNegative) SetJumpType(JumpType.IfNegative);
            }

            Line.positionCount = LineSegmentCount;
            ShadowLine.positionCount = LineSegmentCount;

            m_ArrowCanvas = Arrow.GetComponent<Canvas>();
            m_ShadowArrowCanvas = ShadowArrow.GetComponent<Canvas>();
        }

        protected override void Update()
        {
            base.Update();

            UpdateLineColor(Line, Arrow, m_LineTargetColor);
            UpdateLineAlpha(ShadowLine, ShadowArrow, ref m_ShadowLineCurrAlpha, m_ShadowLineTargetAlpha);

            RefreshLine();
        }

        public void SetJumpType(JumpType type)
        {
            m_JumpTypeSetExternally = true;

            m_Operation.Param2 = type;
            TypeText.text = type switch
            {
                JumpType.Any => "any",
                JumpType.IfZero => "zro",
                JumpType.IfNegative => "neg",
                _ => "?"
            };
        }

        public void SetColor(Color color)
        {
            m_LineColor = color;
            m_LineTargetColor = m_LineColor;

            Gradient grad = Line.colorGradient;
            grad.SetColorKeys(
                new GradientColorKey[]{
                    new GradientColorKey(color, 0),
                    new GradientColorKey(color, 1),
                });
            Line.colorGradient = grad;

            Arrow.color = color;
        }

        public void StartFadeInLine()
        {
            m_LineTargetColor = m_LineColor;
            m_ShadowLineTargetAlpha = .5f;

            Line.sortingOrder = 3;
            ShadowLine.sortingOrder = 2;
            m_ArrowCanvas.sortingOrder = 3;
            m_ShadowArrowCanvas.sortingOrder = 2;
        }

        public void StartFadeOutLine()
        {
            m_LineTargetColor = FadeOutColor;
            m_ShadowLineTargetAlpha = 0f;

            Line.sortingOrder = 1;
            ShadowLine.sortingOrder = 0;
            m_ArrowCanvas.sortingOrder = 1;
            m_ShadowArrowCanvas.sortingOrder = 0;
        }

        private void UpdateLineColor(LineRenderer line, Image arrow, Color tgt)
        {
            Color currColor = arrow.color;

            if (currColor == tgt) return;

            if (ColorDistance(currColor, tgt) < 0.01f) currColor = tgt;
            else currColor = Color.Lerp(currColor, tgt, 10f * Time.unscaledDeltaTime);

            Gradient grad = line.colorGradient;
            grad.SetKeys(
                new GradientColorKey[] {
                new GradientColorKey(currColor, 0),
                new GradientColorKey(currColor, 1),
                },
                grad.alphaKeys
            );
            line.colorGradient = grad;

            float arrowAlpha = arrow.color.a;
            arrow.color = new Color(currColor.r, currColor.g, currColor.b, arrowAlpha);
        }

        private float ColorDistance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }

        private void UpdateLineAlpha(LineRenderer line, Image arrow, ref float curr, float tgt)
        {
            if (curr == tgt) return;

            if (Mathf.Abs(curr - tgt) < 1f) curr = tgt;
            else curr = Mathf.Lerp(curr, tgt, 10f * Time.unscaledDeltaTime);

            Gradient grad = line.colorGradient;
            grad.SetAlphaKeys(
                new GradientAlphaKey[]{
                    new GradientAlphaKey(curr, 0),
                    new GradientAlphaKey(curr, 1),
                });
            line.colorGradient = grad;

            Color color = arrow.color;
            color.a = curr;
            arrow.color = color;
        }

        private Vector2 _shadowOffset = new Vector2(2f, -5f);
        private void RefreshLine()
        {
            if (m_Label == null) return;

            Vector2 startPos = Line.transform.position;
            Vector2 endPos = m_Label.transform.position + Vector3.right * 90f;

            int isStartUp = startPos.y > endPos.y ? 1 : -1;
            float diffY = Mathf.Abs(startPos.y - endPos.y);
            float kOffset = Mathf.Clamp(diffY * .5f + 10f, 50f, 250f);
            float outOffset = Mathf.Clamp((Mathf.Min(diffY, 800f) - 480f) / 7f, 0f, 30f);

            Vector2 startCtrl = startPos + new Vector2(kOffset, isStartUp * outOffset);
            Vector2 endCtrl = endPos + new Vector2(kOffset + 100f, -isStartUp * outOffset);

            float tStep = 1 / (float)(LineSegmentCount - 1);
            for (int i = 0; i < LineSegmentCount; i++)
            {
                float t = i * tStep;
                float u = 1 - t;

                Vector2 point = u * u * u * startPos +
                                           3 * u * u * t * startCtrl +
                                           3 * u * t * t * endCtrl +
                                           t * t * t * endPos;

                Line.SetPosition(i, point);
                ShadowLine.SetPosition(i, point + _shadowOffset);
            }

            Arrow.transform.position = endPos + Vector2.right * 3f;
            ShadowArrow.transform.position = endPos + Vector2.right * 3f + _shadowOffset;
        }
    }
}
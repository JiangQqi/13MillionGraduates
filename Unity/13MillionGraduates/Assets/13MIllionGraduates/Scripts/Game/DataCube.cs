using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game
{
    public class DataCube : MonoBehaviour
    {
        [Header("References")]
        public SpriteRenderer DataCubeRenderer;
        public TextMeshPro DataCubeText;

        [Header("Font Setting")]
        public float DefaultFontSize = 7f;
        public float MaxAllowedWidth = .9f;

        [SerializeField, HideInInspector] private string m_Value;
        [SerializeField, HideInInspector] private ValueType m_Type;

        public string Value => m_Value;
        public ValueType Type => m_Type;

        private int m_CurrRenderOreder = 8;
        public int CurrRenderOreder => m_CurrRenderOreder;

        public void SetValue(string value)
        {
            if (string.IsNullOrEmpty(value)) value = "null";

            m_Value = value;
            m_Type = Arithmetic.GetType(m_Value);

            SetText(value);
            DataCubeRenderer.sprite = GameManager.Ins.DataCubeSpirtes[(int)m_Type];
            DataCubeText.color = GameManager.Ins.DataCubeTextColors[(int)m_Type];

            RandomRotate();
        }

        public void RandomRotate()
        {
            transform.Rotate(0, 0, UnityEngine.Random.Range(-5f, 5f));
        }

        public void SetRendererSortingOrder(int order)
        {
            if (DataCubeRenderer == null || DataCubeText == null) return;

            DataCubeRenderer.sortingOrder = order;
            DataCubeText.sortingOrder = order + 1;

            m_CurrRenderOreder = order;
        }

        private void SetText(string text)
        {
            text = FormatValue(text);
            DataCubeText.text = text;

            DataCubeText.fontSize = DefaultFontSize;
            DataCubeText.ForceMeshUpdate();

            float textWidth = DataCubeText.preferredWidth;
            if (textWidth > MaxAllowedWidth) 
            {
                float scale = MaxAllowedWidth / textWidth;
                DataCubeText.fontSize *= scale;
            }
        }

        private string FormatValue(string value)
        {
            if (value.Equals("true")) return "T";
            if (value.Equals("false")) return "F";
            return value;
        }
    }
}
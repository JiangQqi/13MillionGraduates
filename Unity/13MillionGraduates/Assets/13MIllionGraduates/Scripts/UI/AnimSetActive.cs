using UnityEngine;

namespace Game.UI
{
    public class AnimSetActive : MonoBehaviour
    {
        public void SetActiveTrue() => gameObject.SetActive(true);
        public void SetActiveFalse() => gameObject.SetActive(false);
    }
}
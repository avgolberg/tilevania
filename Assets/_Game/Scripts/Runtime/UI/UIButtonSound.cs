using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TileVania
{
    /// <summary>Plays UI sounds when a button is highlighted or pressed.</summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, ISelectHandler, IPointerEnterHandler
    {
        void Awake() => GetComponent<Button>().onClick.AddListener(() => AudioManager.Play(Sfx.UIClick));

        public void OnSelect(BaseEventData e) => AudioManager.Play(Sfx.UISelect, 0.5f);

        public void OnPointerEnter(PointerEventData e)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}

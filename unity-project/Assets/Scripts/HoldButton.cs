using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Vector2 direction;
    private bool held;
    public void OnPointerDown(PointerEventData e)
    {
        held = true;
        if (GameManager.Instance.IsPlaying)
            GameManager.Instance.SetDirection(direction);
    }
    public void OnPointerUp(PointerEventData e) => Release();
    public void OnPointerExit(PointerEventData e) => Release();
    private void OnDisable() => Release();
    private void Release()
    {
        if (held && GameManager.Instance != null)
            GameManager.Instance.SetDirection(Vector2.zero);
        held = false;
    }
}

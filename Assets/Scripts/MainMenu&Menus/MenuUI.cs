using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public static class MenuUI //: MonoBehaviour
{
    //istantly show or hide a panel 
    public static void SetVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    //Fade a CanvasGroup's alpha, optionally sliding it vertically
    public static IEnumerator Fade(CanvasGroup group, float from, float to, float time,
                                   RectTransform slide = null, Vector2 basePos = default, float slideDistance = 0f)
    {
        if (group == null) yield break;

        group.interactable = false;
        group.blocksRaycasts = to > 0.5f;

        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float p = time > 0f ? Mathf.Clamp01(t / time) : 1f;
            float eased = 1f - (1f - p) * (1f - p); //ease out
            group.alpha = Mathf.Lerp(from, to, eased);
            if (slide != null)
            {
                //Slides from "slideDistance" below into place when showing, and drifts down when hiding.
                float offset = to > from ? (1f - eased) * slideDistance : eased * slideDistance;
                slide.anchoredPosition = basePos + Vector2.down * offset;
            }
            yield return null;
        }

        group.alpha = to;
        if (slide != null) slide.anchoredPosition = basePos;
        group.interactable = to > 0.5f;
        group.blocksRaycasts = to > 0.5f;
    }

    //Select a button for keyboard/gamepad without playing its hover sound
    public static void SelectQuietly(GameObject target)
    {
        if (EventSystem.current == null) return;
        MenuButtonFeedback.SuppressHoverSound();
        EventSystem.current.SetSelectedGameObject(null);
        if (target != null) EventSystem.current.SetSelectedGameObject(target);
    }
}

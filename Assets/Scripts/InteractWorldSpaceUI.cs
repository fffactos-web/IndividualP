using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Windows;

public class InteractWorldSpaceUI : BaseInputModule
{
    [Tooltip("RectTransform прицела")]
    public RectTransform crosshair;

    public override void Process()
    {
        if (crosshair == null) return;

        // лкм
        if (input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(eventSystem);
            pointerData.position = crosshair.position;

            List<RaycastResult> results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            foreach (var r in results)
            {
                Button btn = r.gameObject.GetComponentInParent<Button>();
                if (btn != null)
                {
                    ExecuteEvents.Execute(btn.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                    break;
                }
            }
        }
    }
}
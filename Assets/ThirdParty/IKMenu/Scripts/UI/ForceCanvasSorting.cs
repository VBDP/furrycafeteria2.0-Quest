
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ForceCanvasSorting : UdonSharpBehaviour
{
    public Canvas canvas;

    void LateUpdate()
    {
        if (canvas.overrideSorting)
        {
            canvas.gameObject.SetActive(true);
            canvas.overrideSorting = false;
            canvas.gameObject.SetActive(false);
            if (transform.childCount > 3)
            {
                transform.GetChild(3).GetComponent<Canvas>().overrideSorting = false;
            }
        }
    }
}

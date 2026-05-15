
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class SetMeshLayer : UdonSharpBehaviour
{
    [SerializeField] private string sortingLayerName;
    [SerializeField] private int sortingOrder;

    void Start()
    {
        Renderer meshRenderer= GetComponent<Renderer>();
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }
}

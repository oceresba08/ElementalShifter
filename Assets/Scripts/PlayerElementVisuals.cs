
using UnityEngine;

public class PlayerElementVisuals : MonoBehaviour
{
    [Header("Visual Setup")]
    public Transform visualRoot;
    public GameObject neutralPrefab;

    private GameObject activeVisual;

    public void ShowElement(ElementData element)
    {
        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }

        GameObject prefab = element != null
            ? element.visualPrefab
            : neutralPrefab;

        if (prefab == null)
            return;

        Transform parent = visualRoot != null
            ? visualRoot
            : transform;

        activeVisual = Instantiate(
            prefab,
            parent,
            false
        );

        activeVisual.transform.localPosition =
            Vector3.zero;

        activeVisual.transform.localRotation =
            Quaternion.identity;

        activeVisual.transform.localScale =
            Vector3.one;
    }
}

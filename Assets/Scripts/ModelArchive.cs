using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ModelArchive : MonoBehaviour
{
    public List<GameObject> Models;
    public float Size;
    public void CreateModel(string Name, Vector3 Position)
    {
        foreach (var model in Models)
        {
            if (model.name == Name)
            {
                var obj = Instantiate(model, Position, transform.rotation);
                //FitToStandardSize(obj,Size * Vector3.one);
                obj.AddComponent<Rigidbody>().isKinematic = true;
                obj.AddComponent<SphereCollider>().radius = Size;
                var interact = obj.AddComponent<XRGrabInteractable>();
                interact.throwOnDetach = false;
                interact.useDynamicAttach = true;
            }
        }
    }
    
    public static void FitToStandardSize(GameObject obj, Vector3 standardSize)
    {
        if (obj == null) return;

        // Calculate the bounds of the GameObject
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;

        Bounds bounds = renderer.bounds;

        // Get the current size of the GameObject
        Vector3 currentSize = bounds.size;

        // Calculate the scale factor required to fit the object within the standard size
        Vector3 scaleFactor = new Vector3(
            standardSize.x / currentSize.x,
            standardSize.y / currentSize.y,
            standardSize.z / currentSize.z
        );

        // Apply the minimum scale factor to preserve aspect ratio
        float minScaleFactor = Mathf.Min(scaleFactor.x, scaleFactor.y, scaleFactor.z);

        // Apply the scaling to the GameObject
        obj.transform.localScale *= minScaleFactor;
    }
}

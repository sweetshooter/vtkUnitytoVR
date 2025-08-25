using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

[RequireComponent(typeof(Transform))]
public class ControllerVisualizer : MonoBehaviour
{
    public XRNode xrNode = XRNode.RightHand; // set in inspector to LeftHand/RightHand
    public Transform visualChild; // the small sphere (optional - assign in inspector)
    public Color idleColor = Color.gray;
    public Color pressedColor = Color.green;

    private InputDevice device;
    private Renderer visualRenderer;

    void Start()
    {
        TryInitDevice();
        if (visualChild != null)
        {
            visualRenderer = visualChild.GetComponent<Renderer>();
            if (visualRenderer != null) visualRenderer.material = new Material(visualRenderer.sharedMaterial);
        }
    }

    void TryInitDevice()
    {
        device = InputDevices.GetDeviceAtXRNode(xrNode);
    }

    void Update()
    {
        if (!device.isValid)
        {
            TryInitDevice();
        }

        // update transform from XRNode pose
        Vector3 pos;
        Quaternion rot;
        bool gotPos = device.TryGetFeatureValue(CommonUsages.devicePosition, out pos);
        bool gotRot = device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot);

        if (gotPos) transform.localPosition = pos;
        if (gotRot) transform.localRotation = rot;

        // change visual color when trigger pressed (as example)
        if (visualRenderer != null)
        {
            bool triggerPressed = false;
            if (device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)
            {
                visualRenderer.material.color = pressedColor;
            }
            else
            {
                visualRenderer.material.color = idleColor;
            }
        }
    }
}

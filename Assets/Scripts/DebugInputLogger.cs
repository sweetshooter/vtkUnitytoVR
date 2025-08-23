using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class DebugInputLogger : MonoBehaviour
{
    public bool showOnScreen = true;
    public float triggerThreshold = 0.05f; // float trigger 判定門檻
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    // store last values to avoid spam
    private bool lastLeftTriggerBool = false;
    private float lastLeftTriggerFloat = 0f;
    private bool lastRightTriggerBool = false;
    private float lastRightTriggerFloat = 0f;
    private bool lastLeftValid = false;
    private bool lastRightValid = false;

    void Start()
    {
        InitDevices();
    }

    void InitDevices()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            InitDevices();
        }

        UpdateDevice(leftDevice, XRNode.LeftHand);
        UpdateDevice(rightDevice, XRNode.RightHand);
    }

    void UpdateDevice(InputDevice device, XRNode node)
    {
        if (!device.isValid)
        {
            // log once when it becomes invalid
            if (node == XRNode.LeftHand && lastLeftValid)
            {
                Debug.Log("[DebugInputLogger] Left device became invalid.");
                lastLeftValid = false;
            }
            if (node == XRNode.RightHand && lastRightValid)
            {
                Debug.Log("[DebugInputLogger] Right device became invalid.");
                lastRightValid = false;
            }
            return;
        }

        // mark valid
        if (node == XRNode.LeftHand) lastLeftValid = true;
        if (node == XRNode.RightHand) lastRightValid = true;

        // trigger bool
        bool triggerBool = false;
        device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerBool);

        // trigger float
        float triggerFloat = 0f;
        device.TryGetFeatureValue(CommonUsages.trigger, out triggerFloat);

        // grip
        bool grip = false;
        device.TryGetFeatureValue(CommonUsages.gripButton, out grip);

        // primary button (A/X)
        bool primary = false;
        device.TryGetFeatureValue(CommonUsages.primaryButton, out primary);

        // device position valid?
        Vector3 pos;
        bool hasPos = device.TryGetFeatureValue(CommonUsages.devicePosition, out pos);

        // log only on changes (prevents spam)
        if (node == XRNode.LeftHand)
        {
            if (triggerBool != lastLeftTriggerBool || Mathf.Abs(triggerFloat - lastLeftTriggerFloat) > 0.02f || grip || primary)
            {
                Debug.LogFormat("[DebugInputLogger] Left  | valid:{0} | triggerBool:{1} | triggerFloat:{2:F2} | grip:{3} | primary:{4} | pos:{5}",
                    device.isValid, triggerBool, triggerFloat, grip, primary, hasPos ? pos.ToString("F2") : "no-pos");
                lastLeftTriggerBool = triggerBool;
                lastLeftTriggerFloat = triggerFloat;
            }
        }
        else if (node == XRNode.RightHand)
        {
            if (triggerBool != lastRightTriggerBool || Mathf.Abs(triggerFloat - lastRightTriggerFloat) > 0.02f || grip || primary)
            {
                Debug.LogFormat("[DebugInputLogger] Right | valid:{0} | triggerBool:{1} | triggerFloat:{2:F2} | grip:{3} | primary:{4} | pos:{5}",
                    device.isValid, triggerBool, triggerFloat, grip, primary, hasPos ? pos.ToString("F2") : "no-pos");
                lastRightTriggerBool = triggerBool;
                lastRightTriggerFloat = triggerFloat;
            }
        }
    }

    // simple on-screen overlay so you can see values inside headset (and in Game view)
    void OnGUI()
    {
        if (!showOnScreen) return;

        GUI.color = Color.white;
        GUILayout.BeginArea(new Rect(10, 10, 520, 200), GUI.skin.box);
        GUILayout.Label("DebugInputLogger (left / right):");
        GUILayout.Label(string.Format("Left valid: {0}  |  Right valid: {1}", leftDevice.isValid, rightDevice.isValid));
        float leftFloat = 0f, rightFloat = 0f;
        leftDevice.TryGetFeatureValue(CommonUsages.trigger, out leftFloat);
        rightDevice.TryGetFeatureValue(CommonUsages.trigger, out rightFloat);
        GUILayout.Label(string.Format("Trigger float -> Left: {0:F2}   Right: {1:F2}", leftFloat, rightFloat));
        bool leftBool=false, rightBool=false;
        leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out leftBool);
        rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out rightBool);
        GUILayout.Label(string.Format("Trigger bool -> Left: {0}   Right: {1}", leftBool, rightBool));
        GUILayout.Label("(Also reporting grip and primary to Console when pressed)");
        GUILayout.EndArea();
    }
}

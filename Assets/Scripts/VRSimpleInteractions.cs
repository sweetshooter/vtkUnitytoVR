using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class VRSimpleInteractions : MonoBehaviour
{
    [Header("Prefab & settings")]
    public GameObject spawnPrefab;
    public float spawnOffset = 0.15f;   // spawn distance in front of controller
    public float spawnScale = 0.12f;

    [Header("Light toggle")]
    public Light mainDirectionalLight;

    // internal
    private InputDevice leftDevice;
    private InputDevice rightDevice;
    private bool leftTriggerPrev = false;
    private bool rightTriggerPrev = false;
    private bool leftGripPrev = false;
    private bool rightGripPrev = false;
    private bool leftPrimaryPrev = false;
    private bool rightPrimaryPrev = false;

    // keep last spawned per hand so grip can change color
    private Dictionary<XRNode, GameObject> lastSpawned = new Dictionary<XRNode, GameObject>();

    void Start()
    {
        TryInitDevices();
        if (spawnPrefab == null)
            Debug.LogError("spawnPrefab not assigned in VRSimpleInteractions.");
    }

    void TryInitDevices()
    {
        leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    void Update()
    {
        // if devices are not valid, try reinit
        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            TryInitDevices();
        }

        // --- RIGHT HAND ---
        if (rightDevice.isValid)
        {
            // trigger (spawn)
            bool triggerVal = false;
            if (rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerVal) && triggerVal && !rightTriggerPrev)
            {
                SpawnForDevice(rightDevice, XRNode.RightHand);
            }
            rightTriggerPrev = triggerVal;

            // grip (change color)
            bool gripVal = false;
            if (rightDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripVal) && gripVal && !rightGripPrev)
            {
                ToggleColorLast(XRNode.RightHand);
            }
            rightGripPrev = gripVal;

            // primary (toggle light)
            bool primaryVal = false;
            if (rightDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryVal) && primaryVal && !rightPrimaryPrev)
            {
                ToggleLight();
            }
            rightPrimaryPrev = primaryVal;
        }

        // --- LEFT HAND ---
        if (leftDevice.isValid)
        {
            bool triggerVal = false;
            if (leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerVal) && triggerVal && !leftTriggerPrev)
            {
                SpawnForDevice(leftDevice, XRNode.LeftHand);
            }
            leftTriggerPrev = triggerVal;

            bool gripVal = false;
            if (leftDevice.TryGetFeatureValue(CommonUsages.gripButton, out gripVal) && gripVal && !leftGripPrev)
            {
                ToggleColorLast(XRNode.LeftHand);
            }
            leftGripPrev = gripVal;

            bool primaryVal = false;
            if (leftDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryVal) && primaryVal && !leftPrimaryPrev)
            {
                ToggleLight();
            }
            leftPrimaryPrev = primaryVal;
        }
    }

    void SpawnForDevice(InputDevice device, XRNode node)
    {
        // get position & rotation from device
        Vector3 pos;
        Quaternion rot;
        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out pos))
        {
            // fallback: use camera position in front if device pos unavailable
            var cam = Camera.main;
            pos = cam != null ? cam.transform.position + cam.transform.forward * 0.5f : Vector3.zero;
        }
        if (!device.TryGetFeatureValue(CommonUsages.deviceRotation, out rot))
        {
            rot = Quaternion.identity;
        }

        Vector3 spawnPos = pos + rot * Vector3.forward * spawnOffset;
        GameObject go = Instantiate(spawnPrefab, spawnPos, rot);
        go.transform.localScale = Vector3.one * spawnScale;

        // ensure Rigidbody exists
        Rigidbody rb = go.GetComponent<Rigidbody>();
        if (rb == null) rb = go.AddComponent<Rigidbody>();

        // try set initial velocity based on controller velocity (so it can be thrown)
        Vector3 vel;
        if (device.TryGetFeatureValue(CommonUsages.deviceVelocity, out vel))
        {
            rb.velocity = vel;
        }

        // keep last spawned reference
        lastSpawned[node] = go;
    }

    void ToggleColorLast(XRNode node)
    {
        if (lastSpawned.ContainsKey(node) && lastSpawned[node] != null)
        {
            var go = lastSpawned[node];
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                // random color pick (keeps material instance)
                if (mr.sharedMaterial != null)
                {
                    Material m = new Material(mr.sharedMaterial);
                    m.color = Random.ColorHSV(0f,1f,0.5f,1f,0.7f,1f);
                    mr.material = m;
                }
            }
        }
    }

    void ToggleLight()
    {
        if (mainDirectionalLight != null)
        {
            mainDirectionalLight.enabled = !mainDirectionalLight.enabled;
        }
    }
}

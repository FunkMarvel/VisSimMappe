using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class UIManager : MonoBehaviour
{
    [Header("Surface")] [SerializeField] private GameObject surfaceObj;
    [SerializeField] private GameObject startRainButton;
    private TriangleSurface _surface;

    [Header("PointCloud")] [SerializeField]
    private GameObject pointCloudObj;
    [SerializeField] private GameObject cloudSwitchButton;
    
    private PointCloud _pointCloud;

    private readonly Dictionary<string, bool> activeTable = new Dictionary<string, bool>();

    private void Start()
    {
        activeTable["surface"] = surfaceObj != null;
        if (activeTable["surface"])
        {
            _surface = surfaceObj.GetComponent<TriangleSurface>();
            activeTable["surface"] = _surface != null;
            if (activeTable["surface"]) surfaceObj.SetActive(false);
        }

        activeTable["rainButton"] = startRainButton != null;
        if (activeTable["rainButton"])
        {
            startRainButton.SetActive(false);
        }

        activeTable["cloudSwitch"] = cloudSwitchButton != null;
        if (activeTable["cloudSwitch"])
        {
            cloudSwitchButton.SetActive(false);
        }

        activeTable["pointCloud"] = pointCloudObj != null;
        if (!activeTable["pointCloud"]) return;
        _pointCloud = pointCloudObj.GetComponent<PointCloud>();
        
        activeTable["pointCloud"] = _pointCloud != null;
        if (activeTable["pointCloud"]) pointCloudObj.SetActive(false);
    }

    public void ToggleCloud()
    {
        if (!activeTable["pointCloud"]) return;
        
        var toggle = !pointCloudObj.activeInHierarchy;
        pointCloudObj.SetActive(toggle);
        
        if (activeTable["cloudSwitch"]) cloudSwitchButton.SetActive(toggle);
    }

    public void ToggleSurface()
    {
        if (!activeTable["surface"]) return;
        
        var toggle = !surfaceObj.activeInHierarchy;
        surfaceObj.SetActive(toggle);
        
        if (activeTable["rainButton"]) startRainButton.SetActive(toggle);
    }
}

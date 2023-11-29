// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////
// //FileName: UIManager.cs
// //FileType: Visual C# Source file
// //Author : Anders P. Åsbø
// //Created On : 28/11/2023
// //Last Modified On : 29/11/2023
// //Copy Rights : Anders P. Åsbø
// //Description : Handles UI and interactivity
// //////////////////////////////////////////////////////////////////////////
// //////////////////////////////

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Cameras")] [SerializeField] private Camera wideViewCamera;
    [SerializeField] private Camera closeViewCamera;
    [SerializeField] private Camera followCamera;
    [Header("Ball")] [SerializeField] private GameObject rainManagerObj;

    [Header("Surface")] [SerializeField] private GameObject surfaceObj;
    [SerializeField] private GameObject startRainButton;

    [Header("PointCloud")] [SerializeField]
    private GameObject pointCloudObj;

    [SerializeField] private GameObject cloudSwitchButton;

    private readonly Dictionary<string, bool> notNullTable = new();
    private Camera _camera;
    private bool _chasing;

    private PointCloud _pointCloud;

    private GameObject _pointerSphere;
    private RainManager _rainManager;
    private TriangleSurface _surface;

    private void Start()
    {
        notNullTable["camera"] = wideViewCamera != null && closeViewCamera != null && followCamera != null;
        if (notNullTable["camera"])
        {
            closeViewCamera.enabled = true;
            wideViewCamera.enabled = false;
            followCamera.rect = new Rect(1f - 0.3f, 0.125f, 0.25f, 0.25f);
            followCamera.depth = closeViewCamera.depth > wideViewCamera.depth
                ? closeViewCamera.depth + 1
                : wideViewCamera.depth + 1;
            followCamera.enabled = false;
        }

        _camera = notNullTable["camera"] ? closeViewCamera : Camera.main;

        notNullTable["rainManager"] = rainManagerObj != null;
        if (notNullTable["rainManager"]) _rainManager = rainManagerObj.GetComponent<RainManager>();
        notNullTable["rainManager"] = _rainManager != null;

        notNullTable["surface"] = surfaceObj != null;
        if (notNullTable["surface"])
        {
            _surface = surfaceObj.GetComponent<TriangleSurface>();
            notNullTable["surface"] = _surface != null;
            if (notNullTable["surface"]) surfaceObj.SetActive(false);
        }

        notNullTable["rainButton"] = startRainButton != null;
        if (notNullTable["rainButton"]) startRainButton.SetActive(false);

        notNullTable["cloudSwitch"] = cloudSwitchButton != null;
        if (notNullTable["cloudSwitch"]) cloudSwitchButton.SetActive(false);

        notNullTable["pointCloud"] = pointCloudObj != null;
        if (!notNullTable["pointCloud"]) return;
        _pointCloud = pointCloudObj.GetComponent<PointCloud>();

        notNullTable["pointCloud"] = _pointCloud != null;
        if (notNullTable["pointCloud"]) pointCloudObj.SetActive(false);

        _pointerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _pointerSphere.transform.localScale = new Vector3(4f, 4f, 4f);
    }

    private void Update()
    {
        if (!notNullTable["camera"] && !notNullTable["surface"] && !notNullTable["rainManager"]) return;
        if (_rainManager.SimStarted && _pointerSphere.activeInHierarchy) _pointerSphere.SetActive(false);

        var ray = _camera.ScreenPointToRay(Input.mousePosition);
        if (!_chasing && Physics.Raycast(ray, out var hit))
        {
            var pos = hit.point;
            var onSurface = _surface.GetCollision(pos, false);
            pos = onSurface.Point + Vector3.up * 10f;

            if (!_chasing)
            {
                followCamera.transform.position = pos + (0.5f * Vector3.up + 0.5f * Vector3.back).normalized * 20f;
                followCamera.transform.LookAt(pos, (0.5f * Vector3.up + 0.5f * Vector3.forward).normalized);
            }

            _pointerSphere.transform.position = pos;

            if (Input.GetKeyDown(KeyCode.Mouse0)) _rainManager.SpawnBall(pos);
            if (!_chasing && _rainManager.HasBall) _chasing = true;
        }

        if (!_rainManager.HasBall) return;
        var position = _rainManager.Ball.transform.position;
        followCamera.transform.position = position + (0.5f * Vector3.up + 0.5f * Vector3.back).normalized * 30f;
        followCamera.transform.LookAt(position, (0.5f * Vector3.up + 0.5f * Vector3.back).normalized);

        closeViewCamera.transform.LookAt(position);
    }

    public void ToggleCloud()
    {
        if (!notNullTable["pointCloud"]) return;

        var toggle = !pointCloudObj.activeInHierarchy;
        pointCloudObj.SetActive(toggle);

        if (notNullTable["cloudSwitch"]) cloudSwitchButton.SetActive(toggle);
    }

    public void ToggleSurface()
    {
        if (!notNullTable["surface"]) return;

        var toggle = !surfaceObj.activeInHierarchy;
        surfaceObj.SetActive(toggle);

        if (notNullTable["camera"]) followCamera.enabled = toggle;
        if (notNullTable["rainButton"]) startRainButton.SetActive(toggle);
    }

    public void ToggleCamera()
    {
        if (!notNullTable["camera"]) return;

        if (closeViewCamera.enabled)
        {
            closeViewCamera.enabled = false;
            wideViewCamera.enabled = true;
            _camera = wideViewCamera;
        }
        else
        {
            closeViewCamera.enabled = true;
            wideViewCamera.enabled = false;
            _camera = closeViewCamera;
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene("Scenes/FullSurface");
    }
}
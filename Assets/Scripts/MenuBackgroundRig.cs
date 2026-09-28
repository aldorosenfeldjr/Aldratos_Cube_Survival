using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Renders a small diorama into a low-resolution RenderTexture behind the Main Menu's UI — the reduced
/// resolution is the "out of focus" look, cheap on every platform (no real-time depth-of-field). Own root
/// object in Core, like <c>TimeScaleController</c>: never needs to survive a scene reload, since menu
/// navigation never leaves Core. A slow idle drift plays under whichever named pose is current;
/// <see cref="MoveTo"/> eases to a new pose, cancelling any pose tween already in flight.
/// </summary>
public class MenuBackgroundRig : MonoBehaviour
{
    [System.Serializable]
    public struct Pose
    {
        public Vector3 position;
        public Vector3 eulerAngles;
        public float fieldOfView;
    }

    [SerializeField] private GameObject stagePrefab;
    [SerializeField] private RawImage backgroundImage;
    [Tooltip("UI shader that shows the render through a soft blur (UI/MenuBackgroundBlur).")]
    [SerializeField] private Shader blurShader;
    [SerializeField, Range(0.5f, 4f)] private float blurRadius = 1.8f;
    [SerializeField, Range(2, 8)] private int downscaleFactor = 4;
    [SerializeField] private float poseTweenDuration = 0.7f;
    [SerializeField] private Pose mainMenuIdle = new Pose { position = new Vector3(0f, 1.6f, -6f), eulerAngles = new Vector3(6f, 0f, 0f), fieldOfView = 32f };
    [SerializeField] private Pose charactersOpen = new Pose { position = new Vector3(2.4f, 1.4f, -4.6f), eulerAngles = new Vector3(8f, -18f, 0f), fieldOfView = 28f };
    [SerializeField] private Pose companionsOpen = new Pose { position = new Vector3(-2.4f, 1.2f, -4.6f), eulerAngles = new Vector3(8f, 18f, 0f), fieldOfView = 28f };
    [SerializeField] private float driftAmplitude = 0.15f;
    [SerializeField] private float driftSpeed = 0.15f;

    // Far from the play area so the camera's frustum never picks up real level/hazard geometry from whichever
    // level scenes happen to be additively loaded alongside Core (the camera has no culling mask restriction,
    // same reasoning as SelectionScreen's own far-away preview stage).
    private static readonly Vector3 StagePosition = new Vector3(1000f, 0f, 0f);

    public static MenuBackgroundRig Instance { get; private set; }

    /// <summary>"MainMenu", "Characters" or "Companions": the pose the camera is at or easing to.</summary>
    public string CurrentPoseName { get; private set; } = "MainMenu";
    public float CameraFieldOfView => stageCamera.fieldOfView;
    public bool CameraEnabled => stageCamera.enabled;

    public RenderTexture Texture => texture;
    public bool TextureWired => stageCamera.targetTexture == texture && backgroundImage.texture == texture;

    private Camera stageCamera;
    private Material blurMaterial;
    private RenderTexture texture;
    private Vector2Int textureScreenSize;
    private Pose basePose;

    private void Awake()
    {
        Instance = this;
        transform.position = StagePosition;
        Instantiate(stagePrefab, transform);

        var cameraObject = new GameObject("MenuBackgroundCamera");
        cameraObject.transform.SetParent(transform, false);
        stageCamera = cameraObject.AddComponent<Camera>();
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = new Color(0.1f, 0.12f, 0.2f, 1f);
        stageCamera.nearClipPlane = 0.3f;
        stageCamera.farClipPlane = 60f;
        stageCamera.allowHDR = false;
        stageCamera.allowMSAA = false;

        if (blurShader != null)
        {
            blurMaterial = new Material(blurShader) { name = "MenuBackgroundBlur (runtime)" };
            blurMaterial.SetFloat("_Radius", blurRadius);
            backgroundImage.material = blurMaterial;
        }

        RebuildTexture(new Vector2Int(Screen.width, Screen.height));
        SetPoseImmediate(mainMenuIdle);
    }

    private void OnEnable()
    {
        StartDrift();
    }

    // Other screens hide the menu background (a run starts, for instance): stop rendering the diorama then, it would be wasted GPU work.
    private void LateUpdate()
    {
        stageCamera.enabled = backgroundImage != null && backgroundImage.isActiveAndEnabled;
        if (stageCamera.enabled)
        {
            EnsureTextureSize(new Vector2Int(Screen.width, Screen.height));
        }
    }

    private void OnDisable()
    {
        LeanTween.cancel(gameObject);
        if (stageCamera != null)
        {
            LeanTween.cancel(stageCamera.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (backgroundImage != null)
        {
            backgroundImage.texture = null;
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }
        if (blurMaterial != null)
        {
            Destroy(blurMaterial);
        }
    }

    /// <summary>
    /// The app auto-rotates: a texture built for one orientation would be stretched across the other. Rebuilds it (releasing the
    /// old one) when the screen's size has changed since it was made.
    /// </summary>
    public void EnsureTextureSize(Vector2Int screenSize)
    {
        if (screenSize != textureScreenSize)
        {
            RebuildTexture(screenSize);
        }
    }

    private void RebuildTexture(Vector2Int screenSize)
    {
        stageCamera.targetTexture = null;
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }

        var width = Mathf.Max(4, screenSize.x / downscaleFactor);
        var height = Mathf.Max(4, screenSize.y / downscaleFactor);
        texture = new RenderTexture(width, height, 16) { name = "MenuBackgroundTexture", filterMode = FilterMode.Bilinear };
        stageCamera.targetTexture = texture;
        backgroundImage.texture = texture;
        textureScreenSize = screenSize;
    }

    /// <summary>Eases to the given pose from wherever the camera is right now (even mid-tween), cancelling any tween already running.</summary>
    public void MoveTo(Pose pose)
    {
        LeanTween.cancel(gameObject);
        var start = basePose;
        LeanTween.value(gameObject, 0f, 1f, poseTweenDuration)
            .setEaseInOutSine()
            .setOnUpdate((float t) => basePose = LerpPose(start, pose, t));
    }

    public void MoveToMainMenu() => MoveTo(mainMenuIdle, "MainMenu");
    public void MoveToCharacters() => MoveTo(charactersOpen, "Characters");
    public void MoveToCompanions() => MoveTo(companionsOpen, "Companions");

    private void MoveTo(Pose pose, string poseName)
    {
        CurrentPoseName = poseName;
        MoveTo(pose);
    }

    private void SetPoseImmediate(Pose pose)
    {
        basePose = pose;
    }

    private static Pose LerpPose(Pose a, Pose b, float t)
    {
        return new Pose
        {
            position = Vector3.Lerp(a.position, b.position, t),
            eulerAngles = Vector3.Lerp(a.eulerAngles, b.eulerAngles, t),
            fieldOfView = Mathf.Lerp(a.fieldOfView, b.fieldOfView, t),
        };
    }

    private void StartDrift()
    {
        LeanTween.value(stageCamera.gameObject, 0f, 1f, 1f)
            .setLoopClamp()
            .setOnUpdate((float unused) =>
            {
                var t = Time.unscaledTime * driftSpeed;
                var drift = new Vector3(Mathf.Sin(t) * driftAmplitude, Mathf.Sin(t * 0.7f) * driftAmplitude * 0.5f, 0f);
                stageCamera.transform.localPosition = basePose.position + drift;
                stageCamera.transform.localEulerAngles = basePose.eulerAngles;
                stageCamera.fieldOfView = basePose.fieldOfView;
            });
    }
}

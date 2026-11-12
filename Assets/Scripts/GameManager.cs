using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>
/// Owns asset loading, board generation, input routing and the match flow
/// (selection, resolution, presentation, continuation, completion).
/// Tile-local visuals live in <see cref="TileController"/>; the correct-match UI in <see cref="MatchPresentationView"/>.
/// </summary>
public class GameManager : MonoBehaviour
{
    private struct PointerGesture
    {
        public bool Active;
        public int DeviceId;
        public Vector2 StartPosition;
        public TileController Tile;
        public bool ForContinue;
    }

    [Header("Board")]
    [Tooltip("Parent for generated tiles. Falls back to the scene object named \"Board Root\".")]
    [SerializeField] private Transform boardRoot;
    [Min(0.01f)] public float tileWidth = 1f;
    [Min(0.01f)] public float tileHeight = 1f;
    [Min(0f)] public float tileSpacing = 0.2f;

    [Header("Camera")]
    [Tooltip("Gameplay camera. Falls back to Camera.main.")]
    [SerializeField] private Camera gameplayCamera;
    [Tooltip("World-space margin kept around the board when framing it.")]
    [Min(0f)] [SerializeField] private float boardFramingPadding = 0.4f;

    [Header("Input")]
    [Tooltip("Board/PointerPosition action. When unassigned a runtime <Pointer>/position action is used.")]
    [SerializeField] private InputActionReference pointerPositionAction;
    [Tooltip("Board/PrimaryPress action. When unassigned a runtime <Pointer>/press action is used.")]
    [SerializeField] private InputActionReference primaryPressAction;
    [SerializeField] private LayerMask tileLayerMask = Physics.DefaultRaycastLayers;
    [Tooltip("Maximum pointer travel between press and release for a tap, in inches.")]
    [Min(0.01f)] [SerializeField] private float tapMaxMovementInches = 0.2f;
    [Tooltip("DPI assumed when the display does not report one.")]
    [Min(1f)] [SerializeField] private float fallbackDpi = 160f;

    [Header("Animation")]
    [SerializeField] private TileAnimationSettings animationSettings = new TileAnimationSettings();

    [Header("Presentation")]
    [Tooltip("Optional. Without it, matched tiles are still presented and continued, but no description UI is shown.")]
    [SerializeField] private MatchPresentationView presentationView;

    [Header("Blocked input feedback")]
    [Tooltip("Short device vibration when the player taps while input is locked. Visual state remains the primary signal.")]
    [SerializeField] private bool blockedInputHaptics = true;
    [Min(0.1f)] [SerializeField] private float blockedFeedbackCooldown = 1f;

    [Header("Events")]
    [SerializeField] private UnityEvent onBoardCompleted = new UnityEvent();

    /// <summary>Raised exactly once per board, after the final pair returns.</summary>
    public event Action BoardCompleted;

    private readonly MatchFlow<TileController> flow = new MatchFlow<TileController>();
    private readonly List<TileController> boardTiles = new List<TileController>();
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private AsyncOperationHandle<GameObject> tilePrefabHandle;
    private AsyncOperationHandle<IList<Tile>> tileVariationsHandle;
    private GameObject tilePrefab;
    private List<Tile> tileVariations;
    private bool isSetUp;
    private bool setupFinished;
    private bool isDestroyed;
    private float boardLocalZ = 0f;
    private int boardSize;

    private InputAction positionAction;
    private InputAction pressAction;
    private bool ownsInputActions;
    private PointerGesture gesture;
    private PointerEventData uiPointerData;
    private EventSystem uiPointerDataOwner;
    private bool continueArmed;
    private float nextBlockedFeedbackTime;

    private Coroutine revealRoutine;
    private Coroutine sequenceRoutine;

    private Vector2Int framedScreenSize;
    private Rect framedSafeArea;
    private bool framingDirty;

    public MatchFlowState State => flow.State;
    public int ConfirmedPairs => flow.ConfirmedPairs;
    public int PairCount => flow.PairCount;

    // ---- Lifecycle -------------------------------------------------------------------------

    private void Awake()
    {
        animationSettings.Validate();
        BeginSetup();
    }

    private void OnValidate()
    {
        if(animationSettings == null) animationSettings = new TileAnimationSettings();
        animationSettings.Validate();
    }

    private void OnEnable()
    {
        ResolveInputActions();
        if(pressAction == null) return;
        pressAction.performed += OnPrimaryPressed;
        pressAction.canceled += OnPrimaryReleased;
        pressAction.Enable();
        positionAction?.Enable();
    }

    private void OnDisable()
    {
        if(pressAction != null)
        {
            pressAction.performed -= OnPrimaryPressed;
            pressAction.canceled -= OnPrimaryReleased;
            pressAction.Disable();
            positionAction?.Disable();
        }

        gesture = default;
        CancelActiveSequence(confirmPendingMatch: true, raiseCompletion: !isDestroyed);
    }

    private void OnDestroy()
    {
        isDestroyed = true;

        if(ownsInputActions)
        {
            pressAction?.Dispose();
            positionAction?.Dispose();
            pressAction = null;
            positionAction = null;
        }

        if(tilePrefabHandle.IsValid()) Addressables.Release(tilePrefabHandle);
        if(tileVariationsHandle.IsValid()) Addressables.Release(tileVariationsHandle);
        tilePrefabHandle = default;
        tileVariationsHandle = default;
    }

    private void OnApplicationPause(bool paused)
    {
        if(!paused) return;
        // Drop any half-finished gesture and require a fresh press after resuming.
        gesture = default;
        continueArmed = false;
        nextBlockedFeedbackTime = 0f;
    }

    private void Update()
    {
        if(flow.State == MatchFlowState.AwaitingContinue && !continueArmed && !AnyPointerHeld())
        {
            continueArmed = true;
        }
    }

    private void LateUpdate()
    {
        if(boardTiles.Count == 0) return;

        Vector2Int size = new Vector2Int(Screen.width, Screen.height);
        if(size != framedScreenSize || Screen.safeArea != framedSafeArea) framingDirty = true;

        // Keep the camera static while a pair is being resolved or presented.
        if(framingDirty && (flow.AcceptsSelection || flow.State == MatchFlowState.Complete))
        {
            FrameBoard();
        }
    }

    // ---- Setup -----------------------------------------------------------------------------

    private void BeginSetup()
    {
        flow.ResetToLoading();

        if(boardRoot == null)
        {
            GameObject found = GameObject.Find(Constants.GameObjectNames.BoardRoot);
            if(found != null) boardRoot = found.transform;
        }

        if(gameplayCamera == null) gameplayCamera = Camera.main;

        if(boardRoot == null)
        {
            FailSetup($"No board root assigned and no scene object named \"{Constants.GameObjectNames.BoardRoot}\" was found.");
            return;
        }

        if(gameplayCamera == null)
        {
            FailSetup("No gameplay camera assigned and no camera is tagged MainCamera.");
            return;
        }

        if(tileLayerMask.value == 0)
        {
            FailSetup("Tile layer mask is empty, so tiles could never be selected.");
            return;
        }

        tilePrefabHandle = Addressables.LoadAssetAsync<GameObject>(Constants.AddressablePrefabs.Tile);
        tileVariationsHandle = Addressables.LoadAssetsAsync<Tile>(Constants.AddressableLabels.Tile);
        tilePrefabHandle.Completed += _ => TryFinishSetup();
        tileVariationsHandle.Completed += _ => TryFinishSetup();
    }

    private void TryFinishSetup()
    {
        if(isDestroyed || setupFinished) return;
        if(!tilePrefabHandle.IsValid() || !tileVariationsHandle.IsValid()) return;
        if(!tilePrefabHandle.IsDone || !tileVariationsHandle.IsDone) return;
        setupFinished = true;

        if(tilePrefabHandle.Status != AsyncOperationStatus.Succeeded || tilePrefabHandle.Result == null)
        {
            FailSetup($"Failed to load the tile prefab \"{Constants.AddressablePrefabs.Tile}\": {tilePrefabHandle.OperationException}");
            return;
        }

        if(tileVariationsHandle.Status != AsyncOperationStatus.Succeeded || tileVariationsHandle.Result == null)
        {
            FailSetup($"Failed to load tiles with label \"{Constants.AddressableLabels.Tile}\": {tileVariationsHandle.OperationException}");
            return;
        }

        GameObject prefab = tilePrefabHandle.Result;
        if(prefab.GetComponent<TileView>() == null || prefab.GetComponent<TileController>() == null)
        {
            FailSetup("The tile prefab is missing its TileView or TileController component.");
            return;
        }

        List<Tile> variations = tileVariationsHandle.Result.Where(variation => variation != null).ToList();
        if(variations.Count == 0)
        {
            FailSetup($"No tile variations were loaded for label \"{Constants.AddressableLabels.Tile}\".");
            return;
        }

        tilePrefab = prefab;
        tileVariations = variations;
        isSetUp = true;

        //TODO: REMOVE THIS
        GenerateMemoryGrid(Random.Range(1, 3) * 2);
    }

    private void FailSetup(string reason)
    {
        isSetUp = false;
        flow.Fail();
        Debug.LogError($"GameManager setup failed: {reason} The board will stay non-interactive.", this);
    }

    //TODO: a small menu system (unity input system)
    //       - Title Screen (with title) -> PLAY, SETTINGS, EXIT
    //       - Grid Size Picker (after PLAY button) -> a grid with the max size where the player has to drag with his fingers to draw the size they want
    //           - holding and dragging begins activating grid tiles, letting go (unless it's too small of a selection) starts the game
    //       - Pause Menu -> CONTINUE, MAIN MENU

    // ---- Board generation ------------------------------------------------------------------

    public void GenerateMemoryGrid(int size)
    {
        if(!isSetUp)
        {
            Debug.LogError("Grid generation was called, but the GameManager was not set up yet. Aborting.");
            return;
        }

        int totalTiles = size * size;

        if(size <= 0 || totalTiles % 2 != 0)
        {
            Debug.LogError($"With the received size of {size}, we cannot create a grid with an even number of nodes.");
            return;
        }

        int pairCount = totalTiles / 2;

        if(pairCount > tileVariations.Count)
        {
            Debug.LogError($"Cannot create the requested {pairCount} because there are only {tileVariations.Count} tile variations");
            return;
        }

        List<Tile> tilePool = tileVariations.ToList();
        List<Tile> selectedTiles = new List<Tile>();

        for(int i = 0; i < pairCount; i++)
        {
            int j = Random.Range(0, tilePool.Count);
            selectedTiles.Add(tilePool[j]);
            tilePool.RemoveAt(j);
        }

        // Double the list, so it doesn't relate to couples anymore, but individual tile possibilities.
        selectedTiles.AddRange(selectedTiles.ToList());
        // Turn the list into a stack, so we can use it more easily later in the variation assignment section.
        Stack<Tile> tileVariationStack = selectedTiles.ToShuffledStack();

        // Stop anything running on the old board before its tiles are destroyed.
        CancelActiveSequence(confirmPendingMatch: false, raiseCompletion: false);
        flow.ResetToLoading();
        ClearBoard();

        float stepX = tileWidth + tileSpacing;
        float stepY = tileHeight + tileSpacing;
        float centreOffset = (size - 1) * 0.5f;

        for(int row = 0; row < size; row++)
        {
            for(int column = 0; column < size; column++)
            {
                float x = (column - centreOffset) * stepX;
                float y = (row - centreOffset) * stepY;
                GameObject tileObject = Instantiate(tilePrefab, boardRoot, false);
                tileObject.transform.localPosition = new Vector3(x, y, boardLocalZ);
                TileView tileView = tileObject.GetComponent<TileView>();
                TileController tileController = tileObject.GetComponent<TileController>();

                if(tileView == null || tileController == null)
                {
                    Destroy(tileObject);
                    ClearBoard();
                    FailSetup("Tile prefab is missing a necessary component. Aborting grid generation.");
                    return;
                }

                tileController.Setup(tileVariationStack.Pop());
                tileView.SetSize(tileWidth, tileHeight);
                tileController.CaptureBoardPose();
                boardTiles.Add(tileController);
            }
        }

        boardSize = size;
        FrameBoard();
        flow.StartBoard(pairCount);
    }

    private void ClearBoard()
    {
        boardTiles.Clear();
        boardSize = 0;
        if(boardRoot == null) return;

        List<GameObject> oldTiles = new List<GameObject>();
        foreach(Transform child in boardRoot) oldTiles.Add(child.gameObject);
        foreach(GameObject oldTile in oldTiles)
        {
            // Deactivate first so the destroyed tiles cannot be raycast during the rest of this frame.
            oldTile.SetActive(false);
            Destroy(oldTile);
        }
    }

    // ---- Camera framing --------------------------------------------------------------------

    private void FrameBoard()
    {
        framingDirty = false;
        framedScreenSize = new Vector2Int(Screen.width, Screen.height);
        framedSafeArea = Screen.safeArea;
        if(gameplayCamera == null || boardRoot == null || boardSize <= 0) return;

        Vector3 lossy = boardRoot.lossyScale;
        float stepX = tileWidth + tileSpacing;
        float stepY = tileHeight + tileSpacing;
        Vector2 boardExtent = new Vector2(
            (boardSize * stepX - tileSpacing + boardFramingPadding * 2f) * Mathf.Abs(lossy.x),
            (boardSize * stepY - tileSpacing + boardFramingPadding * 2f) * Mathf.Abs(lossy.y));

        Rect safe = Screen.safeArea;
        Rect normalisedSafe = Screen.width > 0 && Screen.height > 0
            ? new Rect(safe.x / Screen.width, safe.y / Screen.height, safe.width / Screen.width, safe.height / Screen.height)
            : new Rect(0f, 0f, 1f, 1f);

        BoardFraming.Result fit = BoardFraming.Fit(boardExtent, gameplayCamera.aspect, gameplayCamera.fieldOfView, gameplayCamera.orthographic, normalisedSafe);
        Transform cameraTransform = gameplayCamera.transform;
        Vector3 forward = cameraTransform.forward;
        float distance;

        if(gameplayCamera.orthographic)
        {
            gameplayCamera.orthographicSize = fit.OrthographicSize;
            distance = Vector3.Dot(boardRoot.position - cameraTransform.position, forward);
            if(distance <= gameplayCamera.nearClipPlane) distance = 10f;
        }
        else
        {
            distance = fit.Distance;
        }

        cameraTransform.position = boardRoot.position - forward * distance
            + cameraTransform.right * fit.LateralOffset.x
            + cameraTransform.up * fit.LateralOffset.y;

        if(gameplayCamera.farClipPlane < distance + 5f) gameplayCamera.farClipPlane = distance + 5f;
    }

    private float BoardDistance()
    {
        Transform cameraTransform = gameplayCamera.transform;
        return Vector3.Dot(boardRoot.position - cameraTransform.position, cameraTransform.forward);
    }

    private float TileWorldWidth => tileWidth * Mathf.Abs(boardRoot.lossyScale.x);
    private float TileWorldHeight => tileHeight * Mathf.Abs(boardRoot.lossyScale.y);

    // ---- Input -----------------------------------------------------------------------------

    private void ResolveInputActions()
    {
        if(pressAction != null) return;

        InputAction press = primaryPressAction != null ? primaryPressAction.action : null;
        InputAction position = pointerPositionAction != null ? pointerPositionAction.action : null;

        if(press != null)
        {
            pressAction = press;
            positionAction = position;
            ownsInputActions = false;
            return;
        }

        Debug.LogWarning("GameManager has no board press action assigned; using a runtime <Pointer> fallback. Assign the Board action map in the Inspector.", this);
        pressAction = new InputAction("BoardPrimaryPress", InputActionType.Button, "<Pointer>/press");
        positionAction = new InputAction("BoardPointerPosition", InputActionType.Value, "<Pointer>/position");
        ownsInputActions = true;
    }

    private void OnPrimaryPressed(InputAction.CallbackContext context)
    {
        InputDevice device = context.control != null ? context.control.device : null;
        if(device == null) return;

        // One gameplay pointer at a time: other contacts are ignored until it is released.
        if(gesture.Active) return;

        Vector2 position = ReadPointerPosition(device);

        if(flow.State == MatchFlowState.AwaitingContinue)
        {
            // Only a fresh press made after every earlier pointer was released can continue.
            if(!continueArmed) return;
            gesture = new PointerGesture { Active = true, DeviceId = device.deviceId, StartPosition = position, ForContinue = true };
            return;
        }

        if(!flow.AcceptsSelection)
        {
            if(flow.IsLocked && flow.State != MatchFlowState.Complete) TriggerBlockedFeedback();
            return;
        }

        if(IsPointerOverBlockingUI(position)) return;

        gesture = new PointerGesture
        {
            Active = true,
            DeviceId = device.deviceId,
            StartPosition = position,
            Tile = RaycastTile(position),
        };
    }

    private void OnPrimaryReleased(InputAction.CallbackContext context)
    {
        InputDevice device = context.control != null ? context.control.device : null;
        if(!gesture.Active || device == null || device.deviceId != gesture.DeviceId) return;

        PointerGesture completed = gesture;
        gesture = default;
        Vector2 position = ReadPointerPosition(device);

        if(completed.ForContinue)
        {
            if(flow.State == MatchFlowState.AwaitingContinue && continueArmed) ContinueFromPresentation();
            return;
        }

        if(completed.Tile == null || !flow.AcceptsSelection) return;

        float maxMovement = tapMaxMovementInches * (Screen.dpi > 0f ? Screen.dpi : fallbackDpi);
        if((position - completed.StartPosition).sqrMagnitude > maxMovement * maxMovement) return;
        if(IsPointerOverBlockingUI(position)) return;
        if(RaycastTile(position) != completed.Tile) return;

        HandleTileTap(completed.Tile);
    }

    private Vector2 ReadPointerPosition(InputDevice device)
    {
        if(device is Pointer pointer) return pointer.position.ReadValue();
        return positionAction != null ? positionAction.ReadValue<Vector2>() : Vector2.zero;
    }

    private TileController RaycastTile(Vector2 screenPosition)
    {
        if(gameplayCamera == null) return null;
        Ray ray = gameplayCamera.ScreenPointToRay(screenPosition);
        if(!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, tileLayerMask, QueryTriggerInteraction.Collide)) return null;
        TileController tile = hit.collider.GetComponentInParent<TileController>();
        return tile != null && boardTiles.Contains(tile) ? tile : null;
    }

    private bool IsPointerOverBlockingUI(Vector2 screenPosition)
    {
        EventSystem eventSystem = EventSystem.current;
        if(eventSystem == null) return false;
        if(uiPointerData == null || uiPointerDataOwner != eventSystem)
        {
            uiPointerData = new PointerEventData(eventSystem);
            uiPointerDataOwner = eventSystem;
        }
        uiPointerData.position = screenPosition;
        uiRaycastResults.Clear();
        eventSystem.RaycastAll(uiPointerData, uiRaycastResults);

        // Only uGUI graphics block the board; a PhysicsRaycaster hitting tiles must not count.
        foreach(RaycastResult result in uiRaycastResults)
        {
            if(result.module is GraphicRaycaster) return true;
        }
        return false;
    }

    private bool AnyPointerHeld()
    {
        if(pressAction != null && pressAction.IsPressed()) return true;

        Touchscreen touchscreen = Touchscreen.current;
        if(touchscreen != null)
        {
            foreach(var touch in touchscreen.touches)
            {
                if(touch.press.isPressed) return true;
            }
        }

        if(Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
        if(Pen.current != null && Pen.current.tip.isPressed) return true;
        return false;
    }

    private void TriggerBlockedFeedback()
    {
        if(!blockedInputHaptics || Time.unscaledTime < nextBlockedFeedbackTime) return;
        nextBlockedFeedbackTime = Time.unscaledTime + blockedFeedbackCooldown;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    // ---- Match flow ------------------------------------------------------------------------

    private void HandleTileTap(TileController tile)
    {
        MatchSelectionResult result = flow.TrySelect(tile);
        int generation = flow.Generation;

        switch(result)
        {
            case MatchSelectionResult.FirstSelected:
                tile.SetRevealed(true);
                revealRoutine = StartCoroutine(RevealFirst(tile));
                break;

            case MatchSelectionResult.Mismatch:
            case MatchSelectionResult.Match:
                StopRoutine(ref revealRoutine);
                sequenceRoutine = StartCoroutine(ResolvePair(flow.First, flow.Second, result == MatchSelectionResult.Match, generation));
                break;
        }
    }

    private IEnumerator RevealFirst(TileController tile)
    {
        tile.IsAnimating = true;
        float startAngle = tile.FlipAngle;
        yield return Animate(animationSettings.flipDuration, animationSettings.flipCurve,
            t => tile.SetFlipAngle(Mathf.Lerp(startAngle, 180f, t)));
        tile.IsAnimating = false;
        revealRoutine = null;
    }

    private IEnumerator ResolvePair(TileController first, TileController second, bool isMatch, int generation)
    {
        TileAnimationSettings s = animationSettings;
        first.IsAnimating = true;
        second.IsAnimating = true;
        second.SetRevealed(true);

        float firstStart = first.FlipAngle;
        float secondStart = second.FlipAngle;
        yield return Animate(s.flipDuration, s.flipCurve, t =>
        {
            first.SetFlipAngle(Mathf.Lerp(firstStart, 180f, t));
            second.SetFlipAngle(Mathf.Lerp(secondStart, 180f, t));
        });

        if(isMatch)
        {
            yield return PresentMatch(first, second, generation);
            yield break;
        }

        yield return Wait(s.mismatchRevealHold);

        float amplitude = s.shakeAmplitude * TileWorldWidth;
        yield return Animate(s.shakeDuration, null, t =>
        {
            float elapsed = t * s.shakeDuration;
            float offset = Mathf.Sin(elapsed * s.shakeFrequency * Mathf.PI * 2f) * amplitude * (1f - t);
            float tint = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI) * 1.5f) * s.incorrectTintStrength;
            first.SetMismatchFeedback(offset, tint, s.incorrectTint);
            second.SetMismatchFeedback(offset, tint, s.incorrectTint);
        });
        first.ClearMismatchFeedback();
        second.ClearMismatchFeedback();

        yield return Animate(s.flipDuration, s.flipCurve, t =>
        {
            first.SetFlipAngle(Mathf.Lerp(180f, 0f, t));
            second.SetFlipAngle(Mathf.Lerp(180f, 0f, t));
        });

        first.SetRevealed(false);
        second.SetRevealed(false);
        first.IsAnimating = false;
        second.IsAnimating = false;
        sequenceRoutine = null;
        flow.CompleteMismatch(generation);
    }

    private IEnumerator PresentMatch(TileController first, TileController second, int generation)
    {
        TileAnimationSettings s = animationSettings;

        // The tile further left on screen goes to the left slot.
        Vector3 firstScreen = gameplayCamera.WorldToScreenPoint(first.transform.position);
        Vector3 secondScreen = gameplayCamera.WorldToScreenPoint(second.transform.position);
        TileController left = firstScreen.x <= secondScreen.x ? first : second;
        TileController right = left == first ? second : first;

        float depth = PresentationDepth(out float scaleMultiplier);
        float presentedHalfWidth = TileWorldWidth * scaleMultiplier * 0.5f;

        if(presentationView != null)
        {
            presentationView.ConfigureDepth(gameplayCamera, depth, BoardDistance());
            presentationView.Show();
        }

        left.SetPresentationFace(true, leftHalf: true);
        right.SetPresentationFace(true, leftHalf: false);
        left.BeginSequenceSegment();
        right.BeginSequenceSegment();

        yield return Animate(s.presentDuration, s.presentCurve, t =>
        {
            left.SetPresentationProgress(t, PresentationTarget(true, depth, presentedHalfWidth, 0f), scaleMultiplier, s.presentSpins);
            right.SetPresentationProgress(t, PresentationTarget(false, depth, presentedHalfWidth, 0f), scaleMultiplier, s.presentSpins);
        });

        yield return Animate(s.joinDuration, s.joinCurve, t =>
        {
            left.SetPresentationPosition(PresentationTarget(true, depth, presentedHalfWidth, t));
            right.SetPresentationPosition(PresentationTarget(false, depth, presentedHalfWidth, t));
        });

        sequenceRoutine = null;
        if(!flow.CompletePresentation(generation)) yield break;

        // Arm continuation only after every pointer from the selection gesture has been released.
        continueArmed = false;
        gesture = default;
        if(presentationView != null) presentationView.SetPromptVisible(true);
    }

    private void ContinueFromPresentation()
    {
        if(!flow.TryContinue()) return;
        continueArmed = false;
        if(presentationView != null) presentationView.Hide();
        sequenceRoutine = StartCoroutine(ReturnMatch(flow.First, flow.Second, flow.Generation));
    }

    private IEnumerator ReturnMatch(TileController first, TileController second, int generation)
    {
        TileAnimationSettings s = animationSettings;
        first.BeginSequenceSegment();
        second.BeginSequenceSegment();

        yield return Animate(s.returnDuration, s.returnCurve, t =>
        {
            first.SetReturnProgress(t);
            second.SetReturnProgress(t);
        });

        first.MarkMatched();
        second.MarkMatched();
        sequenceRoutine = null;

        if(flow.CompleteMatchReturn(generation, out bool boardCompleted) && boardCompleted)
        {
            RaiseBoardCompleted();
        }
    }

    /// <summary>
    /// Depth in front of the camera for presented tiles, kept clear of the near plane and in front of
    /// the board, plus the scale multiplier that gives them the configured on-screen height.
    /// </summary>
    private float PresentationDepth(out float scaleMultiplier)
    {
        TileAnimationSettings s = animationSettings;
        float boardDistance = BoardDistance();
        float depth = s.presentationDepth;
        if(boardDistance > 0f) depth = Mathf.Min(depth, boardDistance * 0.75f);

        scaleMultiplier = PresentationScale(depth);
        float presentedHalfExtent = Mathf.Max(TileWorldWidth, TileWorldHeight) * scaleMultiplier * 0.5f;
        depth = BoardFraming.SafePresentationDepth(depth, gameplayCamera.nearClipPlane, presentedHalfExtent);
        scaleMultiplier = PresentationScale(depth);
        return depth;
    }

    private float PresentationScale(float depth)
    {
        float visibleHeight = BoardFraming.VisibleHeightAtDepth(depth, gameplayCamera.fieldOfView, gameplayCamera.orthographic, gameplayCamera.orthographicSize);
        float tileHeightWorld = Mathf.Max(0.0001f, TileWorldHeight);
        return Mathf.Max(0.01f, visibleHeight * animationSettings.presentationTileScreenHeight / tileHeightWorld);
    }

    /// <summary>
    /// World centre for a presented tile. At <paramref name="join"/> = 0 the tiles are a tile width apart;
    /// at 1 their inner edges meet at the screen centre so the two description halves form one text.
    /// </summary>
    private Vector3 PresentationTarget(bool left, float depth, float presentedHalfWidth, float join)
    {
        Vector3 cameraRight = gameplayCamera.transform.right;
        Vector3 outward = left ? -cameraRight : cameraRight;
        Vector3 centre = gameplayCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
        Vector3 innerEdge = centre + outward * presentedHalfWidth * (1f - join);
        return innerEdge + outward * presentedHalfWidth;
    }

    private void RaiseBoardCompleted()
    {
        Debug.Log("Board completed: every pair has been found.", this);
        onBoardCompleted?.Invoke();
        BoardCompleted?.Invoke();
    }

    //TODO: if all pairs are found and confirmed, the game is over and we return to the main menu
    //       - until this point, the title could be "We Know... Do You?", but once the player succeeds and return to the main menu, momentarily, the title says
    //         "We Know... And Now You Do Too!", before it turns back after a while.

    //TODO: after a certain amount of seconds, if a game is ongoing and there was no input, it automatically resets to the main menu

    //TODO: leaderboard system if we have time and willpower

    // ---- Cancellation ----------------------------------------------------------------------

    /// <summary>
    /// Stops all running sequences, invalidates stale callbacks and restores every tile to a consistent state.
    /// </summary>
    private void CancelActiveSequence(bool confirmPendingMatch, bool raiseCompletion)
    {
        StopRoutine(ref revealRoutine);
        StopRoutine(ref sequenceRoutine);

        TileController first = flow.First;
        TileController second = flow.Second;
        MatchFlowState previous = flow.State;
        flow.Cancel(confirmPendingMatch, out bool boardCompleted);

        bool pendingMatch = previous == MatchFlowState.PresentingMatch
            || previous == MatchFlowState.AwaitingContinue
            || previous == MatchFlowState.ReturningMatch;

        if(confirmPendingMatch && pendingMatch)
        {
            if(first != null) first.MarkMatched();
            if(second != null) second.MarkMatched();
        }

        foreach(TileController tile in boardTiles)
        {
            if(tile != null) tile.ResetVisualState();
        }

        if(presentationView != null) presentationView.HideImmediate();
        continueArmed = false;
        gesture = default;

        if(boardCompleted && raiseCompletion) RaiseBoardCompleted();
    }

    private void StopRoutine(ref Coroutine routine)
    {
        if(routine != null) StopCoroutine(routine);
        routine = null;
    }

    // ---- Animation helpers -----------------------------------------------------------------

    private static IEnumerator Animate(float duration, AnimationCurve curve, Action<float> apply)
    {
        float elapsed = 0f;
        while(elapsed < duration)
        {
            apply(TileAnimationSettings.Evaluate(curve, elapsed / duration));
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        apply(TileAnimationSettings.Evaluate(curve, 1f));
    }

    private static IEnumerator Wait(float seconds)
    {
        float elapsed = 0f;
        while(elapsed < seconds)
        {
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
    }
}

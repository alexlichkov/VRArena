using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Защитный слот для размещения блоков на арене.
/// Работает вместе с XRSocketInteractor:
/// - когда блок вставляется в сокет, он "замораживается" (нельзя взять, отключена физика);
/// - когда блок убирают из слота, он снова становится обычным XRGrabInteractable.
///
/// Дополнительно:
/// - слушает MatchManager и активен только в заданных фазах (по умолчанию — Preparation).
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public class DefenseSlot : MonoBehaviour
{
    public enum SlotState
    {
        Empty,
        Occupied
    }

    [Header("Debug / Feedback")]
    [SerializeField]
    private bool _logEvents = false;

    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip _placeClip;

    [SerializeField]
    private AudioClip _removeClip;

    [Header("Match integration")]
    [SerializeField]
    private bool _respectMatchState = true;

    [Tooltip("В каких состояниях матча слот активен и позволяет вставлять/убирать блоки.")]
    [SerializeField]
    private MatchManager.MatchState[] _enabledStates =
    {
        MatchManager.MatchState.Preparation
    };

    public SlotState State { get; private set; } = SlotState.Empty;
    public XRGrabInteractable CurrentBlock { get; private set; }

    private XRSocketInteractor _socket;

    private void Awake()
    {
        _socket = GetComponent<XRSocketInteractor>();
        if (_socket == null)
        {
            Debug.LogError($"[{nameof(DefenseSlot)}] XRSocketInteractor не найден на объекте {name}.", this);
        }
    }

    private void OnEnable()
    {
        if (_socket != null)
        {
            _socket.selectEntered.AddListener(OnBlockPlaced);
            _socket.selectExited.AddListener(OnBlockRemoved);
        }

        // Подписываемся на изменения состояния матча, если есть MatchManager
        if (_respectMatchState && MatchManager.HasInstance)
        {
            MatchManager.Instance.MatchStateChanged += OnMatchStateChanged;
            ApplyMatchState(MatchManager.Instance.CurrentState);
        }
    }

    private void OnDisable()
    {
        if (_socket != null)
        {
            _socket.selectEntered.RemoveListener(OnBlockPlaced);
            _socket.selectExited.RemoveListener(OnBlockRemoved);
        }

        if (_respectMatchState && MatchManager.HasInstance)
        {
            MatchManager.Instance.MatchStateChanged -= OnMatchStateChanged;
        }
    }

    private void OnMatchStateChanged(MatchManager.MatchState oldState, MatchManager.MatchState newState)
    {
        ApplyMatchState(newState);
    }

    /// <summary>
    /// Включаем/выключаем сокет в зависимости от текущего состояния матча.
    /// Если MatchManager отсутствует, слот всегда активен.
    /// </summary>
    private void ApplyMatchState(MatchManager.MatchState state)
    {
        if (_socket == null)
            return;

        if (!_respectMatchState || !MatchManager.HasInstance)
        {
            _socket.enabled = true;
            return;
        }

        bool allowed = IsStateAllowed(state);
        _socket.enabled = allowed;

        if (_logEvents)
        {
            Debug.Log($"[{nameof(DefenseSlot)}] Slot '{name}' active = {allowed} for state {state}.", this);
        }
    }

    private bool IsStateAllowed(MatchManager.MatchState state)
    {
        if (_enabledStates == null || _enabledStates.Length == 0)
            return true;

        foreach (var s in _enabledStates)
        {
            if (s == state)
                return true;
        }

        return false;
    }

    private void OnBlockPlaced(SelectEnterEventArgs args)
    {
        ConfigureBlock(args, enableGrab: false, enablePhysics: false);
    }

    private void OnBlockRemoved(SelectExitEventArgs args)
    {
        ConfigureBlock(args, enableGrab: true, enablePhysics: true);
    }

    private void ConfigureBlock(BaseInteractionEventArgs eventArgs, bool enableGrab, bool enablePhysics)
    {
        if (eventArgs == null || eventArgs.interactableObject == null)
        {
            if (_logEvents)
                Debug.LogWarning($"[{nameof(DefenseSlot)}] Пустые eventArgs в ConfigureBlock на {name}.", this);
            return;
        }

        var interactableTransform = eventArgs.interactableObject.transform;
        if (interactableTransform == null)
            return;

        var block = interactableTransform.GetComponent<XRGrabInteractable>();
        if (block == null)
        {
            if (_logEvents)
            {
                Debug.LogWarning(
                    $"[{nameof(DefenseSlot)}] В слот {name} попал объект без XRGrabInteractable: {interactableTransform.name}",
                    this);
            }
            return;
        }

        var rb = block.GetComponent<Rigidbody>();

        State = enableGrab ? SlotState.Empty : SlotState.Occupied;
        CurrentBlock = enableGrab ? null : block;

        block.enabled = enableGrab;

        if (rb != null)
        {
            rb.isKinematic = !enablePhysics;
            rb.useGravity = enablePhysics;
        }

        if (_logEvents)
        {
            var action = enableGrab ? "освобождён из" : "зафиксирован в";
            Debug.Log($"[{nameof(DefenseSlot)}] Блок '{block.name}' {action} слоте '{name}'. Состояние: {State}.", this);
        }

        PlaySfx(enableGrab ? _removeClip : _placeClip);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (_audioSource == null || clip == null)
            return;

        _audioSource.PlayOneShot(clip);
    }
}

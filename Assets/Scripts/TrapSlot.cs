using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Слот для установки ловушки (аналог сокета для баррикад).
/// Использует XRSocketInteractor:
/// - когда ловушку вставили, сообщает ей об этом и фиксирует (без физики, без grab-а);
/// - когда вынули, возвращает grab/физику.
/// </summary>
[RequireComponent(typeof(XRSocketInteractor))]
public sealed class TrapSlot : MonoBehaviour
{
    [Header("Team logic")]
    [Tooltip("Команда, которой разрешено СТАВИТЬ сюда ловушки (обычно противоположная команде острова).")]
    [SerializeField]
    private PlayerTeam.Team _allowedBuilderTeam = PlayerTeam.Team.TeamA;

    /// <summary>Команда, которой разрешено заполнять этот слот ловушками.</summary>
    public PlayerTeam.Team AllowedBuilderTeam => _allowedBuilderTeam;

    private XRSocketInteractor _socket;

    private void Awake()
    {
        _socket = GetComponent<XRSocketInteractor>();
    }

    private void OnEnable()
    {
        if (_socket != null)
        {
            _socket.selectEntered.AddListener(OnSelectEntered);
            _socket.selectExited.AddListener(OnSelectExited);
        }
    }

    private void OnDisable()
    {
        if (_socket != null)
        {
            _socket.selectEntered.RemoveListener(OnSelectEntered);
            _socket.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var trap = args.interactableObject.transform.GetComponent<TrapBase>();
        if (trap == null)
            return;

        // Сообщаем ловушке, что она установленa в слот
        trap.OnPlacedInSlot(this);

        // Фиксируем объект: отключаем grab и физику, чтобы не болтался
        var grab = trap.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.enabled = false;

        var rb = trap.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var trap = args.interactableObject.transform.GetComponent<TrapBase>();
        if (trap == null)
            return;

        trap.OnRemovedFromSlot(this);

        var grab = trap.GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.enabled = true;

        var rb = trap.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
    }
}

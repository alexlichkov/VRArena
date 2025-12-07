using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Универсальный скрипт подсветки XR-объекта при наведении и хвате.
/// Работает с XRGrabInteractable (или любым наследником),
/// подписывается на события hover/select и меняет цвет материала.
///
/// Идея:
/// - hover  → подсветка одним цветом (игрок "смотрит" на объект рукой/интерактором);
/// - select → подсветка другим цветом (объект схвачен);
/// - по выходу из состояний возвращаем базовый цвет.
///
/// Этот скрипт — пример работы с Interactable Events в XRIT:
/// https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit/manual/interactable-events.html
/// </summary>
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class HoverHighlight : MonoBehaviour
{
    [Header("Target renderer")]
    [SerializeField]
    private Renderer _targetRenderer;

    [Header("Colors")]
    [SerializeField]
    private Color _hoverColor = Color.yellow;

    [SerializeField]
    private Color _selectedColor = Color.green;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _interactable;
    private Color _defaultColor;
    private bool _hasDefaultColor;

    private void Awake()
    {
        _interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        // Если рендерер явно не задан — пробуем взять первый попавшийся.
        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (_targetRenderer != null && _targetRenderer.material != null)
        {
            _defaultColor = _targetRenderer.material.color;
            _hasDefaultColor = true;
        }
        else
        {
            Debug.LogWarning(
                $"[{nameof(HoverHighlight)}] На объекте {name} не найден Renderer, подсветка работать не будет.",
                this);
        }
    }

    private void OnEnable()
    {
        if (_interactable == null)
            return;

        // Подписываемся на события Interactable’а.
        // Важно: event args из XR IT по докам валидны только внутри вызова, хранить их нельзя.
        // 
        _interactable.hoverEntered.AddListener(OnHoverEntered);
        _interactable.hoverExited.AddListener(OnHoverExited);
        _interactable.selectEntered.AddListener(OnSelectEntered);
        _interactable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDisable()
    {
        if (_interactable == null)
            return;

        _interactable.hoverEntered.RemoveListener(OnHoverEntered);
        _interactable.hoverExited.RemoveListener(OnHoverExited);
        _interactable.selectEntered.RemoveListener(OnSelectEntered);
        _interactable.selectExited.RemoveListener(OnSelectExited);

        // На всякий случай вернём дефолтный цвет.
        RestoreDefaultColor();
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Если объект уже схвачен — не перекрашиваем hover'ом.
        if (_interactable.isSelected)
            return;

        SetColor(_hoverColor);
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Если объект всё ещё схвачен — цвет должен оставаться "selected".
        if (_interactable.isSelected)
            return;

        RestoreDefaultColor();
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        SetColor(_selectedColor);
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        // Если интеркторы больше не держат объект, возвращаем дефолтный цвет.
        if (!_interactable.isHovered)
            RestoreDefaultColor();
        else
            SetColor(_hoverColor);
    }

    private void SetColor(Color c)
    {
        if (!_hasDefaultColor || _targetRenderer == null)
            return;

        _targetRenderer.material.color = c;
    }

    private void RestoreDefaultColor()
    {
        if (!_hasDefaultColor || _targetRenderer == null)
            return;

        _targetRenderer.material.color = _defaultColor;
    }
}

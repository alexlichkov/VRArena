using UnityEngine;

/// <summary>
/// Простая метка команды для игрока.
/// В текущем прототипе задаётся в инспекторе.
/// В сетевой версии это будет приходить от сервера.
/// </summary>
public class PlayerTeam : MonoBehaviour
{
    /// <summary>
    /// Команды игрока. Можно расширять (Spectator, Neutral и т.д.).
    /// </summary>
    public enum Team
    {
        None = 0,
        TeamA = 1,
        TeamB = 2
    }

    [Header("Team Settings")]
    [SerializeField]
    private Team _team = Team.TeamA;

    /// <summary>
    /// Текущая команда игрока (read-only снаружи).
    /// TeleportTeamFilter и другая логика читают именно это свойство.
    /// </summary>
    public Team CurrentTeam => _team;

    /// <summary>
    /// Метод на будущее: можно будет менять команду из кода
    /// (например, при подключении к сетевому матчу).
    /// </summary>
    public void SetTeam(Team newTeam)
    {
        _team = newTeam;
    }
}

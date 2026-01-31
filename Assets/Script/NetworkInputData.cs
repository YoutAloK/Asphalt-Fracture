using Fusion;

/// <summary>
/// Структура для передачи input данных по сети
/// Должна быть максимально компактной для минимизации трафика
/// </summary>
public struct NetworkInputData : INetworkInput
{
    public float Horizontal;    // -1 до 1 (A/D или стрелки)
    public float Vertical;      // -1 до 1 (W/S или стрелки)
    public NetworkBool IsBraking; // Ручное торможение (пробел)
}
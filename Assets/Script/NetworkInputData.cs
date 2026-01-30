using Fusion;

// Эта структура должна находиться в своем собственном файле,
// чтобы к ней могли обращаться другие скрипты, ответственные за сбор ввода.
public struct NetworkInputData : INetworkInput
{
    public float Horizontal;
    public float Vertical;
}

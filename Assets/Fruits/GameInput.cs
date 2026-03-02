public static class GameInput
{
    public static bool IsLocked { get; private set; }

    public static void Lock() => IsLocked = true;
    public static void Unlock() => IsLocked = false;
}
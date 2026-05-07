public interface ISavable<TState>
{
    TState CaptureState();
    void RestoreState(TState state);
}

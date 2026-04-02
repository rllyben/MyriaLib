namespace MyriaLib.Systems.Events
{
    public class MonsterKilledEventArgs : EventArgs
    {
        public int MonsterId { get; }

        public MonsterKilledEventArgs(int monsterId)
        {
            MonsterId = monsterId;
        }
    }
}

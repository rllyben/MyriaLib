namespace Myria.Lib.Core.Systems.Events
{
    public class MoneyChangedEventArgs
    {
        public long OldValue { get; }
        public long NewValue { get; }
        public long Delta => NewValue - OldValue;
        public string? Source { get; }

        public MoneyChangedEventArgs(long oldValue, long newValue, string? source = null)
        {
            OldValue = oldValue;
            NewValue = newValue;
            Source = source;
        }
    }
}

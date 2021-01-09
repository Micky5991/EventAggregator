namespace Micky5991.EventAggregator
{
    /// <summary>
    /// Priority in which order the events should be executed in. Higher Priority means their calls will be executed
    /// last and their values will overwrite lower values.
    /// </summary>
    public enum EventPriority
    {
        /// <summary>
        /// Lowest priority and will always be executed.
        /// </summary>
        Lowest,

        /// <summary>
        /// Lower priority which can be cancelled by the lowest priority.
        /// </summary>
        Low,

        /// <summary>
        /// Default priority which can be cancelled by Low and Lowest,
        /// </summary>
        Normal,

        /// <summary>
        /// High priority which could be cancelled from all other lower priorities.
        /// </summary>
        High,

        /// <summary>
        /// Highest priority that will be executed and that can change data.
        /// </summary>
        Highest,

        /// <summary>
        /// Event that can be used to watch event data and no data should be modified.
        /// </summary>
        Monitor,
    }
}

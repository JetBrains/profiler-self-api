namespace JetBrains.Profiler.SelfApi
{
    /// <summary>
    /// Operation progress callback. 
    /// </summary>
    public interface IProgress
    {
        /// <summary>
        /// Advances progress to given number of percents.
        /// The sum of all <paramref name="percentDelta"/>-s is less or equal to 100.
        /// </summary>
        void Advance(double percentDelta);
    }
}
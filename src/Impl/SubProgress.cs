namespace JetBrains.Profiler.SelfApi.Impl
{
    internal sealed class SubProgress : IProgress
    {
        private readonly IProgress _parent;
        private readonly double _weight;

        public SubProgress(IProgress parent, double weight)
        {
            _parent = parent;
            _weight = weight;
        }

        public void Advance(double percentDelta)
        {
            _parent.Advance(percentDelta * _weight);
        }
    }
}
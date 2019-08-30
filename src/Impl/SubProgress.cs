using System;

namespace JetBrains.Profiler.SelfApi.Impl
{
    internal sealed class SubProgress : IProgress<double>
    {
        private readonly IProgress<double> _parent;
        private readonly double _start;
        private readonly double _weight;

        public SubProgress(IProgress<double> parent, double start, double weight)
        {
            _parent = parent;
            _start = start;
            _weight = weight;
        }

        public void Report(double value)
        {
            _parent?.Report(_start + value * _weight);
        }
    }
}
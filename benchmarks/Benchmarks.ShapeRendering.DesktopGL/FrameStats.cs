namespace Benchmarks.ShapeRendering
{
    /// <summary>
    /// Rolling average over the last <see cref="WindowSize"/> frame times, so the on-screen FPS
    /// reading doesn't jitter frame-to-frame.
    /// </summary>
    public sealed class FrameStats
    {
        private const int WindowSize = 120;

        private readonly double[] _samples = new double[WindowSize];
        private int _index;
        private int _filled;

        public void AddSample(double frameMs)
        {
            _samples[_index] = frameMs;
            _index = (_index + 1) % _samples.Length;
            if (_filled < _samples.Length)
                _filled++;
        }

        public double AverageMs
        {
            get
            {
                if (_filled == 0)
                    return 0;

                double sum = 0;
                for (int i = 0; i < _filled; i++)
                    sum += _samples[i];
                return sum / _filled;
            }
        }

        public double AverageFps => AverageMs <= 0 ? 0 : 1000.0 / AverageMs;

        public void Reset()
        {
            _index = 0;
            _filled = 0;
        }
    }
}

using System.Threading.Tasks;
using EasySocket.Server;

namespace EasySocket.Benchmark.Benchmarkers
{
    public class Benchmarker<TBenchmarker> : IBenchmarker<TBenchmarker>
        where TBenchmarker : IBenchmarker<TBenchmarker>
    {
        public ValueTask StartAsync()
        {
            return new ValueTask();
        }

        public ValueTask StopAsync()
        {
            return new ValueTask();
        }

    }
}
using System.Threading.Tasks;

namespace EasySocket.Benchmark.Benchmarkers
{
    public interface IBenchmarker<TBenchmarker>
        where TBenchmarker : IBenchmarker<TBenchmarker>
    {
        ValueTask StartAsync();
        ValueTask StopAsync();
    }
}
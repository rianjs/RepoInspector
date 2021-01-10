using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace RepoInspector
{
    public class LoopService
    {
        private readonly IWorker _worker;
        private readonly TimeSpan _loopDelay;
        private readonly CancellationTokenSource _cts;
        private readonly ILogger _logger;

        /// <param name="loopDelay">Duration to wait before running the loop again. If the loop is meant to run every 3 seconds, and the previous iteration took
        /// 2 seconds, the wait will be 1 second. If the previous iteration took 4 seconds, the wait will be zero. Loop values less than 1 second are not
        /// permitted.</param>
        public LoopService(IWorker worker, TimeSpan loopDelay, CancellationTokenSource cts, ILogger logger)
        {
            _worker = worker ?? throw new ArgumentNullException(nameof(worker));
            _loopDelay = loopDelay < TimeSpan.FromSeconds(1)
                ? throw new ArgumentOutOfRangeException($"Loop delay must be at least 1 second")
                : loopDelay;
            _cts = cts ?? throw new ArgumentNullException(nameof(cts));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
    
        public async Task LoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                _logger.LogInformation($"{_worker.Name} - starting work loop");
                var timer = Stopwatch.StartNew();

                try
                {
                    await _worker.ExecuteAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{_worker.Name} - work loop threw an exception");
                }
            
                timer.Stop();
                _logger.LogInformation($"{_worker.Name} - finished work loop");

                var toWait = GetSleepDelay(_loopDelay, timer.Elapsed);
                await Task.Delay(toWait, _cts.Token);
            }
        }

        internal static TimeSpan GetSleepDelay(TimeSpan delay, TimeSpan elapsed)
        {
            var toWait = delay - elapsed;
            return toWait > TimeSpan.Zero
                ? toWait
                : TimeSpan.Zero;
        }
    }
}
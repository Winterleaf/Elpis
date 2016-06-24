namespace Kayak.Net
{
    internal class DefaultKayakScheduler : System.Threading.Tasks.TaskScheduler, IScheduler
    {
        internal DefaultKayakScheduler(ISchedulerDelegate del)
        {
            if (del == null)
                throw new System.ArgumentNullException(nameof(del));

            _del = del;
            _queue = new System.Collections.Concurrent.ConcurrentQueue<System.Threading.Tasks.Task>();
        }

        public override int MaximumConcurrencyLevel => 1;

        private readonly ISchedulerDelegate _del;

        private System.Threading.Thread _dispatch;
        private System.Collections.Concurrent.ConcurrentQueue<System.Threading.Tasks.Task> _queue;
        private bool _stopped;
        private System.Threading.ManualResetEventSlim _wh;

        public void Post(System.Action action)
        {
            System.Diagnostics.Debug.WriteLine("--- Posted task.");

            System.Threading.Tasks.Task task = new System.Threading.Tasks.Task(action);
            task.ContinueWith(t => { _del.OnException(this, t.Exception); }, System.Threading.CancellationToken.None,
                System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted, this);
            task.Start(this);
        }

        public void Start()
        {
            if (_dispatch != null)
                throw new System.InvalidOperationException("The scheduler was already started.");

            Dispatch();
        }

        public void Stop()
        {
            System.Diagnostics.Debug.WriteLine("Scheduler will stop.");
            Post(() => { _stopped = true; });
        }

        public void Dispose()
        {
            // nothing to see here!
        }

        private void Dispatch()
        {
            _wh = new System.Threading.ManualResetEventSlim();

            while (true)
            {
                System.Threading.Tasks.Task outTask;

                if (_queue.TryDequeue(out outTask))
                {
                    System.Diagnostics.Debug.WriteLine("--- Executing Task ---");
                    TryExecuteTask(outTask);
                    System.Diagnostics.Debug.WriteLine("--- Done Executing Task ---");

                    if (!_stopped) continue;
                    _stopped = false;
                    _dispatch = null;
                    _queue = new System.Collections.Concurrent.ConcurrentQueue<System.Threading.Tasks.Task>();

                    System.Diagnostics.Debug.WriteLine("Scheduler stopped.");
                    _del.OnStop(this);

                    break;
                }
                _wh.Wait();
                _wh.Reset();
            }

            _stopped = false;
            _dispatch = null;
            _wh.Dispose();
            _wh = null;
        }

        protected override System.Collections.Generic.IEnumerable<System.Threading.Tasks.Task> GetScheduledTasks()
        {
            yield break;
        }

        protected override void QueueTask(System.Threading.Tasks.Task task)
        {
            _queue.Enqueue(task);
            _wh?.Set();
        }

        protected override bool TryDequeue(System.Threading.Tasks.Task task)
        {
            System.Threading.Tasks.Task outTask;
            _queue.TryDequeue(out outTask);
            return task == outTask;
        }

        protected override bool TryExecuteTaskInline(System.Threading.Tasks.Task task, bool taskWasPreviouslyQueued)
        {
            if (System.Threading.Thread.CurrentThread != _dispatch) return false;

            if (taskWasPreviouslyQueued && !TryDequeue(task))
            {
                return false;
            }

            return TryExecuteTask(task);
        }
    }
}
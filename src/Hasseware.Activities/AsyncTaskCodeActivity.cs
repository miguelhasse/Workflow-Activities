using System;
using System.Activities;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Hasseware.Activities
{
	public abstract class AsyncTaskCodeActivity : AsyncCodeActivity
	{
		protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
		{
			var cts = new CancellationTokenSource();
			context.UserState = cts;

			var tcs = new TaskCompletionSource<bool>(state);
			
			ExecuteAsync(context, cts.Token).ContinueWith(t =>
			{
				if (t.IsFaulted)
				{
					tcs.TrySetResult(false);
					tcs.TrySetException(t.Exception.InnerExceptions);
				}
				else if (t.IsCanceled)
				{
					tcs.TrySetResult(false);
					tcs.TrySetCanceled();
				}
				else tcs.TrySetResult(true);

				if (callback != null)
					callback(tcs.Task);
			});
			return tcs.Task;
		}

		protected sealed override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
		{
		}

		protected sealed override void Cancel(AsyncCodeActivityContext context)
		{
			if (context.UserState is CancellationTokenSource)
			{
				var cts = (CancellationTokenSource)context.UserState;
				cts.Cancel();
			}
		}

		protected abstract Task ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
	}


	public abstract class AsyncTaskCodeActivity<T> : AsyncCodeActivity<T>
	{
		protected sealed override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
		{
			var cts = new CancellationTokenSource();
			context.UserState = cts;

			var tcs = new TaskCompletionSource<T>(state);

			ExecuteAsync(context, cts.Token).ContinueWith(t =>
			{
				if (t.IsFaulted) tcs.TrySetException(t.Exception.InnerExceptions);
				else if (t.IsCanceled) tcs.TrySetCanceled();
				else tcs.TrySetResult(t.Result);

				if (callback != null)
					callback(tcs.Task);
			});
			return tcs.Task;
		}

		protected sealed override T EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
		{
			try
			{
				var task = (Task<T>)result; 
				return task.Result;
			}
			catch (AggregateException ex)
			{
				ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
				throw;
			}
		}

		protected sealed override void Cancel(AsyncCodeActivityContext context)
		{
			if (context.UserState is CancellationTokenSource)
			{
				var cts = (CancellationTokenSource)context.UserState;
				cts.Cancel();
			}
		}

		protected abstract Task<T> ExecuteAsync(AsyncCodeActivityContext context, CancellationToken cancellationToken);
	}
}

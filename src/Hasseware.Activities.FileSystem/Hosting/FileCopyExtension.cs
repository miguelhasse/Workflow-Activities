using System;
using System.Activities;
using System.Activities.Hosting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Hasseware.Activities.Hosting
{
	internal sealed class FileCopyExtension : IWorkflowInstanceExtension
	{
		private WorkflowInstanceProxy instance;
		private ConcurrentDictionary<string, CopyFileContext> contexts;

		IEnumerable<object> IWorkflowInstanceExtension.GetAdditionalExtensions()
		{
			yield break;
		}

		void IWorkflowInstanceExtension.SetInstance(WorkflowInstanceProxy instance)
		{
			this.instance = instance;
			this.contexts = new ConcurrentDictionary<string, CopyFileContext>();
		}

		public bool Resume(Bookmark bookmark, FileInfo source, FileInfo destination, Statements.FileCopyOptions options, int stepIncrement)
		{
			var context = new CopyFileContext(stepIncrement)
			{
				Instance = this,
				Bookmark = bookmark,
				Action = CopyFileAction.Continue
			};
			if (contexts.TryAdd(bookmark.Name, context))
			{
				var action = new Action(() => ExecuteCopyFile(context, source, destination, options));
				action.BeginInvoke((a) =>
				{
					try { action.EndInvoke(a); }
					catch (Exception ex) { ((CopyFileContext)a.AsyncState).Exception = ex; }
					finally { ((CopyFileContext)a.AsyncState).NotifyCompletion(); }
				}, context);
				return true;
			}
			return false;
		}

		public void Cancel(string bookmarkName)
		{
			CopyFileContext context;
			if (contexts.TryRemove(bookmarkName, out context))
				context.Action += 1;
		}

		private void ExecuteCopyFile(CopyFileContext context, FileInfo source, FileInfo destination, Statements.FileCopyOptions options)
		{
			FileCopyHelper.CopyFile(source, destination, options,
				new CopyFileHandler((s, t, st, sz, tr) => ((CopyFileContext)st).HandleProgress(s, t, sz, tr)), context);
		}

		private void NotifyCompletion(CopyFileContext context)
		{
			instance.BeginResumeBookmark(context.Bookmark, context.Exception, (a) => instance.EndResumeBookmark(a), null);
			if (contexts.TryRemove(context.Bookmark.Name, out context)) context.Action = CopyFileAction.Stop;
		}

		private void HandleProgress(CopyFileContext context, FileInfo source, FileInfo destination, int progress)
		{
			instance.BeginResumeBookmark(context.Bookmark, progress, (a) => instance.EndResumeBookmark(a), null);
		}

		class CopyFileContext
		{
			public Bookmark Bookmark;
			public FileCopyExtension Instance;
			public CopyFileAction Action;
			public Exception Exception;

			private int lastStep, stepIncrement;

			public CopyFileContext(int stepIncrement)
			{
				this.stepIncrement = stepIncrement;
				this.lastStep = -1;
			}

			public void NotifyCompletion()
			{
				Instance.NotifyCompletion(this);
			}

			public CopyFileAction HandleProgress(FileInfo source, FileInfo destination, long totalFileSize, long totalBytesTransferred)
			{
				if (this.stepIncrement > 0 && this.stepIncrement < 100)
				{
					int progress = (Convert.ToInt32((((decimal)totalBytesTransferred) / totalFileSize) * 100) / this.stepIncrement) * this.stepIncrement;
					if (progress > this.lastStep) Instance.HandleProgress(this, source, destination, this.lastStep = progress);
				}
				return this.Action;
			}
		}
	}
}

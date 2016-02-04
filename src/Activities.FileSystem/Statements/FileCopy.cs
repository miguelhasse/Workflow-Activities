using System;
using System.Activities;
using System.Activities.Tracking;
using System.ComponentModel;
using System.IO;
using System.Windows.Markup;

namespace Hasseware.Activities.Statements
{
	[ContentProperty("OnProgress")]
	public sealed class FileCopy : NativeActivity
	{
		private Variable<NoPersistHandle> noPersistHandle;
		private Variable<Bookmark> bookmarkProgress;

		private BookmarkCallback bookmarkProgressCallback;

		#region Constructors

		public FileCopy()
		{
			// add the validation to the list of validations for this activity
			this.Constraints.Add(ConstraintHelper.VerifiyNoChildPersistActivity<FileCopy>());

			// create the variables to hold the NoPersistHandle and Bookmark
			this.noPersistHandle = new Variable<NoPersistHandle>();
			this.bookmarkProgress = new Variable<Bookmark>();

			this.bookmarkProgressCallback = new BookmarkCallback(OnExtensionProgress);
			this.StepIncrement = 1;
		}

		#endregion

		[RequiredArgument]
		public InArgument<string> Source { get; set; }

		[RequiredArgument]
		public InArgument<string> Target { get; set; }

		[DefaultValue(FileCopyOptions.None)]
		public FileCopyOptions Option { get; set; }

		[DefaultValue(1)]
		public int StepIncrement { get; set; }

		[DefaultValue(null)]
		[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
		public ActivityAction<int> OnProgress { get; set; }

		protected override bool CanInduceIdle
		{
			get { return true; }
		}

		protected override void CacheMetadata(NativeActivityMetadata metadata)
		{
			// Tell the runtime that we need this extension
			metadata.RequireExtension(typeof(Hosting.FileCopyExtension));

			// Provide a Func<T> to create the extension if it does not already exist
			metadata.AddDefaultExtensionProvider(() => new Hosting.FileCopyExtension());

			if (this.StepIncrement < 0 || this.StepIncrement > 100)
				metadata.AddValidationError(Properties.Resources.StepIncrementOutOfRange);

			if (this.OnProgress != null) metadata.AddDelegate(this.OnProgress);

			metadata.AddImplementationVariable(this.noPersistHandle);
			metadata.AddImplementationVariable(this.bookmarkProgress);

			metadata.AddArgument(new RuntimeArgument("Source", typeof(string), ArgumentDirection.In, true));
			metadata.AddArgument(new RuntimeArgument("Target", typeof(string), ArgumentDirection.In, true));
		}

		protected override void Cancel(NativeActivityContext context)
		{
			context.CancelChildren();
			var bookmark = bookmarkProgress.Get(context);

			var extension = context.GetExtension<Hosting.FileCopyExtension>();
			extension.Cancel(bookmark.Name);

			this.noPersistHandle.Get(context).Exit(context);
			context.RemoveBookmark(bookmark);
			context.MarkCanceled();
		}

		protected override void Execute(NativeActivityContext context)
		{
			FileInfo source = new FileInfo(context.GetValue<string>(this.Source));
			FileInfo target = new FileInfo(context.GetValue<string>(this.Target));

			if (target.Attributes.HasFlag(FileAttributes.Directory))
				target = new FileInfo(Path.Combine(target.FullName, source.Name));

			this.noPersistHandle.Get(context).Enter(context);

			// Set a bookmark for progress callback resuming
			string bookmarkName = String.Format("FileCopy_{0:X}", source.FullName.GetHashCode());
			var bookmark = context.CreateBookmark(bookmarkName, this.bookmarkProgressCallback, BookmarkOptions.MultipleResume);
			bookmarkProgress.Set(context, bookmark);

			var extension = context.GetExtension<Hosting.FileCopyExtension>();
			extension.Resume(bookmark, source, target, this.Option, this.StepIncrement);
		}

		private void OnExtensionProgress(NativeActivityContext context, Bookmark bookmark, Object data)
		{
			if (bookmarkProgress.Get(context) != bookmark)
				return;

			if (data is int && !context.IsCancellationRequested)
			{
				Track(context, data);
				if (this.OnProgress != null)
					context.ScheduleAction<int>(this.OnProgress, (int)data);
			}
			else
			{
				this.noPersistHandle.Get(context).Exit(context);
				context.RemoveBookmark(bookmark);

				if (data is Exception)
					throw data as Exception;
			}
		}

		private void Track(NativeActivityContext context, Object data) 
		{
			string source = this.Source.Get(context), target = this.Target.Get(context);
			context.Track(new CustomTrackingRecord("FileCopy", System.Diagnostics.TraceLevel.Info)
			{
				Data = { { "Source", source }, { "Target", target }, { "Progress", data } }
			});
		}
	}
}

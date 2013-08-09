namespace Ecng.Interop.Dde
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	using Ecng.ComponentModel;
	using Ecng.Common;

	using NDde.Server;

	public class XlsDdeServer : DdeServer
	{
		private readonly SyncObject _registerWait = new SyncObject();
		private Timer _adviseTimer;
		private readonly EventDispatcher _dispather;
		private readonly Action<string, IList<IList<object>>> _poke;
		private readonly Action<Exception> _error;

		public XlsDdeServer(string service, Action<string, IList<IList<object>>> poke, Action<Exception> error)
			: base(service)
		{
			if (poke == null)
				throw new ArgumentNullException("poke");

			if (error == null)
				throw new ArgumentNullException("error");

			_poke = poke;
			_error = error;
			_dispather = new EventDispatcher(error);
		}

		public void Start()
		{
			Exception error = null;

			var regLock = new SyncObject();

			lock (regLock)
			{
				ThreadingHelper
					.Thread(() =>
					{
						try
						{
							Register();
							regLock.Pulse();

							_registerWait.Wait();
						}
						catch (Exception ex)
						{
							error = ex;
							regLock.Pulse();
						}
					})
					.Name("Dde thread")
					.Launch();

				Monitor.Wait(regLock);
			}

			if (error != null)
				throw new InvalidOperationException("������ ������� DDE �������.", error);

			// Create a timer that will be used to advise clients of new data.
			_adviseTimer = ThreadingHelper.Timer(() =>
			{
				try
				{
					// Advise all topic name and item name pairs.
					Advise("*", "*");
				}
				catch (Exception ex)
				{
					_error(ex);
				}
			})
			.Interval(TimeSpan.FromSeconds(1));
		}

		protected override PokeResult OnPoke(DdeConversation conversation, string item, byte[] data, int format)
		{
			_dispather.Add(() =>
			{
				var rows = XlsDdeSerializer.Deserialize(data);
				_poke(conversation.Topic, rows);
			}, conversation.Topic);

			return PokeResult.Processed;
		}

		protected override void Dispose(bool disposing)
		{
			_dispather.Dispose();

			if (disposing)
			{
				if (!_adviseTimer.IsNull())
					_adviseTimer.Dispose();

				_registerWait.Pulse();
			}

			base.Dispose(disposing);
		}
	}
}
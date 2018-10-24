﻿namespace Ecng.Common
{
	using System;
	using System.Threading;

	public class SimpleResettableTimer
	{
		private readonly SyncObject _sync = new SyncObject();
		private readonly TimeSpan _period;

		private Timer _timer;
		private bool _changed;

		public event Action Elapsed;

		public SimpleResettableTimer(TimeSpan period)
		{
			_period = period;
		}

		public void Reset()
		{
			lock (_sync)
			{
				if (_timer == null)
				{
					_timer = ThreadingHelper
						.Timer(OnTimer)
						.Interval(_period);
				}
				else
					_changed = true;
			}
		}

		private void OnTimer()
		{
			var elapsed = false;

			lock (_sync)
			{
				if (!_changed)
				{
					_timer.Dispose();
					_timer = null;

					elapsed = true;
				}
				else
					_changed = false;
			}

			if (elapsed)
				Elapsed?.Invoke();
		}

		public void Flush()
		{
			lock (_sync)
			{
				if (_timer == null)
					return;

				_changed = false;
				_timer.Change(TimeSpan.Zero, _period);
			}
		}

		public void Dispose()
		{
			lock (_sync)
			{
				if (_timer == null)
					return;

				_changed = true;
				_timer.Dispose();
			    _timer = null;
            }
		}
	}
}

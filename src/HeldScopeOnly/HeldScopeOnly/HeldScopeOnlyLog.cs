using System;
using System.IO;

namespace HeldScopeOnly;

internal static class HeldScopeOnlyLog
{
	private static readonly object Sync = new object();

	private static string _path;

	private static bool _initialized;

	internal static void Initialize()
	{
		lock (Sync)
		{
			try
			{
				_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HeldScopeOnly.log");
				File.WriteAllText(_path, string.Empty);
				_initialized = true;
				WriteLine("log initialized at " + DateTime.Now.ToString("O"));
			}
			catch (Exception ex)
			{
				_initialized = false;
				if (HeldScopeOnlyPlugin.Log != null)
				{
					HeldScopeOnlyPlugin.Log.LogWarning((object)("Could not create HeldScopeOnly.log: " + ex));
				}
			}
		}
	}

	internal static void WriteLine(string message)
	{
		string text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message;
		if (HeldScopeOnlyPlugin.Log != null)
		{
			HeldScopeOnlyPlugin.Log.LogInfo((object)("[HeldScopeOnly] " + message));
		}
		if (!_initialized || string.IsNullOrEmpty(_path))
		{
			return;
		}
		lock (Sync)
		{
			try
			{
				File.AppendAllText(_path, text + Environment.NewLine);
			}
			catch
			{
			}
		}
	}
}

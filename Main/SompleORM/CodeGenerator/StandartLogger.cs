using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


namespace CodeGenerator
{
	public class StandartLogger : ILogger
	{
		#region ILogger Members

		public void WriteEntry(string message, LogSeverity severity)
		{
			EventLogEntryType let = EventLogEntryType.Information;

			if (severity == LogSeverity.Information)
				let = EventLogEntryType.Information;
			else if (severity == LogSeverity.Warning)
				let = EventLogEntryType.Warning;
			else if (severity == LogSeverity.Error)
				let = EventLogEntryType.Error;

			EventLog.WriteEntry("Application", message, let);
		}

		#endregion
	}
}

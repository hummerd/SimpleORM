

namespace CodeGenerator
{
	public enum LogSeverity
	{ 
		Information,
		Warning,
		Error
	}

	public enum LogLevel
	{
		None,
		ErrorsOnly,
		ErrorsAndInfo
	}


	public interface ILogger
	{
		void WriteEntry(string message, LogSeverity severity);
	}
}

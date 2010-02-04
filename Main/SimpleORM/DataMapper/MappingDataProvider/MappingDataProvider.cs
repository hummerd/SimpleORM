using System.Collections.Generic;


namespace SimpleORM.MappingDataProvider
{
	public class MappingProvider : IMappingDataProvider
	{
		protected List<IMappingDataProvider> _Providers;


		public MappingProvider(List<IMappingDataProvider> providers)
		{
			_Providers = providers;
		}

		#region IMappingDataProvider Members

		public bool SetConfig(IEnumerable<string> configFiles)
		{
			bool result = false;

			foreach (var item in _Providers)
			{
				bool agree = item.SetConfig(configFiles);
				result |= agree;
			}

			return result;
		}

		public bool GetExtractInfo(ExtractInfo extractInfo)
		{
			bool result = false;

			foreach (var item in _Providers)
			{
				result = item.GetExtractInfo(extractInfo);
				if (result)
					break;
			}

			return result;
		}

		#endregion
	}
}

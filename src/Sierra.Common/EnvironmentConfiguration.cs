namespace Sierra.Common
{
    using System.Collections.Generic;

    public sealed class EnvironmentConfiguration
    {
        public IReadOnlyDictionary<string, string> EnvironmentSubscriptionMap { get; }

        public EnvironmentConfiguration(IReadOnlyDictionary<string, string> environmentSubscriptionMap)
        {
            EnvironmentSubscriptionMap = environmentSubscriptionMap;
        }
    }
}

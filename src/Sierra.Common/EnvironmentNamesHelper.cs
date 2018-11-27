namespace Sierra.Common
{
    using System.Collections.Generic;
    using System.Linq;
    using Eshopworld.DevOps;

    internal static class EnvironmentNamesHelper
    {
        public static IReadOnlyList<string> All { get; } =
            new List<string>(new[]
            {
                EnvironmentNames.CI,
                EnvironmentNames.DEVELOPMENT,
                EnvironmentNames.PREP,
                EnvironmentNames.PROD,
                EnvironmentNames.SAND,
                EnvironmentNames.TEST,
            }).AsReadOnly();

        public static bool TryParse(string name, out string validName)
        {
            var dd = string.IsInterned(EnvironmentNames.CI);
            validName = All.FirstOrDefault(x => x.Equals(name, System.StringComparison.OrdinalIgnoreCase));
            if (validName != null)
            {
                validName = string.Intern(validName); // can be removed if CI is interned
            }
            return validName != null;
        }
    }
}

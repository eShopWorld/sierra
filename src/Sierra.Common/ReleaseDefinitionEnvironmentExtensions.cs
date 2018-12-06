namespace Sierra.Common
{
    using System.IO;
    using System.Runtime.Serialization;
    using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Contracts;

    /// <summary>
    /// extension class to allow for cloning of environmental definition tree
    /// </summary>
    public static class ReleaseDefinitionEnvironmentExtensions
    {
        /// <summary>
        /// deep clone using <see cref="DataContractSerializer"/>
        /// </summary>
        /// <param name="original">original instance</param>
        /// <returns>new instance (deep clone)</returns>
        public static ReleaseDefinitionEnvironment DeepClone(this ReleaseDefinitionEnvironment original)
        {
            using (var stream = new MemoryStream())
            {
                var dcs = new DataContractSerializer(typeof(ReleaseDefinitionEnvironment)); //all the structures are wcf data contracts
                dcs.WriteObject(stream, original);
                stream.Position = 0;
                return (ReleaseDefinitionEnvironment)dcs.ReadObject(stream);
            }
        }
    }
}

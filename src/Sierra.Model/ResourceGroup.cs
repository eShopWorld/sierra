namespace Sierra.Model
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Message payload to specify a resource group to be created or deleted.
    /// </summary>
    [DataContract]
    public class ResourceGroup
    {
        public ResourceGroup()
        {
        }

        public ResourceGroup(string name)
        {
            Name = name;
        }

        [DataMember]
        public string Name { get; set; }
    }
}

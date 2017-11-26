namespace Sierra.PowerShell
{
    using System.Management.Automation;

    [Cmdlet(VerbsDiagnostic.Test, "Stuff")]
    public class TestStuffCommand : Cmdlet
    {
        protected override void BeginProcessing()
        {
            WriteObject("Foo.");
        }
    }
}

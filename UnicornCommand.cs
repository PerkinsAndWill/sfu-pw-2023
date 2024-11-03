using Rhino;
using Rhino.Commands;


//Note: this is left as an example of how Rhino commands could be implemented into the pluginm i.e. just follow the format of this class.
namespace Unicorn
{
    public class UnicornCommand : Command
    {
        public UnicornCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static UnicornCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Test";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {

            // ---
            return Result.Success;
        }
    }
}

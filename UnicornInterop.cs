namespace Unicorn
{
    public class UnicornInterop
    {
        public string IndexPath { get; set; }

        public UnicornInterop()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the UnicornPlugin plug-in.</summary>
        public static UnicornInterop Instance
        {
            get; private set;
        }

    }
}

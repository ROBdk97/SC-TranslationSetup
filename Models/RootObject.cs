namespace SC_TranslationSetup.Models
{
    public class RootObject
    {
        public RootObject()
        {
        }

#pragma warning disable IDE1006 // Naming Styles
        public Dictionary<string, Lang>? languages { get; set; }
#pragma warning restore IDE1006 // Naming Styles
    }
}

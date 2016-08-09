using Verse;
using RimWorld;
using BuildProductive.Injection;

namespace BuildProductive
{
    [StaticConstructorOnStartup]
    class Bootstrapper
    {
        static Bootstrapper()
        {
            Privates.Resolve();

            if (Globals.Injector != null)
            {
                Log.Error("Injector already initialized.");
                return;
            }

            var injector = new HookInjector();
            Globals.Injector = injector;

            // Post load hook
            injector.Inject(typeof(MapIniterUtility), "FinalizeMapInit", typeof(InitScript));

            // Command-related hooks
            injector.Inject(typeof(Command), "get_IconDrawColor", typeof(VerseExtensions));

            // Designator-related hooks
            injector.Inject(typeof(GenConstruct), "PlaceBlueprintForBuild", typeof(VerseExtensions));
            injector.Inject(typeof(Blueprint_Build), "MakeSolidThing", typeof(VerseExtensions));
            injector.Inject(typeof(Frame), "CompleteConstruction", typeof(VerseExtensions));
            injector.Inject(typeof(Frame), "FailConstruction", typeof(VerseExtensions));
            injector.Inject(typeof(Designator_Cancel), "DesignateThing", typeof(VerseExtensions));
        }
    }
}

using System;
using Verse;
using RimWorld;
using BuildProductive.Injection;

namespace BuildProductive
{
    [StaticConstructorOnStartup]
    class Bootstrapper
    {
        public static readonly Logger.Level Verbosity = Logger.Level.Debug;

        static Bootstrapper()
        {
            Globals.Logger = new Logger { MessagePrefix = "BuildProductive: ", Verbosity = Verbosity };

            Privates.Resolve();

            if (Globals.Injector != null)
            {
                Globals.Logger.Error("Injector already initialized.");
                return;
            }

            var injector = new HookInjector();
            Globals.Injector = injector;

            // Post load hook
            injector.Inject(typeof(MapIniterUtility), "FinalizeMapInit", typeof(InitScript));

            // Command-related hooks
            injector.Inject(typeof(Command), "get_IconDrawColor", typeof(VerseExtensions));
            //injector.Inject(typeof(GizmoGridDrawer), "DrawGizmoGrid", typeof(VerseExtensions));

            // Designator-related hooks
            injector.Inject(typeof(GenConstruct), "PlaceBlueprintForBuild", typeof(VerseExtensions));
            injector.Inject(typeof(Blueprint_Build), "MakeSolidThing", typeof(VerseExtensions));
            injector.Inject(typeof(Frame), "CompleteConstruction", typeof(VerseExtensions));
            injector.Inject(typeof(Frame), "FailConstruction", typeof(VerseExtensions));

            var genLeaving = typeof(GenLeaving);
            var doLeavingsFor = genLeaving.GetMethod("DoLeavingsFor", new Type[] { typeof(Thing), typeof(DestroyMode) });
            //injector.Inject(genLeaving, doLeavingsFor, typeof(VerseExtensions));

            //injector.Inject(typeof(Designator_Cancel), "DesignateThing", typeof(VerseExtensions));

            Globals.Logger.Info("Bootstrapped.");
        }
    }
}

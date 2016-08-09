using UnityEngine;
using Verse;

namespace BuildProductive
{
    class InitScript : MonoBehaviour
    {
        internal static void OnFinalizeMapInit()
        {
            MapIniterUtility.FinalizeMapInit();

            var go = new GameObject();
            go.AddComponent<InitScript>();
        }

        void Start()
        {
            var des = new Designator_BuildCopy();
            Bootstrapper.CopyDesignator = des;
            ReverseDesignatorDatabase.AllDesignators.Add(des);

            Destroy(gameObject);
        }
    }
}

using UnityEngine;
using Verse;

namespace BuildProductive
{
    class InitScript : MonoBehaviour
    {
        internal static void MapIniterUtility_FinalizeMapInit()
        {
            MapIniterUtility.FinalizeMapInit();

            // Delegate init to GameObject in order to execute on main thread
            var go = new GameObject();
            go.AddComponent<InitScript>();
        }

        void Start()
        {
            var des = new Designator_BuildCopy();
            Globals.CopyDesignator = des;
            ReverseDesignatorDatabase.AllDesignators.Add(des);

            Destroy(gameObject);
        }
    }
}

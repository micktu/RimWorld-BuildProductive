using System.Collections.Generic;
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
            Privates.InspectGizmoGrid_gizmoList = Privates.GizmoListField.GetValue(Privates.InspectGizmoGrid) as List<Gizmo>;
            Privates.InspectGizmoGrid_objList = Privates.ObjListField.GetValue(Privates.InspectGizmoGrid) as List<object>;

            var des = new Designator_BuildCopy();
            Globals.CopyDesignator = des;
            ReverseDesignatorDatabase.AllDesignators.Add(des);

            Globals.Logger.Info("Post-load initialized.");

            Destroy(gameObject);
        }
    }
}

using System.Reflection;

namespace Kindred_Spirit
{
    public static class Helper
    {
        public static T GetCopyOf<T>(this Component comp, T other) where T : Component
        {
            Type type = comp.GetType();
            if (type != other.GetType()) return null; // type mis-match
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
            PropertyInfo[] pinfos = type.GetProperties(flags);
            foreach (var pinfo in pinfos)
            {
                if (pinfo.CanWrite)
                {
                    try
                    {
                        pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }
            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos)
            {
                finfo.SetValue(comp, finfo.GetValue(other));
            }
            return comp as T;
        }
        public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component
        {
            return go.AddComponent<T>().GetCopyOf(toAdd) as T;
        }
        public static FsmFloat CreateFsmFloat(this PlayMakerFSM fsm, string floatName, float value)
        {
            var @new = new FsmFloat(floatName);
            @new.Value = value;

            fsm.FsmVariables.FloatVariables = fsm.FsmVariables.FloatVariables.Append(@new).ToArray();

            return @new;
        }

        public static FsmEvent CreateFsmEvent(this PlayMakerFSM fsm, string eventName)
        {
            var @new = new FsmEvent(eventName);

            fsm.Fsm.Events = fsm.Fsm.Events.Append(@new).ToArray();

            return @new;
        }

        public static FsmGameObject CreateFsmGameObject(this PlayMakerFSM fsm, string objname)
        {
            var @new = new FsmGameObject(objname);

            FsmGameObject[] newlist = new FsmGameObject[fsm.FsmVariables.GameObjectVariables.Length+1];

            int counter = 0;
            foreach (FsmGameObject item in fsm.FsmVariables.GameObjectVariables)
            {
                newlist[counter] = item;
                counter += 1;
            }
            newlist[counter] = @new;

            fsm.FsmVariables.GameObjectVariables = newlist;

            return @new;
        }

    }
}

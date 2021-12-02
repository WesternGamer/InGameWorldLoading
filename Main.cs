using HarmonyLib;
using System.Reflection;
using VRage.Plugins;

namespace InGameWorldLoading
{
    public class Main : IPlugin
    {
        public Main()
        {
            Harmony harmony = new Harmony("InGameWorldLoading");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void Dispose()
        {

        }

        public void Init(object gameInstance)
        {

        }

        public void Update()
        {

        }
    }
}

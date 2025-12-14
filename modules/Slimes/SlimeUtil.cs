using UnityEngine;
using MonomiPark.SlimeRancher;
using MonomiPark.SlimeRancher.Regions;

namespace bepinex_test.Modules.Slimes
{
    public static class SlimeUtil
    {
        public static int TeleportAll()
        {
            var playerPos = SRSingleton<SceneContext>.Instance.Player.transform.position;
            var vacuumables = Object.FindObjectsOfType<Vacuumable>();
            int count = 0;
            foreach (var vac in vacuumables)
            {
                if (vac == null) continue;
                var ident = vac.GetComponent<Identifiable>();
                if (ident != null)
                {
                    vac.transform.position = playerPos + Random.insideUnitSphere;
                    count++;
                }
            }
            Debug.Log($"Teleported {count} slimes!");
            return count;
        }
    }
}

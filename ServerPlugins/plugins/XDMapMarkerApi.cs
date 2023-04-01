using Color = UnityEngine.Color;
using Oxide.Core;
using System.Collections.Generic;
using VLB;
using UnityEngine;
using Oxide.Core.Plugins;
namespace Oxide.Plugins
{
    [Info("XDMapMarkerApi", "DezLife", "1.0.1")]
    public class XDMapMarkerApi : RustPlugin
    {
        
                private void OnPlayerConnected(BasePlayer player)
        {
            foreach (var marker in mapMarkers)
                if (marker != null)
                    marker.UpdateMarkers();
        }
        [PluginReference]
        private Plugin MarkerManager;

        private void API_CreateMarker(BaseEntity entity, string name,
            int duration = 0, float refreshRate = 3f, float radius = 0.4f,
            string displayName = "Marker", string colorMarker = "00FFFF", string colorOutline = "00FFFFFF")
        {
            CreateMarker(entity, duration, refreshRate, name, displayName, radius, colorMarker, colorOutline);
        }
        
        
        private void API_CreateMarker(Vector3 position, string name,
            int duration = 0, float refreshRate = 3f, float radius = 0.4f,
            string displayName = "Marker", string colorMarker = "00FFFF", string colorOutline = "00FFFFFF")
        {
            CreateMarker(position, duration, refreshRate, name, displayName, radius, colorMarker, colorOutline);
        }

        private void API_RemoveMarker(string name) => RemoveMarker(name);
        private void OnServerInitialized()
        {
            if (MarkerManager != null)
            {
                NextTick(() =>
                {
                    PrintError("Вам нужно удалить плагин MarkerManager. Данные плагины не совместимы!\n\nYou need to remove the MarkerManager plugin. These plugins are not compatible!");
                    Interface.Oxide.UnloadPlugin(Name);
                });
            }
        }
        private const string vendingPrefab = "assets/prefabs/deployable/vendingmachine/vending_mapmarker.prefab";

        private void RemoveMarkers()
        {
            foreach (CustomMapMarker marker in mapMarkers)
                if (marker.name != null)
                    UnityEngine.Object.Destroy(marker);
        }

        
                private class CustomMapMarker : MonoBehaviour
        {
            private MapMarkerGenericRadius generic;

            private void CreateMarkers()
            {
                vending = GameManager.server.CreateEntity(vendingPrefab, position)
                    .GetComponent<VendingMachineMapMarker>();
                vending.markerShopName = displayName;
                vending.enableSaving = false;
                vending.Spawn();

                generic = GameManager.server.CreateEntity(genericPrefab).GetComponent<MapMarkerGenericRadius>();
                generic.color1 = color1;
                generic.color2 = color2;
                generic.radius = radius;
                generic.alpha = 1f;
                generic.enableSaving = false;
                generic.SetParent(vending);
                generic.Spawn();

                if (duration != 0)
                {
                    Invoke(nameof(DestroyMakers), duration);
                }

                UpdateMarkers();

                if (refreshRate > 0f)
                {
                    if (asChild)
                    {
                        InvokeRepeating(nameof(UpdatePosition), refreshRate, refreshRate);
                    }
                    else
                    {
                        InvokeRepeating(nameof(UpdateMarkers), refreshRate, refreshRate);
                    }
                }
            }
            private bool asChild;
            public Color color1;

            public void UpdateMarkers()
            {
                vending.SendNetworkUpdate();
                generic.SendUpdate();
            }

            private void DestroyMakers()
            {
                if (vending.IsValid())
                {
                    vending.Kill();
                }

                if (generic.IsValid())
                {
                    generic.Kill();
                }
            }
            public float refreshRate;
            private VendingMachineMapMarker vending;
            public string displayName;
            public float radius;

            private void Start()
            {
                transform.position = position;
                asChild = parent != null;
                CreateMarkers();
            }
            public Color color2;
            public int duration;

            public string name;
		   		 		  						  	   		  	  			  	  			  		  		   			
            private void UpdatePosition()
            {
                if (asChild == true)
                {
                    if (parent.IsValid() == false)
                    {
                        Destroy(this);
                        return;
                    }
                    else
                    {
                        var pos = parent.transform.position;
                        transform.position = pos;
                        vending.transform.position = pos;
                    }
                }

                UpdateMarkers();
            }
            public Vector3 position;
            public BaseEntity parent;

            private void OnDestroy()
            {
                DestroyMakers();
            }
        }

        private void CreateMarker(BaseEntity entity, int duration, float refreshRate, string name, string displayName,
            float radius = 0.3f, string colorMarker = "00FFFF", string colorOutline = "00FFFFFF")
        {
            CustomMapMarker marker = entity.gameObject.GetOrAddComponent<CustomMapMarker>();
            marker.name = name;
            marker.displayName = displayName;
            marker.radius = radius;
            marker.refreshRate = refreshRate;
            marker.parent = entity;
            marker.position = entity.transform.position;
            marker.duration = duration;
            ColorUtility.TryParseHtmlString($"#{colorMarker}", out marker.color1);
            ColorUtility.TryParseHtmlString($"#{colorOutline}", out marker.color2);
            mapMarkers.Add(marker);
        }
        private void Unload() => RemoveMarkers();

                private const string genericPrefab = "assets/prefabs/tools/map/genericradiusmarker.prefab";

        private void RemoveMarker(string name)
        {
            foreach (CustomMapMarker marker in mapMarkers)
                if (marker.name != null && marker.name == name)
                    UnityEngine.Object.Destroy(marker);
        }

        
        
        private void CreateMarker(Vector3 position, int duration, float refreshRate, string name, string displayName,
            float radius = 0.3f, string colorMarker = "00FFFF", string colorOutline = "00FFFFFF")
        {
            CustomMapMarker marker = new GameObject().AddComponent<CustomMapMarker>();
            marker.name = name;
            marker.displayName = displayName;
            marker.radius = radius;
            marker.position = position;
            marker.duration = duration;
            marker.refreshRate = refreshRate;
            ColorUtility.TryParseHtmlString($"#{colorMarker}", out marker.color1);
            ColorUtility.TryParseHtmlString($"#{colorOutline}", out marker.color2);
            mapMarkers.Add(marker);
        }
        private List<CustomMapMarker> mapMarkers = new List<CustomMapMarker>();
            }  
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//  VolFx © NullTale - https://x.com/NullTale
namespace VolFx
{
    internal static class Installer
    {
        public const           string k_PrefsFile = nameof(VolFx) + "_Prefs.json";
        public static readonly string k_PrefsPath = "ProjectSettings" + Path.DirectorySeparatorChar + k_PrefsFile;

        public static SettingsData Data = new SettingsData();
        
        [Serializable]
        public class SettingsData
        {
            public bool _hideMessage;
        }
        
        // =======================================================================
        public static void LoadData()
        {
            if (File.Exists(k_PrefsPath) == false)
                SaveData();

            try
            {
                Data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(k_PrefsPath));
            }
            catch 
            {
                SaveData();
            }
        }

        public static void SaveData()
        {
            File.WriteAllText(k_PrefsPath, JsonUtility.ToJson(Data));
        }
        
        [DidReloadScripts]
        public static void CheckDefine()
        {
            //AssemblyReloadEvents.afterAssemblyReload += () => {};
            if (InPackage() == false)
                AddDefine("VOL_FX");
         
            // render pipeline directives
            /*switch (PiplineType())
            {
                case RpType.URP:
                {
                    if (HasDefine("VOL_FX_URP"))
                        return;
                    
                    var isOk = EditorUtility.DisplayDialog("• VolFx by NullTale ☄",
                                                           "VolFx now compiled for a different render pipeline. \n" +
                                                           "In order to work properly they need to add custom defines. \n \n" +
                                                           "Do you want to add custom defines to recompile it for URP ?",
                                                           "Recompile to Urp", "Not now");
                    if (isOk)
                    {
                        RemoveDefines("VOL_FX_BUILD_IN", "VOL_FX_URP", "VOL_FX_HDRP");
                        AddDefine("VOL_FX_URP");
                    }
                } break;
                case RpType.HDRP:
                {
                    if (HasDefine("VOL_FX_HDRP"))
                        return;
                    
                    var isOk = EditorUtility.DisplayDialog("• VolFx by NullTale ☄",
                                                           "VolFx now compiled for a different render pipeline \n" +
                                                           "In order to work properly they need to custom defines. \n \n" +
                                                           "Do you want to add custom defines to recompile it for HDRP ?",
                                                           "Recompile to Hdrp", "Not now");
                    if (isOk)
                    {
                        RemoveDefines("VOL_FX_BUILD_IN", "VOL_FX_URP", "VOL_FX_HDRP");
                        AddDefine("VOL_FX_HDRP");
                    }
                } break;
                case RpType.BuildIn:
                {
                    if (HasDefine("VOL_FX_BUILD_IN"))
                        return;
                    
                    var isOk = EditorUtility.DisplayDialog("• VolFx by NullTale ☄",
                                                           "VolFx now compiled for a different render pipeline \n" +
                                                           "In order to work properly they need to custom defines. \n \n" +
                                                           "Do you want to add custom defines to recompile it for HDRP ?",
                                                           "Recompile to BuildIn", "Not now");
                    if (isOk)
                    {
                        RemoveDefines("VOL_FX_BUILD_IN", "VOL_FX_URP", "VOL_FX_HDRP");
                        AddDefine("VOL_FX_BUILD_IN");
                    }
                } break;
                case RpType.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }*/
        }

        [DidReloadScripts]
        //[Shortcut("Debug Check", KeyCode.G)]
        public static void CheckInUse()
        {
#pragma warning disable CS4014
            _delay();
#pragma warning restore CS4014
            
            async Task _delay()
            {
                await Task.Yield();
                await Task.Yield();
                await Task.Yield();

                if (EditorApplication.isPlaying)
                    return;
                
                if (EditorApplication.isCompiling)
                {
                    while (EditorApplication.isCompiling)
                        await Task.Yield();
                    
                    await Task.Yield();
                }
                
                CheckInUseCall();
            }
        }
        
        public static void CheckInUseCall()
        {
            if (Application.isPlaying)
                return;
            
            switch (PiplineType())
            {
                case RpType.URP:
                {
                    LoadData();
                    
                    if (Data._hideMessage)
                        return;

                    if (_hasFeature<VolFx>(GetPipline() as UniversalRenderPipelineAsset))
                        return;
                        
                    var isOk = EditorUtility.DisplayDialogComplex("• VolFx by NullTale ☄",
                                                           $"The current Urp Asset ({GetPipline().name}) does not contain the VolFx feature" +
                                                           "this is needed for the effect to take effect.\n\n" +
                                                           "• Do you want to add VolFx to Urp assets in use?",
                                                           "Yes",
                                                           "Do not show again, I will personally customise the project",
                                                           "Not now");

                    switch (isOk)
                    {
                        // yes
                        case 0:
                        {
                            AddFeatures();   
                        } break;
                        // skip
                        case 1:
                        {
                            Data._hideMessage = true;
                            SaveData();
                        } break;
                        // no
                        case 2:
                            break;
                    }
                } break;
                case RpType.HDRP:
                    break;
                case RpType.BuildIn:
                    break;
                case RpType.Unknown:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public static void AddFeatures()
        {
            var features = AddRendererFeature<VolFx>().ToArray();
            foreach (var volFx in features)
            {
                foreach (var pass in _getPasses())
                {
                    SoCollectionUtils.AddItem(new SerializedObject(volFx).FindProperty(nameof(VolFx._passes)), pass);
                }
            }
            if (features.Length != 0)
            {
                Debug.Log($"<color=white>• Render features was added, don't forget to change the order of effects to suit your preferences ☄</color> \n" +
                          $"• You can select the asset by clicking a Log message, effects can be controlled via scene Volume components \n");
            }
        }
        
        public static IEnumerable<VolFx.Pass> _getPasses()
        {
            var types = TypeCache.GetTypesDerivedFrom(typeof(VolFx.Pass))
                                 .Where(type => type.IsAbstract == false && type.IsGenericTypeDefinition == false)
                                 .Except(new[] { typeof(BlitPass), typeof(MaskPass) })
                                 .OrderBy(n => n.Name)
                                 .Append(typeof(MaskPass))
                                 .ToList();

            
            var regex = new Regex("Pass$");
            
            foreach (var type in types)
            {
                var t = (VolFx.Pass)ScriptableObject.CreateInstance(type);
                t.name = ObjectNames.NicifyVariableName(regex.Replace(type.Name, ""));
                t.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                if (t is MaskPass _maskPass)
                    _maskPass._useVolumeSettings = true;
                
                yield return t;
            }
        }

        public static IEnumerable<T> AddRendererFeature<T>() where T : ScriptableRendererFeature
        {
            var handledDataObjects = new List<ScriptableRendererData>();
            
            foreach (var urpAsset in _getPiplines().Where(n => n != null).Distinct().ToArray())
            {
                var feature = _addFeature(urpAsset);
                if (feature != null)
                    yield return feature;
            }

            // -----------------------------------------------------------------------
            IEnumerable<UniversalRenderPipelineAsset> _getPiplines()
            {
                var asset = GetDefaultPipline() as UniversalRenderPipelineAsset;
                yield return asset;
                
                var levels = QualitySettings.names.Length;
                for (var level = 0; level < levels; level ++)
                {
                    // Fetch renderer data
                    asset = QualitySettings.GetRenderPipelineAssetAt(level) as UniversalRenderPipelineAsset;
                    yield return asset;
                }
            }
            
            T _addFeature(UniversalRenderPipelineAsset asset)
            {
                if (asset == null)
                    return null;

                // Do NOT use asset.LoadBuiltinRendererData().
                // It's a trap, see: https://github.com/Unity-Technologies/Graphics/blob/b57fcac51bb88e1e589b01e32fd610c991f16de9/Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRenderPipelineAsset.cs#L719
                var data = _getDefaultRenderer(asset);

                // This is needed in case multiple renderers share the same renderer data object.
                // If they do then we only handle it once.
                if (handledDataObjects.Contains(data))
                    return null;

                handledDataObjects.Add(data);

                // Create & add feature if not yet existing
                var found = data.rendererFeatures.OfType<T>().Any();
                if (found)
                    return null;
                
                // Create the feature
                var feature = ScriptableObject.CreateInstance<T>();
                feature.name = typeof(T).Name;

                // Add it to the renderer data.
                _addRenderFeature(data, feature);

                Selection.activeObject = data;
                EditorGUIUtility.PingObject(data);
                Debug.Log($"• {feature.name} added to the [{data.name.Replace(" Urp renderer", "")}]", data );
                return feature;
            }
        }
        
        private static bool _hasFeature<T>(UniversalRenderPipelineAsset asset) where T : ScriptableRendererFeature
        {
            if (asset == null)
                return false;

            // Do NOT use asset.LoadBuiltinRendererData().
            // It's a trap, see: https://github.com/Unity-Technologies/Graphics/blob/b57fcac51bb88e1e589b01e32fd610c991f16de9/Packages/com.unity.render-pipelines.universal/Runtime/Data/UniversalRenderPipelineAsset.cs#L719
            var data = _getDefaultRenderer(asset);
            
            var found = data.rendererFeatures.OfType<T>().Any();
            return found;
        }
        
        private static ScriptableRendererData _getDefaultRenderer(UniversalRenderPipelineAsset asset)
        {
            if (asset)
            {
                var rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset)
                                                                 .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance)
                                                                 .GetValue(asset);
                var defaultRendererIndex = _getDefaultRendererIndex(asset);

                return rendererDataList[defaultRendererIndex];
            }
            
            Debug.LogError("No Universal Render Pipeline is currently active.");
            return null;
            
            int _getDefaultRendererIndex(UniversalRenderPipelineAsset asset)
            {
                return (int)typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asset);
            }
        }

        private static void _addRenderFeature(ScriptableRendererData data, ScriptableRendererFeature feature)
        {
            // Let's mirror what Unity does.
            var serializedObject = new SerializedObject(data);

            var renderFeaturesProp = serializedObject.FindProperty("m_RendererFeatures"); // Let's hope they don't change these.
            var renderFeaturesMapProp = serializedObject.FindProperty("m_RendererFeatureMap");

            serializedObject.Update();

            // Store this new effect as a sub-asset so we can reference it safely afterwards.
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(data))
                AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out var guid, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            renderFeaturesProp.arraySize++;
            var componentProp = renderFeaturesProp.GetArrayElementAtIndex(renderFeaturesProp.arraySize - 1);
            componentProp.objectReferenceValue = feature;

            // Update GUID Map
            renderFeaturesMapProp.arraySize++;
            var guidProp = renderFeaturesMapProp.GetArrayElementAtIndex(renderFeaturesMapProp.arraySize - 1);
            guidProp.longValue = localId;

            // Force save / refresh
            if (EditorUtility.IsPersistent(data))
                AssetDatabase.SaveAssetIfDirty(data);

            serializedObject.ApplyModifiedProperties();
        }
        
        public enum RpType
        {
            URP,
            HDRP,
            BuildIn,
            Unknown,
        }
        
        public static RpType PiplineType()
        {
            var rpa = GetPipline();
            if (rpa != null)
            {
                switch (rpa.GetType().Name)
                {
                    case "UniversalRenderPipelineAsset": return RpType.URP;
                    case "HDRenderPipelineAsset":        return RpType.HDRP;
                }
            }

            return RpType.BuildIn;
        }
        
        public static RenderPipelineAsset GetPipline()
        {
            var result = QualitySettings.renderPipeline;
            if (result == null)
                result = GetDefaultPipline();
            
            return result;
        }
        
        public static RenderPipelineAsset GetDefaultPipline()
        {
#if UNITY_6000_0_OR_NEWER
            
            return GraphicsSettings.defaultRenderPipeline;
#else
            return  GraphicsSettings.renderPipelineAsset;
#endif
        }
        
        public static bool InPackage()
        {
            var assembly    = System.Reflection.Assembly.GetExecutingAssembly();
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(assembly);
            return packageInfo != null;
        }
        
        public static bool HasDefine(string def)
        {
            return GetDefines().Contains(def);
        }
        
        public static void RemoveDefines(params string[] defines)
        {
            var definesList = GetDefines().ToList();
            foreach (var def in defines)
            {
                if (definesList.Contains(def))
                {
                    definesList.Remove(def);
                    SetDefines(definesList);
                }
            }
        }

        public static List<string> GetDefines()
        {
            var target           = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var defines          = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            return defines.Split(';').ToList();
        }
        
        public static void AddDefine(string define)
        {
            var defs = GetDefines();
            if (defs.Contains(define))
                return;
            
            defs.Add(define);
            SetDefines(defs);
        }

        public static void SetDefines(List<string> definesList)
        {
            var target           = EditorUserBuildSettings.activeBuildTarget;
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(target);
            var defines          = string.Join(";", definesList.ToArray());
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
        }
    }
}
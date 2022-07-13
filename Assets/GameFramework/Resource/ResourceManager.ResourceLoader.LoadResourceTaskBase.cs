//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            private abstract class LoadResourceTaskBase : TaskBase
            {
                private static int s_Serial = 0; //加载任务，会永远自增

                private string m_AssetName;  //asset名字，为工程中assets开始的路径"Assets/GameMain/UI/UIForms/DialogForm.prefab"
                private Type m_AssetType;
                private ResourceInfo m_ResourceInfo;
                private string[] m_DependencyAssetNames; // 依赖的资源名字
                private readonly List<object> m_DependencyAssets; //这里传入object，是因为依赖的资源可能是 (UnityEngine.Material)，(UnityEngine.Texture2D) 多种不同类型
                private ResourceObject m_ResourceObject; //resourceObject 在内存中
                private DateTime m_StartTime;
                private int m_TotalDependencyAssetCount;

                public LoadResourceTaskBase()
                {
                    m_AssetName = null;
                    m_AssetType = null;
                    m_ResourceInfo = null;
                    m_DependencyAssetNames = null;
                    m_DependencyAssets = new List<object>();
                    m_ResourceObject = null;
                    m_StartTime = default(DateTime);
                    m_TotalDependencyAssetCount = 0;
                }

                public string AssetName
                {
                    get
                    {
                        return m_AssetName;
                    }
                }

                public Type AssetType
                {
                    get
                    {
                        return m_AssetType;
                    }
                }

                public ResourceInfo ResourceInfo
                {
                    get
                    {
                        return m_ResourceInfo;
                    }
                }

                public ResourceObject ResourceObject
                {
                    get
                    {
                        return m_ResourceObject;
                    }
                }

                public abstract bool IsScene
                {
                    get;
                }

                public DateTime StartTime
                {
                    get
                    {
                        return m_StartTime;
                    }
                    set
                    {
                        m_StartTime = value;
                    }
                }

                public int LoadedDependencyAssetCount
                {
                    get
                    {
                        return m_DependencyAssets.Count;
                    }
                }

                public int TotalDependencyAssetCount
                {
                    get
                    {
                        return m_TotalDependencyAssetCount;
                    }
                    set
                    {
                        m_TotalDependencyAssetCount = value;
                    }
                }

                public override string Description
                {
                    get
                    {
                        return m_AssetName;
                    }
                }

                public override void Clear()
                {
                    base.Clear();
                    m_AssetName = null;
                    m_AssetType = null;
                    m_ResourceInfo = null;
                    m_DependencyAssetNames = null;
                    m_DependencyAssets.Clear();
                    m_ResourceObject = null;
                    m_StartTime = default(DateTime);
                    m_TotalDependencyAssetCount = 0;
                }

                public string[] GetDependencyAssetNames()
                {
                    return m_DependencyAssetNames;
                }

                public List<object> GetDependencyAssets()
                {
                    return m_DependencyAssets;
                }

                //从resource里加载目标asset
                public void LoadMain(LoadResourceAgent agent, ResourceObject resourceObject)
                {
                    m_ResourceObject = resourceObject;
                    agent.Helper.LoadAsset(resourceObject.Target, AssetName, AssetType, IsScene);
                }

                public virtual void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                }

                public virtual void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                }

                public virtual void OnLoadAssetUpdate(LoadResourceAgent agent, LoadResourceProgress type, float progress)
                {
                }

                //加载完自己，也是一种依赖
                public virtual void OnLoadDependencyAsset(LoadResourceAgent agent, string dependencyAssetName, object dependencyAsset)
                {
                    GameFrameworkLog.Info("依赖资源加载完后放入{0}-->{1}", dependencyAssetName, dependencyAsset);
                    //依赖资源加载完后放入Assets/GameMain/Textures/part_star_dff.tif-->part_star_dff (UnityEngine.Texture2D)
                    //依赖资源加载完后放入Assets/GameMain/Materials/part_star_mat.mat-->part_star_mat (UnityEngine.Material)
                    m_DependencyAssets.Add(dependencyAsset);
                }

                protected void Initialize(string assetName, Type assetType, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, object userData)
                {
                    Initialize(++s_Serial, null, priority, userData);
                    m_AssetName = assetName;
                    m_AssetType = assetType;
                    m_ResourceInfo = resourceInfo;
                    m_DependencyAssetNames = dependencyAssetNames;
                    string sDepend = PublicTools.GetObj2Json(dependencyAssetNames);
                    GameFrameworkLog.Info("初始化加载任务{0}-->{1}", assetName, sDepend);
                }
            }
        }
    }
}

//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.ObjectPool;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 资源对象。为内存中的一个asset 对象，非gameobject
            /// </summary>
            private sealed class AssetObject : ObjectBase
            {
                private List<object> m_DependencyAssets;
                private object m_Resource;
                private IResourceHelper m_ResourceHelper;
                private ResourceLoader m_ResourceLoader;

                public AssetObject()
                {
                    m_DependencyAssets = new List<object>();
                    m_Resource = null;
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                public override bool CustomCanReleaseFlag
                {
                    get
                    {
                        int targetReferenceCount = 0;
                        m_ResourceLoader.m_AssetDependencyCount.TryGetValue(Target, out targetReferenceCount);
                        return base.CustomCanReleaseFlag && targetReferenceCount <= 0;
                    }
                }

                /// <summary>
                /// 创建出assetobject时，记录引用+1，所有被依赖的asset+1
                /// </summary>
                /// <param name="name"></param>
                /// <param name="target"></param>
                /// <param name="dependencyAssets"></param>
                /// <param name="resource"></param>
                /// <param name="resourceHelper"></param>
                /// <param name="resourceLoader"></param>
                /// <returns></returns>
                public static AssetObject Create(string name, object target, List<object> dependencyAssets, object resource, IResourceHelper resourceHelper, ResourceLoader resourceLoader)
                {
                    if (dependencyAssets == null)
                    {
                        throw new GameFrameworkException("Dependency assets is invalid.");
                    }

                    if (resource == null)
                    {
                        throw new GameFrameworkException("Resource is invalid.");
                    }

                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    AssetObject assetObject = ReferencePool.Acquire<AssetObject>();
                    assetObject.Initialize(name, target);
                    assetObject.m_DependencyAssets.AddRange(dependencyAssets);
                    assetObject.m_Resource = resource;
                    assetObject.m_ResourceHelper = resourceHelper;
                    assetObject.m_ResourceLoader = resourceLoader;

                    //所有依赖的asset 引用+1，有它自己+1吗
                    foreach (object dependencyAsset in dependencyAssets)
                    {
                        int referenceCount = 0;
                        GameFrameworkLog.Info("Asset-->{0}引用次数+1", dependencyAsset);
                        if (resourceLoader.m_AssetDependencyCount.TryGetValue(dependencyAsset, out referenceCount))
                        {
                            resourceLoader.m_AssetDependencyCount[dependencyAsset] = referenceCount + 1;
                        }
                        else
                        {
                            resourceLoader.m_AssetDependencyCount.Add(dependencyAsset, 1);
                        }
                    }

                    return assetObject;
                }

                public override void Clear()
                {
                    base.Clear();
                    m_DependencyAssets.Clear();
                    m_Resource = null;
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                protected internal override void OnUnspawn()
                {
                    base.OnUnspawn();
                    //卸载时把相应的资源也处理
                    foreach (object dependencyAsset in m_DependencyAssets)
                    {
                        m_ResourceLoader.m_AssetPool.Unspawn(dependencyAsset);
                    }
                }

                protected internal override void Release(bool isShutdown)
                {
                    GameFrameworkLog.Info("AssetObject.Release {0}", Target);
                    if (!isShutdown)
                    {
                        int targetReferenceCount = 0;
                        //不为0，不可以主动卸载
                        if (m_ResourceLoader.m_AssetDependencyCount.TryGetValue(Target, out targetReferenceCount) && targetReferenceCount > 0)
                        {
                            throw new GameFrameworkException(Utility.Text.Format("Asset target '{0}' reference count is '{1}' larger than 0.", Name, targetReferenceCount));
                        }

                        foreach (object dependencyAsset in m_DependencyAssets)
                        {
                            int referenceCount = 0;
                            if (m_ResourceLoader.m_AssetDependencyCount.TryGetValue(dependencyAsset, out referenceCount))
                            {
                                m_ResourceLoader.m_AssetDependencyCount[dependencyAsset] = referenceCount - 1;
                                //被依赖的asset -1
                            }
                            else
                            {
                                throw new GameFrameworkException(Utility.Text.Format("Asset target '{0}' dependency asset reference count is invalid.", Name));
                            }
                        }

                        m_ResourceLoader.m_ResourcePool.Unspawn(m_Resource);
                    }

                    m_ResourceLoader.m_AssetDependencyCount.Remove(Target);
                    m_ResourceLoader.m_AssetToResourceMap.Remove(Target);
                    m_ResourceHelper.Release(Target); //AssetBundle.Unload(true),这里传入的是asset
                }
            }
        }
    }
}

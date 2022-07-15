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
            /// 资源对象。
            /// </summary>
            private sealed class ResourceObject : ObjectBase
            {
                private List<object> m_DependencyResources;
                private IResourceHelper m_ResourceHelper;
                private ResourceLoader m_ResourceLoader;

                public ResourceObject()
                {
                    m_DependencyResources = new List<object>();
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                public override bool CustomCanReleaseFlag
                {
                    get
                    {
                        int targetReferenceCount = 0;
                        m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount);
                        return base.CustomCanReleaseFlag && targetReferenceCount <= 0;
                    }
                }

                public static ResourceObject Create(string name, object target, IResourceHelper resourceHelper, ResourceLoader resourceLoader)
                {
                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    ResourceObject resourceObject = ReferencePool.Acquire<ResourceObject>();
                    resourceObject.Initialize(name, target);
                    resourceObject.m_ResourceHelper = resourceHelper;
                    resourceObject.m_ResourceLoader = resourceLoader;
                    return resourceObject;
                }

                public override void Clear()
                {
                    base.Clear();
                    m_DependencyResources.Clear();
                    m_ResourceHelper = null;
                    m_ResourceLoader = null;
                }

                /// <summary>
                /// 所有引用这个asset 的resource引用+1。。例如a.asset 不是b.ab里的，但是a依赖b中的c.asset ,所以在加载a时，b的引用+1
                /// </summary>
                /// <param name="dependencyResource"></param>
                public void AddDependencyResource(object dependencyResource)
                {
                    if (Target == dependencyResource)
                    {
                        return;
                    }

                    if (m_DependencyResources.Contains(dependencyResource))
                    {
                        return;
                    }

                    m_DependencyResources.Add(dependencyResource);

                    int referenceCount = 0;
                    GameFrameworkLog.Info("Resource-->{0}引用+1", dependencyResource);
                    if (m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                    {
                        m_ResourceLoader.m_ResourceDependencyCount[dependencyResource] = referenceCount + 1;
                    }
                    else
                    {
                        m_ResourceLoader.m_ResourceDependencyCount.Add(dependencyResource, 1);
                    }
                }

                protected internal override void Release(bool isShutdown)
                {
                    if (!isShutdown)
                    {
                        int targetReferenceCount = 0;
                        if (m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(Target, out targetReferenceCount) && targetReferenceCount > 0)
                        {
                            throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' reference count is '{1}' larger than 0.", Name, targetReferenceCount));
                        }

                        foreach (object dependencyResource in m_DependencyResources)
                        {
                            int referenceCount = 0;
                            if (m_ResourceLoader.m_ResourceDependencyCount.TryGetValue(dependencyResource, out referenceCount))
                            {
                                m_ResourceLoader.m_ResourceDependencyCount[dependencyResource] = referenceCount - 1;
                                //只会-1，不会对为 0 的Assetbundle进行卸载
                            }
                            else
                            {
                                throw new GameFrameworkException(Utility.Text.Format("Resource target '{0}' dependency asset reference count is invalid.", Name));
                            }
                        }
                    }

                    m_ResourceLoader.m_ResourceDependencyCount.Remove(Target);
                    m_ResourceHelper.Release(Target); //AssetBundle.Unload(true) ,也可能是asset 执行释放，也可能是scene执行释放
                }
            }
        }
    }
}

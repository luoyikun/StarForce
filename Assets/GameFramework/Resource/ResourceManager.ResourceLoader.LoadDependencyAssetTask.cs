//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            private sealed class LoadDependencyAssetTask : LoadResourceTaskBase
            {
                private LoadResourceTaskBase m_MainTask; //主任务，是指被依赖的目标a。。例如要加载a，依赖c。a就是的主任务。。。可能a也是别的目标任务的依赖任务

                public LoadDependencyAssetTask()
                {
                    m_MainTask = null;
                }

                public override bool IsScene
                {
                    get
                    {
                        return false;
                    }
                }

                public static LoadDependencyAssetTask Create(string assetName, int priority, ResourceInfo resourceInfo, string[] dependencyAssetNames, LoadResourceTaskBase mainTask, object userData)
                {
                    GameFrameworkLog.Info("创建依赖资源任务{0}", assetName);
                    LoadDependencyAssetTask loadDependencyAssetTask = ReferencePool.Acquire<LoadDependencyAssetTask>();
                    loadDependencyAssetTask.Initialize(assetName, null, priority, resourceInfo, dependencyAssetNames, userData);
                    loadDependencyAssetTask.m_MainTask = mainTask; //这里是主任务，也可能 a依赖b，b依赖c  ，对c而言，b就是任务
                    loadDependencyAssetTask.m_MainTask.TotalDependencyAssetCount++;
                    return loadDependencyAssetTask;
                }

                public override void Clear()
                {
                    base.Clear();
                    m_MainTask = null;
                }

                public override void OnLoadAssetSuccess(LoadResourceAgent agent, object asset, float duration)
                {
                    base.OnLoadAssetSuccess(agent, asset, duration);
                    m_MainTask.OnLoadDependencyAsset(agent, AssetName, asset);
                }

                public override void OnLoadAssetFailure(LoadResourceAgent agent, LoadResourceStatus status, string errorMessage)
                {
                    base.OnLoadAssetFailure(agent, status, errorMessage);
                    m_MainTask.OnLoadAssetFailure(agent, LoadResourceStatus.DependencyError, Utility.Text.Format("Can not load dependency asset '{0}', internal status '{1}', internal error message '{2}'.", AssetName, status, errorMessage));
                }
            }
        }
    }
}

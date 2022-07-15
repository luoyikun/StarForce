﻿//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;

namespace GameFramework.Resource
{
    internal sealed partial class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private sealed partial class ResourceLoader
        {
            /// <summary>
            /// 加载资源代理。
            /// </summary>
            private sealed partial class LoadResourceAgent : ITaskAgent<LoadResourceTaskBase>
            {
                private static readonly Dictionary<string, string> s_CachedResourceNames = new Dictionary<string, string>(StringComparer.Ordinal); //资源名，资源全路径？好像没作用
                private static readonly HashSet<string> s_LoadingAssetNames = new HashSet<string>(StringComparer.Ordinal); //正在加载中的asset，防止重复开启加载
                private static readonly HashSet<string> s_LoadingResourceNames = new HashSet<string>(StringComparer.Ordinal);//正在加载中的resource，即ab

                private readonly ILoadResourceAgentHelper m_Helper;
                private readonly IResourceHelper m_ResourceHelper;
                private readonly ResourceLoader m_ResourceLoader;
                private readonly string m_ReadOnlyPath;
                private readonly string m_ReadWritePath;
                private readonly DecryptResourceCallback m_DecryptResourceCallback;
                private LoadResourceTaskBase m_Task;

                /// <summary>
                /// 初始化加载资源代理的新实例。
                /// </summary>
                /// <param name="loadResourceAgentHelper">加载资源代理辅助器。</param>
                /// <param name="resourceHelper">资源辅助器。</param>
                /// <param name="resourceLoader">加载资源器。</param>
                /// <param name="readOnlyPath">资源只读区路径。</param>
                /// <param name="readWritePath">资源读写区路径。</param>
                /// <param name="decryptResourceCallback">解密资源回调函数。</param>
                public LoadResourceAgent(ILoadResourceAgentHelper loadResourceAgentHelper, IResourceHelper resourceHelper, ResourceLoader resourceLoader, string readOnlyPath, string readWritePath, DecryptResourceCallback decryptResourceCallback)
                {
                    if (loadResourceAgentHelper == null)
                    {
                        throw new GameFrameworkException("Load resource agent helper is invalid.");
                    }

                    if (resourceHelper == null)
                    {
                        throw new GameFrameworkException("Resource helper is invalid.");
                    }

                    if (resourceLoader == null)
                    {
                        throw new GameFrameworkException("Resource loader is invalid.");
                    }

                    if (decryptResourceCallback == null)
                    {
                        throw new GameFrameworkException("Decrypt resource callback is invalid.");
                    }

                    m_Helper = loadResourceAgentHelper;
                    GameFrameworkLog.Info("加载资源代理辅助器{0}", m_Helper.GetType()); //UnityGameFramework.Runtime.DefaultLoadResourceAgentHelper
                    m_ResourceHelper = resourceHelper;
                    m_ResourceLoader = resourceLoader;
                    m_ReadOnlyPath = readOnlyPath;
                    m_ReadWritePath = readWritePath;
                    m_DecryptResourceCallback = decryptResourceCallback;
                    m_Task = null;
                }

                public ILoadResourceAgentHelper Helper
                {
                    get
                    {
                        return m_Helper;
                    }
                }

                /// <summary>
                /// 获取加载资源任务。
                /// </summary>
                public LoadResourceTaskBase Task
                {
                    get
                    {
                        return m_Task;
                    }
                }

                /// <summary>
                /// 初始化加载资源代理。
                /// </summary>
                public void Initialize()
                {
                    m_Helper.LoadResourceAgentHelperUpdate += OnLoadResourceAgentHelperUpdate;
                    m_Helper.LoadResourceAgentHelperReadFileComplete += OnLoadResourceAgentHelperReadFileComplete;
                    m_Helper.LoadResourceAgentHelperReadBytesComplete += OnLoadResourceAgentHelperReadBytesComplete;
                    m_Helper.LoadResourceAgentHelperParseBytesComplete += OnLoadResourceAgentHelperParseBytesComplete;
                    m_Helper.LoadResourceAgentHelperLoadComplete += OnLoadResourceAgentHelperLoadComplete;
                    m_Helper.LoadResourceAgentHelperError += OnLoadResourceAgentHelperError;
                }

                /// <summary>
                /// 加载资源代理轮询。
                /// </summary>
                /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
                /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
                public void Update(float elapseSeconds, float realElapseSeconds)
                {
                }

                /// <summary>
                /// 关闭并清理加载资源代理。
                /// </summary>
                public void Shutdown()
                {
                    Reset();
                    m_Helper.LoadResourceAgentHelperUpdate -= OnLoadResourceAgentHelperUpdate;
                    m_Helper.LoadResourceAgentHelperReadFileComplete -= OnLoadResourceAgentHelperReadFileComplete;
                    m_Helper.LoadResourceAgentHelperReadBytesComplete -= OnLoadResourceAgentHelperReadBytesComplete;
                    m_Helper.LoadResourceAgentHelperParseBytesComplete -= OnLoadResourceAgentHelperParseBytesComplete;
                    m_Helper.LoadResourceAgentHelperLoadComplete -= OnLoadResourceAgentHelperLoadComplete;
                    m_Helper.LoadResourceAgentHelperError -= OnLoadResourceAgentHelperError;
                }

                public static void Clear()
                {
                    s_CachedResourceNames.Clear();
                    s_LoadingAssetNames.Clear();
                    s_LoadingResourceNames.Clear();
                }

                /// <summary>
                /// 开始处理加载资源任务。 在taskPool.update中把空闲代理分配给到工作代理中
                /// </summary>
                /// <param name="task">要处理的加载资源任务。</param>
                /// <returns>开始处理任务的状态。</returns>
                public StartTaskStatus Start(LoadResourceTaskBase task)
                {
                    if (task == null)
                    {
                        throw new GameFrameworkException("Task is invalid.");
                    }

                    m_Task = task;
                    m_Task.StartTime = DateTime.UtcNow;
                    ResourceInfo resourceInfo = m_Task.ResourceInfo;
                    //资源信息是ab信息，文件大小，加载方式
                    if (!resourceInfo.Ready)
                    {
                        GameFrameworkLog.Info("资源信息未准备好");
                        m_Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }
                    
                    //资源正在加载中
                    if (IsAssetLoading(m_Task.AssetName))
                    {
                        //针对同一时间开启加载同一个asset的任务
                        GameFrameworkLog.Info("资源正在加载中{0}",m_Task.AssetName);
                        m_Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    //任务不是场景，说明要实例化
                    if (!m_Task.IsScene)
                    {
                        //从对象池里拿一个，已经可以从ab里实例出来asset，任务做完了。
                        //是否是依赖的asset全部准备好了，才可以提取出一个
                        AssetObject assetObject = m_ResourceLoader.m_AssetPool.Spawn(m_Task.AssetName);
                        if (assetObject != null)
                        {
                            OnAssetObjectReady(assetObject);
                            return StartTaskStatus.Done;
                        }
                    }

                    //遍历依赖
                    foreach (string dependencyAssetName in m_Task.GetDependencyAssetNames())
                    {
                        //如果依赖asset不能spawn，接着等待
                        if (!m_ResourceLoader.m_AssetPool.CanSpawn(dependencyAssetName))
                        {
                            GameFrameworkLog.Info("{0}依赖项{1}未加载完成", m_Task.AssetName,dependencyAssetName);
                            m_Task.StartTime = default(DateTime);
                            return StartTaskStatus.HasToWait;
                        }
                    }

                    //resource正在加载，等待，防止重复加载，例如task1，task2 都是加载同一个resource
                    string resourceName = resourceInfo.ResourceName.Name;
                    if (IsResourceLoading(resourceName))
                    {
                        m_Task.StartTime = default(DateTime);
                        return StartTaskStatus.HasToWait;
                    }

                    s_LoadingAssetNames.Add(m_Task.AssetName);

                    //从resource对象池中取出，说明任务可以接着执行
                    ResourceObject resourceObject = m_ResourceLoader.m_ResourcePool.Spawn(resourceName);
                    if (resourceObject != null)
                    {
                        OnResourceObjectReady(resourceObject);
                        return StartTaskStatus.CanResume;
                    }

                    s_LoadingResourceNames.Add(resourceName);

                    string fullPath = null;
                    if (!s_CachedResourceNames.TryGetValue(resourceName, out fullPath))
                    {
                        fullPath = Utility.Path.GetRegularPath(Path.Combine(resourceInfo.StorageInReadOnly ? m_ReadOnlyPath : m_ReadWritePath, resourceInfo.UseFileSystem ? resourceInfo.FileSystemName : resourceInfo.ResourceName.FullName));
                        s_CachedResourceNames.Add(resourceName, fullPath);
                    }

                    //根据resource的加载方式
                    if (resourceInfo.LoadType == LoadType.LoadFromFile)
                    {
                        if (resourceInfo.UseFileSystem)
                        {
                            IFileSystem fileSystem = m_ResourceLoader.m_ResourceManager.GetFileSystem(resourceInfo.FileSystemName, resourceInfo.StorageInReadOnly);
                            m_Helper.ReadFile(fileSystem, resourceInfo.ResourceName.FullName);
                        }
                        else
                        {
                            m_Helper.ReadFile(fullPath);
                        }
                    }
                    else if (resourceInfo.LoadType == LoadType.LoadFromMemory || resourceInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || resourceInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt)
                    {
                        if (resourceInfo.UseFileSystem)
                        {
                            IFileSystem fileSystem = m_ResourceLoader.m_ResourceManager.GetFileSystem(resourceInfo.FileSystemName, resourceInfo.StorageInReadOnly);
                            m_Helper.ReadBytes(fileSystem, resourceInfo.ResourceName.FullName);
                        }
                        else
                        {
                            m_Helper.ReadBytes(fullPath);
                        }
                    }
                    else
                    {
                        throw new GameFrameworkException(Utility.Text.Format("Resource load type '{0}' is not supported.", resourceInfo.LoadType));
                    }

                    return StartTaskStatus.CanResume;
                }

                /// <summary>
                /// 重置加载资源代理。
                /// </summary>
                public void Reset()
                {
                    m_Helper.Reset();
                    m_Task = null;
                }

                private static bool IsAssetLoading(string assetName)
                {
                    return s_LoadingAssetNames.Contains(assetName);
                }

                private static bool IsResourceLoading(string resourceName)
                {
                    return s_LoadingResourceNames.Contains(resourceName);
                }

                private void OnAssetObjectReady(AssetObject assetObject)
                {
                    m_Helper.Reset();

                    object asset = assetObject.Target;
                    if (m_Task.IsScene)
                    {
                        m_ResourceLoader.m_SceneToAssetMap.Add(m_Task.AssetName, asset);
                    }

                    m_Task.OnLoadAssetSuccess(this, asset, (float)(DateTime.UtcNow - m_Task.StartTime).TotalSeconds);
                    m_Task.Done = true;
                }

                private void OnResourceObjectReady(ResourceObject resourceObject)
                {
                    m_Task.LoadMain(this, resourceObject);
                }

                private void OnError(LoadResourceStatus status, string errorMessage)
                {
                    m_Helper.Reset();
                    m_Task.OnLoadAssetFailure(this, status, errorMessage);
                    s_LoadingAssetNames.Remove(m_Task.AssetName);
                    s_LoadingResourceNames.Remove(m_Task.ResourceInfo.ResourceName.Name);
                    m_Task.Done = true;
                }

                private void OnLoadResourceAgentHelperUpdate(object sender, LoadResourceAgentHelperUpdateEventArgs e)
                {
                    m_Task.OnLoadAssetUpdate(this, e.Type, e.Progress);
                }

               /// <summary>
               /// resource加载完成
               /// </summary>
               /// <param name="sender"></param>
               /// <param name="e"></param>
                private void OnLoadResourceAgentHelperReadFileComplete(object sender, LoadResourceAgentHelperReadFileCompleteEventArgs e)
                {
                    GameFrameworkLog.Info("resource加载完成{0}", m_Task.ResourceInfo.ResourceName.Name);
                    ResourceObject resourceObject = ResourceObject.Create(m_Task.ResourceInfo.ResourceName.Name, e.Resource, m_ResourceHelper, m_ResourceLoader);
                    m_ResourceLoader.m_ResourcePool.Register(resourceObject, true);
                    s_LoadingResourceNames.Remove(m_Task.ResourceInfo.ResourceName.Name);
                    OnResourceObjectReady(resourceObject);
                }

                private void OnLoadResourceAgentHelperReadBytesComplete(object sender, LoadResourceAgentHelperReadBytesCompleteEventArgs e)
                {
                    byte[] bytes = e.GetBytes();
                    ResourceInfo resourceInfo = m_Task.ResourceInfo;
                    if (resourceInfo.LoadType == LoadType.LoadFromMemoryAndQuickDecrypt || resourceInfo.LoadType == LoadType.LoadFromMemoryAndDecrypt)
                    {
                        m_DecryptResourceCallback(bytes, 0, bytes.Length, resourceInfo.ResourceName.Name, resourceInfo.ResourceName.Variant, resourceInfo.ResourceName.Extension, resourceInfo.StorageInReadOnly, resourceInfo.FileSystemName, (byte)resourceInfo.LoadType, resourceInfo.Length, resourceInfo.HashCode);
                    }

                    m_Helper.ParseBytes(bytes);
                }

                private void OnLoadResourceAgentHelperParseBytesComplete(object sender, LoadResourceAgentHelperParseBytesCompleteEventArgs e)
                {
                    ResourceObject resourceObject = ResourceObject.Create(m_Task.ResourceInfo.ResourceName.Name, e.Resource, m_ResourceHelper, m_ResourceLoader);
                    m_ResourceLoader.m_ResourcePool.Register(resourceObject, true);
                    s_LoadingResourceNames.Remove(m_Task.ResourceInfo.ResourceName.Name);
                    OnResourceObjectReady(resourceObject);
                }

                /// <summary>
                /// asset加载完成
                /// </summary>
                /// <param name="sender"></param>
                /// <param name="e"></param>
                private void OnLoadResourceAgentHelperLoadComplete(object sender, LoadResourceAgentHelperLoadCompleteEventArgs e)
                {
                    
                    AssetObject assetObject = null;
                    if (m_Task.IsScene) //如果是场景
                    {
                        assetObject = m_ResourceLoader.m_AssetPool.Spawn(m_Task.AssetName);
                    }

                    if (assetObject == null)
                    {
                        List<object> dependencyAssets = m_Task.GetDependencyAssets();
                        assetObject = AssetObject.Create(m_Task.AssetName, e.Asset, dependencyAssets, m_Task.ResourceObject.Target, m_ResourceHelper, m_ResourceLoader);
                        GameFrameworkLog.Info("asset-->{0}加载完成,并且创建assetObject到m_AssetPool缓冲池中", m_Task.AssetName);
                        m_ResourceLoader.m_AssetPool.Register(assetObject, true);
                        m_ResourceLoader.m_AssetToResourceMap.Add(e.Asset, m_Task.ResourceObject.Target);
                        foreach (object dependencyAsset in dependencyAssets)
                        {
                            object dependencyResource = null;
                            if (m_ResourceLoader.m_AssetToResourceMap.TryGetValue(dependencyAsset, out dependencyResource))
                            {
                                m_Task.ResourceObject.AddDependencyResource(dependencyResource); //所有依赖这个asset的resource引用+1
                            }
                            else
                            {
                                throw new GameFrameworkException("Can not find dependency resource.");
                            }
                        }
                    }

                    s_LoadingAssetNames.Remove(m_Task.AssetName);
                    OnAssetObjectReady(assetObject);
                }

                private void OnLoadResourceAgentHelperError(object sender, LoadResourceAgentHelperErrorEventArgs e)
                {
                    OnError(e.Status, e.ErrorMessage);
                }
            }
        }
    }
}

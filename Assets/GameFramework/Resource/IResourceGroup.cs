//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections.Generic;

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源组接口。可以获取资源组的参数的一些API，资源为什么要分组管理呢？比如某个模块的资源更新或者其他的，整体管理可以更加方便等等。  
    /// </summary>
    public interface IResourceGroup
    {
        /// <summary>
        /// 获取资源组名称。
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// 获取资源组是否准备完毕。
        /// </summary>
        bool Ready
        {
            get;
        }

        /// <summary>
        /// 获取资源组包含资源数量。
        /// </summary>
        int TotalCount
        {
            get;
        }

        /// <summary>
        /// 获取资源组中已准备完成资源数量。
        /// </summary>
        int ReadyCount
        {
            get;
        }

        /// <summary>
        /// 获取资源组包含资源的总大小。
        /// </summary>
        long TotalLength
        {
            get;
        }

        /// <summary>
        /// 获取资源组包含资源压缩后的总大小。
        /// </summary>
        long TotalCompressedLength
        {
            get;
        }

        /// <summary>
        /// 获取资源组中已准备完成资源的总大小。
        /// </summary>
        long ReadyLength
        {
            get;
        }

        /// <summary>
        /// 获取资源组中已准备完成资源压缩后的总大小。
        /// </summary>
        long ReadyCompressedLength
        {
            get;
        }

        /// <summary>
        /// 获取资源组的完成进度。
        /// </summary>
        float Progress
        {
            get;
        }

        /// <summary>
        /// 获取资源组包含的资源名称列表。
        /// </summary>
        /// <returns>资源组包含的资源名称列表。</returns>
        string[] GetResourceNames();

        /// <summary>
        /// 获取资源组包含的资源名称列表。
        /// </summary>
        /// <param name="results">资源组包含的资源名称列表。</param>
        void GetResourceNames(List<string> results);
    }
}

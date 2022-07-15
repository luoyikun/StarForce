//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Runtime.InteropServices;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceAnalyzerController
    {
        [StructLayout(LayoutKind.Auto)]
        private struct Stamp
        {
            private readonly string m_HostAssetName;
            private readonly string m_DependencyAssetName;

            /// <summary>
            /// 印记
            /// </summary>
            /// <param name="hostAssetName">主目标asset</param>
            /// <param name="dependencyAssetName">被依赖的asset</param>
            public Stamp(string hostAssetName, string dependencyAssetName)
            {
                GameFrameworkLog.Info("{0}印记{1}",hostAssetName,dependencyAssetName);
                m_HostAssetName = hostAssetName;
                m_DependencyAssetName = dependencyAssetName;
            }

            public string HostAssetName
            {
                get
                {
                    return m_HostAssetName;
                }
            }

            public string DependencyAssetName
            {
                get
                {
                    return m_DependencyAssetName;
                }
            }
        }
    }
}

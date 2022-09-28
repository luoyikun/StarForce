//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Collections.Generic;
using System.Linq;

namespace UnityGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceAnalyzerController
    {
        private sealed class CircularDependencyChecker
        {
            private readonly Stamp[] m_Stamps;

            public CircularDependencyChecker(Stamp[] stamps)
            {
                m_Stamps = stamps;
                PublicTools.DebugObj2(m_Stamps, "m_Stamps","D:/Stamp.json"); //只有场景，mat，prefab 文件
            }

            public string[][] Check()
            {
                //唯一的string 的数列
                HashSet<string> hosts = new HashSet<string>();

                //所有有依赖的asset
                foreach (Stamp stamp in m_Stamps)
                {
                    hosts.Add(stamp.HostAssetName);
                }
                PublicTools.DebugObj(hosts, "hosts"); //只有场景，mat，prefab 文件 

                List<string[]> results = new List<string[]>();
                foreach (string host in hosts) //主资源被引用情况
                {
                    LinkedList<string> route = new LinkedList<string>();
                    HashSet<string> visited = new HashSet<string>();
                    if (Check(host, route, visited))
                    {
                        results.Add(route.ToArray());
                    }
                }

                return results.ToArray();
            }

            private bool Check(string host, LinkedList<string> route, HashSet<string> visited)
            {
                //把主资源，放入参观表，路径表
                visited.Add(host);
                route.AddLast(host);

                foreach (Stamp stamp in m_Stamps)
                {
                    if (host != stamp.HostAssetName) //只找当前host的依赖情况
                    {
                        GameFrameworkLog.Info("不是主资源名跳过{0}", host); 
                        continue;
                    }

                    //如果参观表包含了依赖 stamp的依赖
                    if (visited.Contains(stamp.DependencyAssetName))
                    {
                        //插入路径表最后项目返回
                        route.AddLast(stamp.DependencyAssetName);
                        return true;
                    }

                    //递归检查 当前stamp的依赖 b
                    if (Check(stamp.DependencyAssetName, route, visited))
                    {
                        return true;
                    }
                }

                //路径表移除最后一个
                route.RemoveLast();
                //参观表移除host
                visited.Remove(host);
                return false;
            }
        }
    }
}

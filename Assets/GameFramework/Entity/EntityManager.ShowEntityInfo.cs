//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

namespace GameFramework.Entity
{
    internal sealed partial class EntityManager : GameFrameworkModule, IEntityManager
    {
        //显示 entityinfo，只是在加载中使用，加载完成后回到引用池
        private sealed class ShowEntityInfo : IReference
        {
            private int m_SerialId; //序列id（加载id）
            private int m_EntityId;  //创建后实体id，本地为负数--，网络为正数++
            private EntityGroup m_EntityGroup;
            private object m_UserData;

            public ShowEntityInfo()
            {
                m_SerialId = 0;
                m_EntityId = 0;
                m_EntityGroup = null;
                m_UserData = null;
            }

            public int SerialId
            {
                get
                {
                    return m_SerialId;
                }
            }

            public int EntityId
            {
                get
                {
                    return m_EntityId;
                }
            }

            public EntityGroup EntityGroup
            {
                get
                {
                    return m_EntityGroup;
                }
            }

            public object UserData
            {
                get
                {
                    return m_UserData;
                }
            }

            public static ShowEntityInfo Create(int serialId, int entityId, EntityGroup entityGroup, object userData)
            {
                ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
                showEntityInfo.m_SerialId = serialId;
                showEntityInfo.m_EntityId = entityId;
                showEntityInfo.m_EntityGroup = entityGroup;
                showEntityInfo.m_UserData = userData;
                return showEntityInfo;
            }

            public void Clear()
            {
                m_SerialId = 0;
                m_EntityId = 0;
                m_EntityGroup = null;
                m_UserData = null;
            }
        }
    }
}

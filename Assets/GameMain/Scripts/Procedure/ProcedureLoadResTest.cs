using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameFramework.DataTable;
using GameFramework.Event;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;
namespace StarForce
{
    public class ProcedureLoadResTest : ProcedureBase
    {
        int m_entityID = 0;
        public override bool UseNativeDialog
        {
            get
            {
                return false;
            }
        }

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            //加载资源测试

            

        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                m_entityID = GameEntry.Entity.GenerateSerialId();
                GameEntry.Entity.ShowAsteroid(
                new AsteroidData(m_entityID, 60000)
                {
                    //位置随机
                    Position = Vector3.zero,
                }
                );
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                GameEntry.Entity.HideEntity(m_entityID);
            }
        }
    }
}

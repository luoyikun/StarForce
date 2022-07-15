using GameFramework;
using GameFramework.Procedure;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;
using GameFramework.Resource;
using StarForce;
using UnityGameFramework.Runtime;
public class ProcedureTest : GameFramework.Procedure.ProcedureBase
{
    protected override void OnEnter(ProcedureOwner procedureOwner)
    {
        base.OnEnter(procedureOwner);

        Debug.Log("进入测试流程");

        TestLoadResource();
    }

    void TestLoadResource()
    {
        //public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks)
        //StarForce.GameEntry.Resource.LoadAsset("Assets/GameMain/luoyikun/Cube1.prefab",new GameFramework.Resource.LoadAssetCallbacks(LoadAssetSuccessCallback));
        StarForce.GameEntry.Resource.LoadAsset("Assets/GameMain/UI/UIForms/AboutForm.prefab", new GameFramework.Resource.LoadAssetCallbacks(LoadAssetSuccessCallback));
    }

    void LoadAssetSuccessCallback(string assetName, object asset, float duration, object userData)
    {
        GameFrameworkLog.Info("加载成功{0}，{1}", assetName, asset);

        //StarForce.GameEntry.Resource.UnloadAsset(asset);
    }
}

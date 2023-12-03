using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

public class HPBar : MonoBehaviour
{

    public Image HP;
    private float hpTargetPer;
    private float hpCurPer;
    private bool updatingHP=false;
    private bool hpFlag = false;
    private float prehp;
    public Text Bullets;

    public void Updatehp(float curhp)
    {
        if (Mathf.Abs(curhp -prehp)>0.1f)
        {
            float targetper = curhp / 100.0f;
            prehp = curhp;
            UpdateHP(targetper);
        }

    }

    public void UpdateBullet(int curBullets,int reserveBullets)
    {
        Bullets.text = $"{curBullets}/{reserveBullets}";
    }
    
    private  void UpdateHP(float targetper)
    {
      
        hpTargetPer = targetper;
        HP.GetComponent<Image>().material.SetFloat("_TargetPer",hpTargetPer);
        hpFlag = hpTargetPer > hpCurPer ;
        if (!updatingHP)
        {
            UpdateHPTask().Forget();
        }
    }  
   
   
    private async UniTaskVoid UpdateHPTask()
    {
        updatingHP = true;
        while (true)
        {
            hpCurPer +=hpCurPer<hpTargetPer? Time.fixedDeltaTime *1:-Time.fixedDeltaTime *1;
            HP.GetComponent<Image>().material.SetFloat("_CurPer", hpCurPer);
            await UniTask.Yield(PlayerLoopTiming.FixedUpdate,this.GetCancellationTokenOnDestroy());
            if (hpFlag)
            {
                if (hpCurPer >= hpTargetPer) break;
            }
            else
            {
                if (hpCurPer <= hpTargetPer) break;
            }
        }
        hpCurPer = hpTargetPer;
        HP.GetComponent<Image>().material.SetFloat("_CurPer", hpCurPer);
        updatingHP = false;
    }
}

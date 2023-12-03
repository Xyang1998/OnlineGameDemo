using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimController : MonoBehaviour
{
    private Queue<AnimInputType> AnimQueue;
    private bool isFinish = true;
    private Animator animator;
    public int selfid;
    public bool islocal;
    private int actionNum;
    private GameController _controller;
    public int index1 ;
    public int index2 ;
    public int index3 ;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        AnimQueue = new Queue<AnimInputType>();
        _controller = FindObjectOfType<GameController>();
    }

    public void Update()
    {
        if (!islocal)
        {
            if (GameConfig.AnimActionDict.ContainsKey(selfid))
            {
                
                if (GameConfig.AnimActionDict[selfid].Count != 0)
                {
                    Debug.LogWarning(GameConfig.AnimActionDict[selfid].Count);
                    actionNum = GameConfig.AnimActionDict[selfid].Count;
                    for (int i = 0; i < actionNum; i++)
                    {
                        var t = GameConfig.AnimActionDict[selfid][i];
                        AddAnimToQueue(t);
                    }
                    GameConfig.AnimActionDict[selfid].RemoveRange(0, actionNum);
                }
            }
        }
    }

    public void AddAnimToQueue(AnimInputType animGameSystemInput)
    {
        lock (AnimQueue)
        {
            AnimQueue.Enqueue(animGameSystemInput);
            ContinuePlayAnim();
        }

        
    }

    public void ContinuePlayAnim() //code用
    {
        if (AnimQueue.Count != 0)
        {
            AnimInputType temp = AnimQueue.Dequeue();
            PlayAnim(temp);
            isFinish = false;

        }
    }

    public void Respawn()
    {
        animator.SetBool("Dead", false);
        animator.SetLayerWeight(index1, 1);
        animator.SetLayerWeight(index2, 1);
        animator.SetLayerWeight(index3, 0);
    }

    public void Death()
    {           
        animator.SetLayerWeight(index1, 1);
        animator.SetLayerWeight(index2, 0);
        animator.SetLayerWeight(index3, 0);
        animator.SetBool("Dead", true);
    }

    public void PlayAnim(AnimInputType animGameSystemInput)
    {
        if (animGameSystemInput == AnimInputType.Shoot)
        {
            animator.SetBool("Shoot", true);
            

        }
        else if (animGameSystemInput == AnimInputType.Reload)
        {
            animator.SetBool("Reloading", true);
        }
    }

    public void ContinueAnimEvent() //动画事件用
    {

        if (!islocal)
        {
            Debug.LogWarning(AnimQueue.Count);
            if (AnimQueue.Count != 0)
            {
                AnimInputType temp = AnimQueue.Dequeue();
                PlayAnim(temp);
                isFinish = false;
            }
            else
            {
                isFinish = true;
            }
        }
    }
    void  StopShoot()
    {
        if (!islocal)
        {
            animator.SetBool("Shoot", false);
            isFinish = true;
            ContinueAnimEvent();
        }
    }

    public void ReloadFinish()
    {
        if (islocal)
        {
            animator.SetBool("Reloading", false);
            _controller.ReloadFinish();
        }
        else
        {
            animator.SetBool("Reloading", false);
            ContinueAnimEvent();
        }
    }
}

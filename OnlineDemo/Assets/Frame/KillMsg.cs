using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class KillMsg : MonoBehaviour
{
   public Text killer;
   public Text killed;

   public void Init(string killerName,string killedName)
   {
      killer.text = killerName;
      killed.text = killedName;
      DestroyDelay().Forget();
   }

   public async UniTaskVoid DestroyDelay()
   {
      await UniTask.Delay(GameConfig.KillMsgDestroyTime);
      Destroy(gameObject);
   }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = System.Random;

public static class DeepCopy
{
   public static T DeepCopyByReflect<T>(T obj)
   {
      if (obj == null || (obj is string) || (obj.GetType().IsValueType)) return obj;
 
      object retval = Activator.CreateInstance(obj.GetType());
      FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
      foreach (FieldInfo field in fields)
      {
         try { field.SetValue(retval, DeepCopyByReflect(field.GetValue(obj))); }
         catch { }
      }
      return (T)retval;
   }
   
}
public class GameConfig : MonoBehaviour
{
   public static float moveSpeed = 5.0f;
   public static float SeverUpdateTime = 0.1f;
   public static float shootCD = 0.1f;
   public static float Damage = 20f;
   public static int RespawnTime = 5000; //毫秒
   public static float PlayerCapsuleRadius = 0.3f;
   public static List<Vector2> SpawnPosList=new List<Vector2>()
   {
      new Vector2(-0.519999981f,12.25f),
      new Vector2(-0.140000001f,-12.6899996f),
      new Vector2(-4.76999998f,0.0500000007f),
      new Vector2(4.42999983f,0.0500000007f)
   };
   public static Random Random = new Random();
   public static int KillMsgDestroyTime = 5000;

   public static float GetCurrentTime()
   {
      return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000.0f;
   }

   public static Dictionary<int, List<AnimInputType>> AnimActionDict = new Dictionary<int, List<AnimInputType>>();
    /// <summary>
    /// 2D叉乘
    /// </summary>
    /// <param name="v1">点1</param>
    /// <param name="v2">点2</param>
    /// <returns></returns>
    public static float CrossProduct2D(Vector2 v1,Vector2 v2)
    {
        //叉乘运算公式 x1*y2 - x2*y1
        return v1.x * v2.y - v2.x * v1.y;
    }
    
    /// <summary>
    /// 点是否在直线上
    /// </summary>
    /// <param name="point"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    /// <returns></returns>
    public static bool IsPointOnLine(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        float value = CrossProduct2D(point - lineStart, lineEnd - lineStart);
        return Mathf.Abs(value) <0.0003 /* 使用 Mathf.Approximately(value,0) 方式，在斜线上好像无法趋近为0*/;
    }
 
    /// <summary>
    /// 点到直线上的投影坐标
    /// </summary>
    /// <param name="point"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    /// <returns></returns>
    public static Vector2 Point2LineProject(Vector2 point,Vector2 lineStart,Vector2 lineEnd)
    {
    	if (IsPointOnLine(point,lineStart,lineEnd))
            return point;
        Vector2 v = point - lineStart;
        Vector2 u = lineEnd - lineStart;
        //求出u'的长度
        float u1Length = Vector2.Dot(u, v) / u.magnitude;
        return u1Length * u.normalized + lineStart;
    }
 
    /// <summary>
    /// 点到线段的距离
    /// </summary>
    /// <param name="point"></param>
    /// <param name="lineStart"></param>
    /// <param name="lineEnd"></param>
    /// <returns></returns>
    public static float Point2SegmentDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
    {
        Vector2 projectPoint = Point2LineProject(point,lineStart,lineEnd);
        if (projectPoint.x >= Mathf.Min(lineStart.x, lineEnd.x) &&
            projectPoint.x <= Mathf.Max(lineStart.x, lineEnd.x) &&
            projectPoint.y >= Mathf.Min(lineStart.y, lineEnd.y) && projectPoint.y <= Mathf.Max(lineStart.y, lineEnd.y))
            return Vector2.Distance(point, projectPoint);
        return Mathf.Min(Vector2.Distance(point,lineStart),Vector2.Distance(point,lineEnd));
    }
   
   
   public static List<PhysicalCheck> FindAllPhysicalChecks(GameObject prefab)
   {
      List<PhysicalCheck> list = new List<PhysicalCheck>();
      GameObject[] gameObjects = GameObject.FindGameObjectsWithTag("StaticCube");
      foreach (var gameObject in gameObjects)
      {
         list.Add(GetWorldPositionOfVertexs(gameObject,prefab));
      }
      return list;

   }
   
   static Rectangle GetWorldPositionOfVertexs(GameObject myGameObject,GameObject prefab)
   {
      BoxCollider boxCollider = myGameObject.GetComponent<BoxCollider>();
      float rad = myGameObject.transform.rotation.eulerAngles.y;
      //Vector3 temp1=boxCollider.transform.TransformPoint(boxCollider.center + new Vector3(boxCollider.size.x, boxCollider.size.y, boxCollider.size.z) * 0.5f);
      
      Vector3 realTopRight=boxCollider.transform.TransformPoint(boxCollider.center + new Vector3(boxCollider.size.x, boxCollider.size.y, boxCollider.size.z) * 0.5f);
      Vector3 realTopLeft=boxCollider.transform.TransformPoint(boxCollider.center + new Vector3(-boxCollider.size.x, boxCollider.size.y, boxCollider.size.z) * 0.5f);
      Vector3 pos = myGameObject.transform.position + new Vector3(myGameObject.transform.localScale.x*boxCollider.size.x / 2, 0, myGameObject.transform.localScale.z*boxCollider.size.z/2);
      //tr.transform.position = pos;
      Vector2 topRight= new Vector2(pos.x,pos.z); 
      pos = myGameObject.transform.position + new Vector3(-myGameObject.transform.localScale.x*boxCollider.size.x / 2, 0, -myGameObject.transform.localScale.z*boxCollider.size.z/2);
      //Vector3 temp2=boxCollider.transform.TransformPoint(boxCollider.center + new Vector3(-boxCollider.size.x, -boxCollider.size.y,- boxCollider.size.z) * 0.5f); 
      //bf.transform.position = pos;
      Vector3 realBottomLeft=boxCollider.transform.TransformPoint(boxCollider.center + new Vector3(-boxCollider.size.x, -boxCollider.size.y,- boxCollider.size.z) * 0.5f); 
      Vector3 realBottomRight=boxCollider.transform.TransformPoint(boxCollider.center + new Vector3(boxCollider.size.x, -boxCollider.size.y,- boxCollider.size.z) * 0.5f); 
      Vector2 bottomLeft=new Vector2(pos.x,pos.z);
      pos = boxCollider.transform.TransformPoint(boxCollider.center);
      Vector2 center = new Vector2(pos.x, pos.z);
      return new Rectangle(center, topRight, bottomLeft, rad,new Vector2(realTopRight.x,realTopRight.z),new Vector2(realBottomLeft.x,realBottomLeft.z)
      ,new Vector2(realTopLeft.x,realTopLeft.z),new Vector2(realBottomRight.x,realBottomRight.z));


   }
}
[Serializable]
public class PlayerState
{
   public int id;
   public string name;
   public Vector2 pos; //位置
   public Vector2 speed; //速度
   public Vector2 input;
   public float hp;
   public float dizzyEndTime;
   public Quaternion rotation;
   public Circle Circle;

   public PlayerState()
   {
     
   }

   public PlayerState(int _id,string _name,Vector2 _pos,float _hp)
   {
      name = _name;
      id = _id;
      pos = _pos;
      hp = _hp;
      rotation=Quaternion.Euler(0,0,0);
      Circle = new Circle(_pos, GameConfig.PlayerCapsuleRadius);
   }
}
[Serializable]
public class KDState
{
   public int id;
   public int kill;
   public int dead;


   public KDState()
   {
     
   }
   public KDState(int _id)
   {
      id = _id;
      kill = 0;
      dead = 0;
   }


}
[Serializable]
public class MsgStruct
{
   public InputType InputType;
   public string data;
}
[Serializable]

public class ClientInput
{
   public int frame;
   public int playerid;
   public InputType InputType;
   public string data;
}
/// <summary>
/// client在实现和解时所用数据结构
/// </summary>
public struct ClientSelfMsg
{
   public int frame;
   public GameSystemInput Input;
}


[Serializable]
public class ServerMsg
{
   public int lastSynFrame;
   public List<MsgStruct> MsgStructs;
}


//两种，圆与圆，圆与矩形检测
public interface PhysicalCheck
{
   bool Check(Circle c);
   bool Check(Rectangle r);

}

public class Circle : PhysicalCheck
{
   public Vector2 pos;
   public float radius;


   public Circle(Vector2 _pos,float _radius)
   {
      pos = _pos;
      radius = _radius;
   }
   public bool Check(Circle c)
   {
      return Vector2.Distance(pos, c.pos) <= (radius + c.radius);
   }

   public bool Check(Rectangle r)
   {
      //STEP1旋转
      float cos = Mathf.Cos(r.rad / 180 * 3.1415F);
      float sin = Mathf.Sin(r.rad / 180 * 3.1415F);
      Vector2 temp = new Vector2((pos.x - r.center.x) * cos - (pos.y - r.center.y) * sin + r.center.x,
         (pos.x - r.center.x) * sin + (pos.y - r.center.y) * cos + r.center.y);

      return BoxCircleIntersect(r.center,r.TopRight,temp,radius);

   }
   bool BoxCircleIntersect(Vector2 c,Vector2 h,Vector2 p,float r) //c:矩形中心坐标,h:topright,p:圆心,r:圆半径
   {
      Vector2 v = new Vector2(Mathf.Abs(p.x -c.x),Mathf.Abs(p.y-c.y));    //第1步:转换至第1象限
      Vector2 temp = h - c; //矩形中点指向topright
      Vector2 u = new Vector2(Mathf.Max(v.x - temp.x,0),Mathf.Max(v.y - temp.y,0)); //第2步:求圆心至矩形的最短距离矢量
      return u.sqrMagnitude <= r * r; //第3步:长度平方与半径平方比较
   }

}
public class Rectangle : PhysicalCheck
{
   
   public Rectangle(Vector2 _center,Vector2 _TopRight,Vector2 _BottomLeft,float _rad,Vector2 _realTopRight,Vector2 _realBottomLeft,Vector2 _realTopLeft,Vector2 _realBottomRight)
   {
      center = _center;
      TopRight = _TopRight;
      BottomLeft = _BottomLeft;
      rad = _rad;
      RealTopRight = _realTopRight;
      RealBottomLeft = _realBottomLeft;
      RealTopLeft = _realTopLeft;
      RealBottomRight = _realBottomRight;
   }

   public Vector2 center;
   public Vector2 TopRight;
   public Vector2 BottomLeft;
   public Vector2 RealTopRight;
   public Vector2 RealBottomRight;
   public Vector2 RealTopLeft;
   public Vector2 RealBottomLeft;
   public float rad;
   public bool Check(Circle c)
   {
      return c.Check(this);
   }

   public bool Check(Rectangle r)
   {
      return false;
   }

}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class SingleScore : MonoBehaviour
{
    public Text PlayerName;
    public Text Kill;
    public Text Death;
    private int kill;
    private int death;

    public void Init(string name, int k, int d)
    {
        PlayerName.text = name;
        Kill.text = k.ToString();
        Death.text = d.ToString();
        kill = k;
        death = d;
    }

    public void AddKill()
    {
        kill++;
        Kill.text = kill.ToString();
    }

    public void AddDeath()
    {
        death++;
        Death.text = death.ToString();
    }
    
    
}

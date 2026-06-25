using GlobalEnum;
using System;
using UnityEngine;

public interface IDamagable
{
    public event Action Outline;
    public void Damage(float dmg);
}
